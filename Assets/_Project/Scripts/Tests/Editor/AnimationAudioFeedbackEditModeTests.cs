using System.Reflection;
using LiteRealm.Combat;
using LiteRealm.Core;
using LiteRealm.Player;
using NUnit.Framework;
using UnityEngine;

namespace LiteRealm.Tests.Editor
{
    public class AnimationAudioFeedbackEditModeTests
    {
        [Test]
        public void PlayerStats_RaisesDamagedEvent_WhenDamageIsApplied()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerStats stats = player.AddComponent<PlayerStats>();
                InvokeLifecycle(stats, "Awake");
                int damageEvents = 0;
                stats.Damaged += _ => damageEvents++;

                stats.ApplyDamage(new DamageInfo(12f, player.transform.position, Vector3.up, Vector3.forward, null, "zombie.basic"));

                Assert.AreEqual(1, damageEvents);
                Assert.Less(stats.CurrentHealth, 100f);
                Assert.IsNotNull(player.GetComponent<PlayerDamageAudioController>());
                Assert.IsNotNull(player.GetComponent<PlayerImpactFeedbackController>());
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HealthComponent_CanKeepGameObjectActive_OnDeath()
        {
            GameObject zombie = new GameObject("Zombie");
            try
            {
                HealthComponent health = zombie.AddComponent<HealthComponent>();
                InvokeLifecycle(health, "Awake");
                health.ConfigureDeathObjectHandling(false, false);

                health.ApplyDamage(new DamageInfo(999f, zombie.transform.position, Vector3.up, Vector3.forward, null, "weapon.rifle"));

                Assert.IsTrue(health.IsDead);
                Assert.IsTrue(zombie.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(zombie);
            }
        }

        [Test]
        public void WeaponBase_EmptyMagazine_DoesNotFire_AndRaisesEmptyEvent()
        {
            GameObject weaponObject = new GameObject("Rifle");
            try
            {
                HitscanRifle rifle = weaponObject.AddComponent<HitscanRifle>();
                InvokeLifecycle(rifle, "Awake");
                int emptyEvents = 0;
                rifle.EmptyMagazineTriggered += _ => emptyEvents++;
                rifle.ForceSetAmmo(0);

                bool fired = rifle.TryFire(new WeaponFireContext
                {
                    AimCamera = null,
                    HitMask = ~0,
                    Instigator = weaponObject,
                    EventHub = null,
                    IsAiming = false
                });

                Assert.IsFalse(fired);
                Assert.AreEqual(1, emptyEvents);
                Assert.AreEqual(0, rifle.CurrentAmmo);
            }
            finally
            {
                Object.DestroyImmediate(weaponObject);
            }
        }

        [Test]
        public void GameEventHub_RaisesDamageDealtEvent()
        {
            GameObject hubObject = new GameObject("Hub");
            GameObject instigator = new GameObject("Player");
            GameObject target = new GameObject("Zombie");
            try
            {
                GameEventHub hub = hubObject.AddComponent<GameEventHub>();
                DamageDealtEvent received = new DamageDealtEvent();
                int receivedCount = 0;
                hub.DamageDealt += data =>
                {
                    received = data;
                    receivedCount++;
                };

                hub.RaiseDamageDealt(new DamageDealtEvent
                {
                    SourceId = "weapon.rifle",
                    Amount = 22f,
                    Position = Vector3.one,
                    Normal = Vector3.up,
                    Instigator = instigator,
                    Target = target,
                    Killed = true
                });

                Assert.AreEqual(1, receivedCount);
                Assert.AreEqual("weapon.rifle", received.SourceId);
                Assert.AreEqual(22f, received.Amount);
                Assert.AreSame(instigator, received.Instigator);
                Assert.AreSame(target, received.Target);
                Assert.IsTrue(received.Killed);
            }
            finally
            {
                Object.DestroyImmediate(hubObject);
                Object.DestroyImmediate(instigator);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void WeaponManager_ForwardsEmptyMagazineEventFromActiveWeapon()
        {
            GameObject player = new GameObject("Player");
            GameObject weaponObject = new GameObject("Rifle");
            try
            {
                weaponObject.transform.SetParent(player.transform);
                HitscanRifle rifle = weaponObject.AddComponent<HitscanRifle>();
                InvokeLifecycle(rifle, "Awake");

                WeaponManager manager = player.AddComponent<WeaponManager>();
                InvokeLifecycle(manager, "Awake");

                int emptyEvents = 0;
                manager.EmptyMagazineTriggered += weapon =>
                {
                    Assert.AreSame(rifle, weapon);
                    emptyEvents++;
                };

                rifle.ForceSetAmmo(0);
                bool fired = manager.TryFireActiveWeapon();

                Assert.IsFalse(fired);
                Assert.AreEqual(1, emptyEvents);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = null;
            System.Type type = target.GetType();
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.IsNotNull(method, $"Missing lifecycle method {methodName} on {target.GetType().Name}.");
            method.Invoke(target, null);
        }
    }
}
