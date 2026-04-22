using System.Globalization;
using UnityEngine;

namespace LiteRealm.Multiplayer
{
    public enum LanPacketType
    {
        Unknown,
        Join,
        Accept,
        Reject,
        State,
        Leave,
        Ping
    }

    public struct LanPacket
    {
        public LanPacketType Type;
        public int PlayerId;
        public int MaxPlayers;
        public string Name;
        public string Reason;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public static class LanProtocol
    {
        public const int DefaultPort = 7777;
        public const int MaxPlayers = 4;

        private const string Version = "LRLAN1";
        private const char Separator = '|';
        private const int MaxNameLength = 24;
        private const int MaxReasonLength = 96;

        public static string BuildJoin(string playerName)
        {
            return Version + Separator + "JOIN" + Separator + SanitizePlayerName(playerName);
        }

        public static string BuildAccept(int playerId, int maxPlayers)
        {
            return Version + Separator + "ACCEPT" + Separator + Mathf.Max(1, playerId).ToString(CultureInfo.InvariantCulture)
                   + Separator + Mathf.Clamp(maxPlayers, 1, MaxPlayers).ToString(CultureInfo.InvariantCulture);
        }

        public static string BuildReject(string reason)
        {
            return Version + Separator + "REJECT" + Separator + SanitizeReason(reason);
        }

        public static string BuildState(int playerId, string playerName, Vector3 position, Quaternion rotation)
        {
            return string.Join(
                Separator.ToString(),
                Version,
                "STATE",
                Mathf.Max(1, playerId).ToString(CultureInfo.InvariantCulture),
                SanitizePlayerName(playerName),
                FloatToString(position.x),
                FloatToString(position.y),
                FloatToString(position.z),
                FloatToString(rotation.x),
                FloatToString(rotation.y),
                FloatToString(rotation.z),
                FloatToString(rotation.w));
        }

        public static string BuildLeave(int playerId)
        {
            return Version + Separator + "LEAVE" + Separator + Mathf.Max(1, playerId).ToString(CultureInfo.InvariantCulture);
        }

        public static string BuildPing()
        {
            return Version + Separator + "PING";
        }

        public static bool TryParse(string message, out LanPacket packet)
        {
            packet = new LanPacket
            {
                Type = LanPacketType.Unknown,
                Rotation = Quaternion.identity
            };

            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            string[] parts = message.Split(Separator);
            if (parts.Length < 2 || parts[0] != Version)
            {
                return false;
            }

            switch (parts[1])
            {
                case "JOIN":
                    if (parts.Length < 3)
                    {
                        return false;
                    }

                    packet.Type = LanPacketType.Join;
                    packet.Name = SanitizePlayerName(parts[2]);
                    return true;

                case "ACCEPT":
                    if (parts.Length < 4 || !TryParsePlayerId(parts[2], out int acceptedId) || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxPlayers))
                    {
                        return false;
                    }

                    packet.Type = LanPacketType.Accept;
                    packet.PlayerId = acceptedId;
                    packet.MaxPlayers = Mathf.Clamp(maxPlayers, 1, MaxPlayers);
                    return true;

                case "REJECT":
                    if (parts.Length < 3)
                    {
                        return false;
                    }

                    packet.Type = LanPacketType.Reject;
                    packet.Reason = SanitizeReason(parts[2]);
                    return true;

                case "STATE":
                    if (parts.Length < 11 || !TryParsePlayerId(parts[2], out int stateId))
                    {
                        return false;
                    }

                    if (!TryParseFloat(parts[4], out float px)
                        || !TryParseFloat(parts[5], out float py)
                        || !TryParseFloat(parts[6], out float pz)
                        || !TryParseFloat(parts[7], out float rx)
                        || !TryParseFloat(parts[8], out float ry)
                        || !TryParseFloat(parts[9], out float rz)
                        || !TryParseFloat(parts[10], out float rw))
                    {
                        return false;
                    }

                    packet.Type = LanPacketType.State;
                    packet.PlayerId = stateId;
                    packet.Name = SanitizePlayerName(parts[3]);
                    packet.Position = new Vector3(px, py, pz);
                    packet.Rotation = new Quaternion(rx, ry, rz, rw);
                    return true;

                case "LEAVE":
                    if (parts.Length < 3 || !TryParsePlayerId(parts[2], out int leftId))
                    {
                        return false;
                    }

                    packet.Type = LanPacketType.Leave;
                    packet.PlayerId = leftId;
                    return true;

                case "PING":
                    packet.Type = LanPacketType.Ping;
                    return true;

                default:
                    return false;
            }
        }

        public static string SanitizePlayerName(string playerName)
        {
            return SanitizeText(playerName, "Player", MaxNameLength);
        }

        private static string SanitizeReason(string reason)
        {
            return SanitizeText(reason, "No reason provided.", MaxReasonLength);
        }

        private static string SanitizeText(string value, string fallback, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string sanitized = value.Trim()
                .Replace(Separator, ' ')
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            while (sanitized.Contains("  "))
            {
                sanitized = sanitized.Replace("  ", " ");
            }

            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength).Trim();
            }

            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private static bool TryParsePlayerId(string value, out int playerId)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out playerId) && playerId > 0;
        }

        private static bool TryParseFloat(string value, out float parsed)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
        }

        private static string FloatToString(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
