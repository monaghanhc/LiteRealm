using UnityEngine;

namespace LiteRealm.Multiplayer
{
    public enum LanSessionMode
    {
        Offline,
        Host,
        Client
    }

    public struct PendingLanSession
    {
        public LanSessionMode Mode;
        public string HostAddress;
        public string PlayerName;

        public bool IsActive => Mode != LanSessionMode.Offline;
    }

    public static class LanSessionLauncher
    {
        private const string ModeKey = "LiteRealm.Lan.Mode";
        private const string HostAddressKey = "LiteRealm.Lan.HostAddress";
        private const string PlayerNameKey = "LiteRealm.Lan.PlayerName";
        private const string HostModeValue = "Host";
        private const string ClientModeValue = "Client";

        public static void StartHost(string playerName)
        {
            PlayerPrefs.SetString(ModeKey, HostModeValue);
            PlayerPrefs.SetString(HostAddressKey, string.Empty);
            PlayerPrefs.SetString(PlayerNameKey, LanProtocol.SanitizePlayerName(playerName));
            PlayerPrefs.Save();
        }

        public static void StartClient(string hostAddress, string playerName)
        {
            PlayerPrefs.SetString(ModeKey, ClientModeValue);
            PlayerPrefs.SetString(HostAddressKey, string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim());
            PlayerPrefs.SetString(PlayerNameKey, LanProtocol.SanitizePlayerName(playerName));
            PlayerPrefs.Save();
        }

        public static PendingLanSession PeekPendingSession()
        {
            string mode = PlayerPrefs.GetString(ModeKey, string.Empty);
            if (mode == HostModeValue)
            {
                return new PendingLanSession
                {
                    Mode = LanSessionMode.Host,
                    PlayerName = PlayerPrefs.GetString(PlayerNameKey, "Player")
                };
            }

            if (mode == ClientModeValue)
            {
                return new PendingLanSession
                {
                    Mode = LanSessionMode.Client,
                    HostAddress = PlayerPrefs.GetString(HostAddressKey, "127.0.0.1"),
                    PlayerName = PlayerPrefs.GetString(PlayerNameKey, "Player")
                };
            }

            return new PendingLanSession
            {
                Mode = LanSessionMode.Offline
            };
        }

        public static PendingLanSession ConsumePendingSession()
        {
            PendingLanSession session = PeekPendingSession();
            ClearPendingSession();
            return session;
        }

        public static void ClearPendingSession()
        {
            PlayerPrefs.DeleteKey(ModeKey);
            PlayerPrefs.DeleteKey(HostAddressKey);
            PlayerPrefs.DeleteKey(PlayerNameKey);
            PlayerPrefs.Save();
        }
    }
}
