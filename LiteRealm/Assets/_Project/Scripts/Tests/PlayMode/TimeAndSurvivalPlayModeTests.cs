using System.Collections;
using LiteRealm.AI;
using LiteRealm.Player;
using LiteRealm.UI;
using LiteRealm.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class TimeAndSurvivalPlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator TimeAndSurvival_FastForwardNight_StatsDrain_HudUpdates_NoNullRefs()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");

            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            DayNightCycleManager dayNight = Object.FindObjectOfType<DayNightCycleManager>();
            Assert.IsNotNull(dayNight, "DayNightCycleManager missing in Main scene.");

            SpawnerZone spawner = Object.FindObjectOfType<SpawnerZone>();
            Assert.IsNotNull(spawner, "SpawnerZone missing in Main scene.");

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            Assert.IsNotNull(player, "Player missing in Main scene.");

            PlayerStats stats = player.GetComponent<PlayerStats>();
            Assert.IsNotNull(stats, "PlayerStats missing on player.");

            SurvivalHUDController hud = Object.FindObjectOfType<SurvivalHUDController>();
            Assert.IsNotNull(hud, "SurvivalHUDController missing.");
            Assert.IsTrue(hud.HasCoreHudBindings, "HUD core bindings are incomplete.");
            Assert.IsTrue(hud.HasRuntimeReferences, "HUD runtime references are incomplete.");

            dayNight.SetTimeNormalized(0.25f);
            yield return null;
            yield return null;
            Assert.IsFalse(dayNight.IsNight, "Expected daytime after setting normalized time to 0.25.");

            float dayMultiplier = spawner.CurrentSpawnMultiplier;
            int dayMaxAlive = spawner.CurrentMaxAlive;

            int existingZombieCount = Object.FindObjectsOfType<ZombieAI>(true).Length;
            spawner.SpawnZombie();
            yield return null;
            yield return new WaitForSeconds(0.2f);

            ZombieAI[] spawnedZombies = Object.FindObjectsOfType<ZombieAI>(true);
            Assert.Greater(spawnedZombies.Length, existingZombieCount, "Could not spawn zombie for day/night aggression check.");
            ZombieAI zombie = spawnedZombies[spawnedZombies.Length - 1];
            Assert.IsNotNull(zombie, "Spawned zombie reference is null.");

            float daySenseMultiplier = zombie.CurrentSenseMultiplier;
            Assert.GreaterOrEqual(daySenseMultiplier, 1f, "Day sense multiplier should be >= 1.");

            dayNight.SetTimeNormalized(0.75f);
            yield return null;
            yield return null;
            Assert.IsTrue(dayNight.IsNight, "Expected nighttime after setting normalized time to 0.75.");

            float nightMultiplier = spawner.CurrentSpawnMultiplier;
            int nightMaxAlive = spawner.CurrentMaxAlive;
            Assert.Greater(nightMultiplier, dayMultiplier, "Night spawn multiplier did not increase.");
            Assert.Greater(nightMaxAlive, dayMaxAlive, "Night max alive did not increase.");
            Assert.Greater(zombie.CurrentSenseMultiplier, daySenseMultiplier, "Zombie night aggression/sense multiplier did not increase.");

            float hungerStart = stats.CurrentHunger;
            float thirstStart = stats.CurrentThirst;
            float hungerBarStart = hud.HungerBarValue;
            float thirstBarStart = hud.ThirstBarValue;

            yield return new WaitForSeconds(5f);

            Assert.Less(stats.CurrentHunger, hungerStart, "Hunger did not drain over time.");
            Assert.Less(stats.CurrentThirst, thirstStart, "Thirst did not drain over time.");

            Assert.IsFalse(string.IsNullOrWhiteSpace(hud.CurrentTimeLabel), "HUD time label is empty.");
            Assert.GreaterOrEqual(hud.HungerBarValue, 0f, "HUD hunger bar value is invalid.");
            Assert.GreaterOrEqual(hud.ThirstBarValue, 0f, "HUD thirst bar value is invalid.");
            Assert.Less(hud.HungerBarValue, hungerBarStart, "HUD hunger bar did not update.");
            Assert.Less(hud.ThirstBarValue, thirstBarStart, "HUD thirst bar did not update.");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
