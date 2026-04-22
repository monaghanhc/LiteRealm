using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace LiteRealm.Multiplayer
{
    [DisallowMultipleComponent]
    public class LanMultiplayerManager : MonoBehaviour
    {
        [SerializeField] private int port = LanProtocol.DefaultPort;
        [SerializeField] private int maxPlayers = LanProtocol.MaxPlayers;
        [SerializeField] private float sendInterval = 0.1f;
        [SerializeField] private float joinRetryInterval = 1f;
        [SerializeField] private float peerTimeoutSeconds = 10f;
        [SerializeField] private float avatarSmoothing = 14f;
        [SerializeField] private bool showStatusHud = true;

        private readonly Dictionary<int, RemotePlayerState> remotePlayers = new Dictionary<int, RemotePlayerState>();
        private readonly Dictionary<int, HostClientState> hostClientsById = new Dictionary<int, HostClientState>();
        private readonly Dictionary<string, HostClientState> hostClientsByEndpoint = new Dictionary<string, HostClientState>();

        private UdpClient udp;
        private IPEndPoint hostEndpoint;
        private Transform localPlayer;
        private Camera mainCamera;
        private LanSessionMode mode = LanSessionMode.Offline;
        private string localPlayerName = "Player";
        private string statusText = "LAN offline";
        private int localPlayerId;
        private bool clientAccepted;
        private float nextSendTime;
        private float nextJoinAttemptTime;

        public LanSessionMode Mode => mode;
        public int ConnectedPlayerCount => mode == LanSessionMode.Host ? hostClientsById.Count + 1 : remotePlayers.Count + (clientAccepted ? 1 : 0);
        public string StatusText => statusText;

        private void Start()
        {
            maxPlayers = Mathf.Clamp(maxPlayers, 1, LanProtocol.MaxPlayers);
            PendingLanSession session = LanSessionLauncher.ConsumePendingSession();
            if (!session.IsActive)
            {
                enabled = false;
                return;
            }

            localPlayer = FindLocalPlayer();
            if (localPlayer == null)
            {
                statusText = "LAN could not start: no local player found.";
                enabled = false;
                return;
            }

            mainCamera = Camera.main;
            localPlayerName = LanProtocol.SanitizePlayerName(session.PlayerName);

            if (session.Mode == LanSessionMode.Host)
            {
                StartHost();
            }
            else
            {
                StartClient(session.HostAddress);
            }
        }

        private void Update()
        {
            if (udp == null)
            {
                UpdateRemoteAvatars(Time.unscaledDeltaTime);
                return;
            }

            PollIncomingPackets();
            RemoveTimedOutPeers();
            UpdateRemoteAvatars(Time.unscaledDeltaTime);

            float now = Time.unscaledTime;
            if (mode == LanSessionMode.Client && !clientAccepted && now >= nextJoinAttemptTime)
            {
                nextJoinAttemptTime = now + joinRetryInterval;
                SendToHost(LanProtocol.BuildJoin(localPlayerName));
            }

            if (now < nextSendTime)
            {
                return;
            }

            nextSendTime = now + Mathf.Max(0.03f, sendInterval);
            if (mode == LanSessionMode.Host)
            {
                BroadcastAllKnownStates();
            }
            else if (mode == LanSessionMode.Client && clientAccepted)
            {
                SendLocalStateToHost();
            }
        }

        private void OnDestroy()
        {
            SendLeavePacket();
            CloseSocket();
            ClearRemotePlayers();
        }

        private void OnApplicationQuit()
        {
            SendLeavePacket();
            CloseSocket();
        }

        private void OnGUI()
        {
            if (!showStatusHud || mode == LanSessionMode.Offline)
            {
                return;
            }

            Rect area = new Rect(12f, 12f, 330f, mode == LanSessionMode.Host ? 118f : 96f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("LAN " + mode + "  " + ConnectedPlayerCount + "/" + maxPlayers);
            GUILayout.Label(statusText);
            if (mode == LanSessionMode.Host)
            {
                GUILayout.Label("Host IP: " + GetLocalIPv4Address() + ":" + port);
            }

            GUILayout.EndArea();
        }

        private void StartHost()
        {
            try
            {
                udp = new UdpClient(AddressFamily.InterNetwork);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                udp.Client.Blocking = false;
                mode = LanSessionMode.Host;
                localPlayerId = 1;
                clientAccepted = true;
                statusText = "Hosting on local Wi-Fi.";
                Debug.Log("LAN host started on " + GetLocalIPv4Address() + ":" + port);
            }
            catch (Exception ex)
            {
                statusText = "LAN host failed: " + ex.Message;
                Debug.LogWarning(statusText);
                CloseSocket();
            }
        }

        private void StartClient(string hostAddress)
        {
            mode = LanSessionMode.Client;
            if (!TryResolveHostAddress(hostAddress, out IPAddress address))
            {
                statusText = "LAN join failed: invalid host address.";
                return;
            }

            try
            {
                hostEndpoint = new IPEndPoint(address, port);
                udp = new UdpClient(0);
                udp.Client.Blocking = false;
                statusText = "Joining " + hostEndpoint + "...";
                nextJoinAttemptTime = 0f;
                Debug.Log("LAN client joining " + hostEndpoint);
            }
            catch (Exception ex)
            {
                statusText = "LAN join failed: " + ex.Message;
                Debug.LogWarning(statusText);
                CloseSocket();
            }
        }

        private void PollIncomingPackets()
        {
            try
            {
                while (udp != null && udp.Available > 0)
                {
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udp.Receive(ref sender);
                    if (data == null || data.Length == 0 || data.Length > 2048)
                    {
                        continue;
                    }

                    string message = Encoding.UTF8.GetString(data);
                    if (!LanProtocol.TryParse(message, out LanPacket packet))
                    {
                        continue;
                    }

                    if (mode == LanSessionMode.Host)
                    {
                        HandleHostPacket(packet, sender);
                    }
                    else if (mode == LanSessionMode.Client)
                    {
                        HandleClientPacket(packet);
                    }
                }
            }
            catch (SocketException)
            {
                // Non-blocking UDP can report would-block during normal polling.
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void HandleHostPacket(LanPacket packet, IPEndPoint sender)
        {
            switch (packet.Type)
            {
                case LanPacketType.Join:
                    HandleJoinRequest(packet, sender);
                    break;

                case LanPacketType.State:
                    HandleClientState(packet, sender);
                    break;

                case LanPacketType.Leave:
                    RemoveClient(sender);
                    break;
            }
        }

        private void HandleClientPacket(LanPacket packet)
        {
            switch (packet.Type)
            {
                case LanPacketType.Accept:
                    localPlayerId = packet.PlayerId;
                    clientAccepted = true;
                    statusText = "Connected to LAN host.";
                    break;

                case LanPacketType.Reject:
                    statusText = "LAN join rejected: " + packet.Reason;
                    Debug.LogWarning(statusText);
                    CloseSocket();
                    break;

                case LanPacketType.State:
                    if (packet.PlayerId != localPlayerId)
                    {
                        UpsertRemotePlayer(packet.PlayerId, packet.Name, packet.Position, packet.Rotation);
                    }

                    break;

                case LanPacketType.Leave:
                    RemoveRemotePlayer(packet.PlayerId);
                    break;
            }
        }

        private void HandleJoinRequest(LanPacket packet, IPEndPoint sender)
        {
            string endpointKey = BuildEndpointKey(sender);
            if (hostClientsByEndpoint.TryGetValue(endpointKey, out HostClientState existing))
            {
                existing.Name = packet.Name;
                existing.LastHeardTime = Time.unscaledTime;
                SendTo(sender, LanProtocol.BuildAccept(existing.PlayerId, maxPlayers));
                return;
            }

            if (hostClientsById.Count + 1 >= maxPlayers)
            {
                SendTo(sender, LanProtocol.BuildReject("Session full."));
                return;
            }

            int playerId = AllocateClientId();
            if (playerId < 0)
            {
                SendTo(sender, LanProtocol.BuildReject("No open player slots."));
                return;
            }

            HostClientState client = new HostClientState
            {
                PlayerId = playerId,
                Name = packet.Name,
                Endpoint = CopyEndpoint(sender),
                Position = localPlayer != null ? localPlayer.position + Vector3.right * playerId : Vector3.zero,
                Rotation = Quaternion.identity,
                LastHeardTime = Time.unscaledTime
            };

            hostClientsById[playerId] = client;
            hostClientsByEndpoint[endpointKey] = client;
            SendTo(sender, LanProtocol.BuildAccept(playerId, maxPlayers));
            UpsertRemotePlayer(playerId, client.Name, client.Position, client.Rotation);
            statusText = "Hosting on local Wi-Fi. Players: " + ConnectedPlayerCount + "/" + maxPlayers;
        }

        private void HandleClientState(LanPacket packet, IPEndPoint sender)
        {
            string endpointKey = BuildEndpointKey(sender);
            if (!hostClientsByEndpoint.TryGetValue(endpointKey, out HostClientState client) || client.PlayerId != packet.PlayerId)
            {
                return;
            }

            client.Name = packet.Name;
            client.Position = packet.Position;
            client.Rotation = packet.Rotation;
            client.LastHeardTime = Time.unscaledTime;
            UpsertRemotePlayer(client.PlayerId, client.Name, client.Position, client.Rotation);
        }

        private void BroadcastAllKnownStates()
        {
            if (localPlayer == null || hostClientsById.Count == 0)
            {
                return;
            }

            string hostState = LanProtocol.BuildState(localPlayerId, localPlayerName, localPlayer.position, localPlayer.rotation);
            foreach (HostClientState recipient in hostClientsById.Values)
            {
                SendTo(recipient.Endpoint, hostState);
            }

            foreach (HostClientState clientState in hostClientsById.Values)
            {
                string message = LanProtocol.BuildState(clientState.PlayerId, clientState.Name, clientState.Position, clientState.Rotation);
                foreach (HostClientState recipient in hostClientsById.Values)
                {
                    SendTo(recipient.Endpoint, message);
                }
            }
        }

        private void SendLocalStateToHost()
        {
            if (localPlayer == null || localPlayerId <= 0)
            {
                return;
            }

            SendToHost(LanProtocol.BuildState(localPlayerId, localPlayerName, localPlayer.position, localPlayer.rotation));
        }

        private void SendLeavePacket()
        {
            if (udp == null || localPlayerId <= 0)
            {
                return;
            }

            if (mode == LanSessionMode.Client && hostEndpoint != null)
            {
                SendToHost(LanProtocol.BuildLeave(localPlayerId));
            }
            else if (mode == LanSessionMode.Host)
            {
                string leave = LanProtocol.BuildLeave(localPlayerId);
                foreach (HostClientState client in hostClientsById.Values)
                {
                    SendTo(client.Endpoint, leave);
                }
            }
        }

        private void SendToHost(string message)
        {
            if (hostEndpoint != null)
            {
                SendTo(hostEndpoint, message);
            }
        }

        private void SendTo(IPEndPoint endpoint, string message)
        {
            if (udp == null || endpoint == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                udp.Send(data, data.Length, endpoint);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning("LAN send failed: " + ex.Message);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void RemoveTimedOutPeers()
        {
            if (mode != LanSessionMode.Host || hostClientsById.Count == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            List<int> timedOut = null;
            foreach (HostClientState client in hostClientsById.Values)
            {
                if (now - client.LastHeardTime >= peerTimeoutSeconds)
                {
                    if (timedOut == null)
                    {
                        timedOut = new List<int>();
                    }

                    timedOut.Add(client.PlayerId);
                }
            }

            if (timedOut == null)
            {
                return;
            }

            for (int i = 0; i < timedOut.Count; i++)
            {
                RemoveClient(timedOut[i]);
            }
        }

        private void RemoveClient(IPEndPoint endpoint)
        {
            string endpointKey = BuildEndpointKey(endpoint);
            if (hostClientsByEndpoint.TryGetValue(endpointKey, out HostClientState client))
            {
                RemoveClient(client.PlayerId);
            }
        }

        private void RemoveClient(int playerId)
        {
            if (!hostClientsById.TryGetValue(playerId, out HostClientState client))
            {
                return;
            }

            hostClientsById.Remove(playerId);
            hostClientsByEndpoint.Remove(BuildEndpointKey(client.Endpoint));
            RemoveRemotePlayer(playerId);

            string leave = LanProtocol.BuildLeave(playerId);
            foreach (HostClientState recipient in hostClientsById.Values)
            {
                SendTo(recipient.Endpoint, leave);
            }

            statusText = "Hosting on local Wi-Fi. Players: " + ConnectedPlayerCount + "/" + maxPlayers;
        }

        private int AllocateClientId()
        {
            for (int id = 2; id <= maxPlayers; id++)
            {
                if (!hostClientsById.ContainsKey(id))
                {
                    return id;
                }
            }

            return -1;
        }

        private void UpsertRemotePlayer(int playerId, string playerName, Vector3 position, Quaternion rotation)
        {
            if (playerId <= 0 || playerId == localPlayerId)
            {
                return;
            }

            if (!remotePlayers.TryGetValue(playerId, out RemotePlayerState state))
            {
                state = new RemotePlayerState
                {
                    PlayerId = playerId,
                    Avatar = CreateRemoteAvatar(playerId, playerName)
                };
                remotePlayers[playerId] = state;
            }

            state.Name = LanProtocol.SanitizePlayerName(playerName);
            state.TargetPosition = position;
            state.TargetRotation = IsZeroRotation(rotation) ? Quaternion.identity : rotation;
            state.LastHeardTime = Time.unscaledTime;
        }

        private GameObject CreateRemoteAvatar(int playerId, string playerName)
        {
            GameObject root = new GameObject("RemotePlayer_" + playerId);
            root.transform.position = localPlayer != null ? localPlayer.position + Vector3.right * playerId : Vector3.zero;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateAvatarMaterial(playerId);
            }

            GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rifle.name = "Rifle";
            rifle.transform.SetParent(root.transform, false);
            rifle.transform.localPosition = new Vector3(0.42f, 1.05f, 0.35f);
            rifle.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
            rifle.transform.localScale = new Vector3(0.12f, 0.12f, 0.85f);
            Collider rifleCollider = rifle.GetComponent<Collider>();
            if (rifleCollider != null)
            {
                Destroy(rifleCollider);
            }

            Renderer rifleRenderer = rifle.GetComponent<Renderer>();
            if (rifleRenderer != null)
            {
                rifleRenderer.material = CreateSimpleMaterial(new Color(0.08f, 0.075f, 0.07f), 0.2f);
            }

            GameObject label = new GameObject("NameLabel");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = LanProtocol.SanitizePlayerName(playerName);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.09f;
            text.fontSize = 32;
            text.color = Color.white;

            return root;
        }

        private void UpdateRemoteAvatars(float deltaTime)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            float t = 1f - Mathf.Exp(-avatarSmoothing * Mathf.Max(0f, deltaTime));
            foreach (RemotePlayerState state in remotePlayers.Values)
            {
                if (state.Avatar == null)
                {
                    continue;
                }

                state.Avatar.transform.position = Vector3.Lerp(state.Avatar.transform.position, state.TargetPosition, t);
                state.Avatar.transform.rotation = Quaternion.Slerp(state.Avatar.transform.rotation, state.TargetRotation, t);

                Transform label = state.Avatar.transform.Find("NameLabel");
                if (label != null)
                {
                    TextMesh text = label.GetComponent<TextMesh>();
                    if (text != null)
                    {
                        text.text = state.Name;
                    }

                    if (mainCamera != null)
                    {
                        label.rotation = Quaternion.LookRotation(label.position - mainCamera.transform.position);
                    }
                }
            }
        }

        private void RemoveRemotePlayer(int playerId)
        {
            if (!remotePlayers.TryGetValue(playerId, out RemotePlayerState state))
            {
                return;
            }

            if (state.Avatar != null)
            {
                Destroy(state.Avatar);
            }

            remotePlayers.Remove(playerId);
        }

        private void ClearRemotePlayers()
        {
            foreach (RemotePlayerState state in remotePlayers.Values)
            {
                if (state.Avatar != null)
                {
                    Destroy(state.Avatar);
                }
            }

            remotePlayers.Clear();
        }

        private Transform FindLocalPlayer()
        {
            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                return player != null ? player.transform : null;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private void CloseSocket()
        {
            try
            {
                udp?.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            udp = null;
        }

        private static string BuildEndpointKey(IPEndPoint endpoint)
        {
            return endpoint == null ? string.Empty : endpoint.Address + ":" + endpoint.Port;
        }

        private static IPEndPoint CopyEndpoint(IPEndPoint endpoint)
        {
            return endpoint == null ? null : new IPEndPoint(endpoint.Address, endpoint.Port);
        }

        private static bool IsZeroRotation(Quaternion rotation)
        {
            return Mathf.Approximately(rotation.x, 0f)
                   && Mathf.Approximately(rotation.y, 0f)
                   && Mathf.Approximately(rotation.z, 0f)
                   && Mathf.Approximately(rotation.w, 0f);
        }

        private static bool TryResolveHostAddress(string hostAddress, out IPAddress address)
        {
            address = null;
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                return false;
            }

            if (IPAddress.TryParse(hostAddress.Trim(), out address))
            {
                return true;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(hostAddress.Trim());
                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = addresses[i];
                        return true;
                    }
                }
            }
            catch (SocketException)
            {
            }

            return false;
        }

        public static string GetLocalIPv4Address()
        {
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    NetworkInterface networkInterface = interfaces[i];
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    IPInterfaceProperties properties = networkInterface.GetIPProperties();
                    UnicastIPAddressInformationCollection addresses = properties.UnicastAddresses;
                    foreach (UnicastIPAddressInformation info in addresses)
                    {
                        if (info.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(info.Address))
                        {
                            return info.Address.ToString();
                        }
                    }
                }
            }
            catch (NetworkInformationException)
            {
            }

            return "127.0.0.1";
        }

        private static Material CreateAvatarMaterial(int playerId)
        {
            Color[] colors =
            {
                new Color(0.18f, 0.52f, 0.78f),
                new Color(0.82f, 0.58f, 0.18f),
                new Color(0.35f, 0.72f, 0.38f),
                new Color(0.74f, 0.34f, 0.28f)
            };

            return CreateSimpleMaterial(colors[Mathf.Abs(playerId) % colors.Length], 0.32f);
        }

        private static Material CreateSimpleMaterial(Color color, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.35f);
            }

            return material;
        }

        private sealed class HostClientState
        {
            public int PlayerId;
            public string Name;
            public IPEndPoint Endpoint;
            public Vector3 Position;
            public Quaternion Rotation = Quaternion.identity;
            public float LastHeardTime;
        }

        private sealed class RemotePlayerState
        {
            public int PlayerId;
            public string Name;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation = Quaternion.identity;
            public GameObject Avatar;
            public float LastHeardTime;
        }
    }
}
