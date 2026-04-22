using LiteRealm.Multiplayer;
using NUnit.Framework;
using UnityEngine;

namespace LiteRealm.Tests.Editor
{
    public class LanMultiplayerEditModeTests
    {
        [SetUp]
        public void SetUp()
        {
            LanSessionLauncher.ClearPendingSession();
        }

        [TearDown]
        public void TearDown()
        {
            LanSessionLauncher.ClearPendingSession();
        }

        [Test]
        public void JoinPacketSanitizesAndParsesPlayerName()
        {
            string message = LanProtocol.BuildJoin("  Scout|One\n");

            Assert.IsTrue(LanProtocol.TryParse(message, out LanPacket packet));
            Assert.AreEqual(LanPacketType.Join, packet.Type);
            Assert.AreEqual("Scout One", packet.Name);
        }

        [Test]
        public void StatePacketRoundTripsPose()
        {
            Vector3 position = new Vector3(12.25f, 1.5f, -8.75f);
            Quaternion rotation = Quaternion.Euler(0f, 137.5f, 0f);
            string message = LanProtocol.BuildState(3, "Ranger", position, rotation);

            Assert.IsTrue(LanProtocol.TryParse(message, out LanPacket packet));
            Assert.AreEqual(LanPacketType.State, packet.Type);
            Assert.AreEqual(3, packet.PlayerId);
            Assert.AreEqual("Ranger", packet.Name);
            Assert.That(Vector3.Distance(position, packet.Position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rotation, packet.Rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void ParserRejectsWrongProtocolVersion()
        {
            Assert.IsFalse(LanProtocol.TryParse("OLD|JOIN|Player", out LanPacket packet));
            Assert.AreEqual(LanPacketType.Unknown, packet.Type);
        }

        [Test]
        public void SessionLauncherStoresAndConsumesClientRequest()
        {
            LanSessionLauncher.StartClient("192.168.0.25", "Scout|Two");

            PendingLanSession pending = LanSessionLauncher.PeekPendingSession();
            Assert.IsTrue(pending.IsActive);
            Assert.AreEqual(LanSessionMode.Client, pending.Mode);
            Assert.AreEqual("192.168.0.25", pending.HostAddress);
            Assert.AreEqual("Scout Two", pending.PlayerName);

            PendingLanSession consumed = LanSessionLauncher.ConsumePendingSession();
            Assert.AreEqual(LanSessionMode.Client, consumed.Mode);
            Assert.IsFalse(LanSessionLauncher.PeekPendingSession().IsActive);
        }

        [Test]
        public void ProtocolPlayerCapMatchesLocalCoopScope()
        {
            Assert.AreEqual(4, LanProtocol.MaxPlayers);
        }
    }
}
