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
