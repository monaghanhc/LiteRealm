using System.Collections;
using LiteRealm.AI;
using LiteRealm.Combat;
using LiteRealm.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class CombatEnemyPlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator CombatSmoke_ZombiePaths_PlayerCanKill_NoExceptionsFor30Seconds()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");

            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            Assert.IsNotNull(player, "Player missing in Main scene.");

            WeaponManager weaponManager = player.GetComponent<WeaponManager>();
            Assert.IsNotNull(weaponManager, "WeaponManager missing on Player.");
            Assert.IsNotNull(weaponManager.ActiveWeapon, "Player has no active weapon configured.");

            SpawnerZone spawner = Object.FindObjectOfType<SpawnerZone>();
            Assert.IsNotNull(spawner, "SpawnerZone missing from Main scene.");

            int beforeCount = Object.FindObjectsOfType<ZombieAI>(true).Length;
            spawner.SpawnZombie();
            yield return null;
            yield return new WaitForSeconds(1f);

            ZombieAI[] zombies = Object.FindObjectsOfType<ZombieAI>(true);
            Assert.Greater(zombies.Length, beforeCount, "Spawner did not create a zombie instance.");

            ZombieAI zombie = FindClosestZombie(player.transform.position, zombies);
            Assert.IsNotNull(zombie, "Could not resolve spawned zombie reference.");

            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            Assert.IsNotNull(agent, "Spawned zombie is missing NavMeshAgent.");

            float startDistance = Vector3.Distance(zombie.transform.position, player.transform.position);
            float chaseTimer = 4f;
            while (chaseTimer > 0f)
            {
                chaseTimer -= Time.deltaTime;
                yield return null;
            }

            float endDistance = Vector3.Distance(zombie.transform.position, player.transform.position);
            bool movedTowardPlayer = endDistance < (startDistance - 0.25f);
            bool hasPathIntent = agent.hasPath || agent.pathPending;
            Assert.IsTrue(movedTowardPlayer || hasPathIntent,
                $"Zombie did not chase player. start={startDistance:F2}, end={endDistance:F2}, hasPath={agent.hasPath}");

            Camera cam = Camera.main;
            Assert.IsNotNull(cam, "Main camera missing.");

            Vector3 killPosition = cam.transform.position + cam.transform.forward * 6f;
            killPosition.y = Mathf.Max(killPosition.y, player.transform.position.y + 0.3f);
            if (agent.enabled)
            {
                agent.Warp(killPosition);
                agent.isStopped = true;
            }
            else
            {
                zombie.transform.position = killPosition;
            }

            HealthComponent zombieHealth = zombie.GetComponent<HealthComponent>();
            Assert.IsNotNull(zombieHealth, "Spawned zombie missing HealthComponent.");

            float fireTimeout = 10f;
            while (fireTimeout > 0f && !zombieHealth.IsDead)
            {
                weaponManager.TryFireActiveWeapon();
                fireTimeout -= 0.12f;
                yield return new WaitForSeconds(0.12f);
            }

            Assert.IsTrue(zombieHealth.IsDead, "Zombie did not die after repeated weapon fire.");

            float simulationTimer = 0f;
            while (simulationTimer < 30f)
            {
                simulationTimer += Time.deltaTime;
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static ZombieAI FindClosestZombie(Vector3 position, ZombieAI[] zombies)
        {
            ZombieAI best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < zombies.Length; i++)
            {
                ZombieAI candidate = zombies[i];
                if (candidate == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(candidate.transform.position, position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }
    }
}
