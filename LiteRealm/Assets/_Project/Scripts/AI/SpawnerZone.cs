using System.Collections;
using System.Collections.Generic;
using LiteRealm.Core;
using LiteRealm.World;
using UnityEngine;

namespace LiteRealm.AI
{
    public class SpawnerZone : MonoBehaviour
    {
        [SerializeField] private ZombieAI zombiePrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Spawn Limits")]
        [SerializeField] [Min(1)] private int maxAliveDay = 8;
        [SerializeField] [Min(1)] private int maxAliveNight = 14;
        [SerializeField] [Min(1f)] private float nightSpawnMultiplier = 1.75f;
        [SerializeField] [Min(0f)] private float respawnInterval = 4f;
        [SerializeField] [Min(0)] private int initialSpawnCount = 4;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private DayNightCycleManager dayNight;

        private readonly List<ZombieAI> alive = new List<ZombieAI>();
        private float spawnTimer;

        public ZombieAI ZombiePrefab => zombiePrefab;
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
        public int AliveCount => alive.Count;
        public bool HasValidConfiguration => zombiePrefab != null && spawnPoints != null && spawnPoints.Length > 0;
        public float CurrentSpawnMultiplier => dayNight != null && dayNight.IsNight ? Mathf.Max(1f, nightSpawnMultiplier) : 1f;
        public int CurrentMaxAlive => GetCurrentMaxAlive();

        private IEnumerator Start()
        {
            if (target == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    target = playerObject.transform;
                }
            }

            // Give runtime NavMesh bootstrap one frame to build data before initial agents spawn.
            yield return null;
            SpawnInitial();
        }

        private void Update()
        {
            CleanupDeadReferences();

            if (zombiePrefab == null)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            int maxAlive = GetCurrentMaxAlive();
            if (alive.Count >= maxAlive)
            {
                return;
            }

            SpawnZombie();
            spawnTimer = respawnInterval;
        }

        public void SpawnZombie()
        {
            if (zombiePrefab == null)
            {
                return;
            }

            Transform spawnPoint = GetNextSpawnPoint();
            ZombieAI zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
            zombie.Initialize(target, eventHub, dayNight);
            zombie.ZombieDied += OnZombieDied;
            alive.Add(zombie);
        }

        public void SpawnBatch(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnZombie();
            }
        }

        private void SpawnInitial()
        {
            int initial = Mathf.Max(0, initialSpawnCount);
            for (int i = 0; i < initial; i++)
            {
                SpawnZombie();
            }
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int index = Random.Range(0, spawnPoints.Length);
                return spawnPoints[index] != null ? spawnPoints[index] : transform;
            }

            return transform;
        }

        private void OnZombieDied(ZombieAI zombie)
        {
            alive.Remove(zombie);
            if (zombie != null)
            {
                zombie.ZombieDied -= OnZombieDied;
            }
        }

        private void CleanupDeadReferences()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (alive[i] == null)
                {
                    alive.RemoveAt(i);
                }
            }
        }

        private int GetCurrentMaxAlive()
        {
            if (dayNight == null || !dayNight.IsNight)
            {
                return Mathf.Max(1, maxAliveDay);
            }

            int multiplied = Mathf.RoundToInt(maxAliveDay * Mathf.Max(1f, nightSpawnMultiplier));
            int nightCap = Mathf.Max(maxAliveNight, multiplied);
            return Mathf.Max(1, nightCap);
        }
    }
}
