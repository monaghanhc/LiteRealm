using System;
using LiteRealm.World;
using UnityEngine;

namespace LiteRealm.AI
{
    [Serializable]
    public struct BossSpawnerState
    {
        public int LastSpawnedDay;
        public bool BossAlive;
    }

    public class BossSpawnManager : MonoBehaviour
    {
        [SerializeField] private BossAI bossPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool spawnAtNightOnly = true;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private LiteRealm.Core.GameEventHub eventHub;
        [SerializeField] private DayNightCycleManager dayNight;

        public BossAI ActiveBoss { get; private set; }
        public int LastSpawnedDay { get; private set; }
        public BossAI BossPrefab => bossPrefab;
        public Transform SpawnPoint => spawnPoint;

        private bool forceSpawnRequested;

        private void Start()
        {
            if (target == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    target = playerObject.transform;
                }
            }
        }

        private void Update()
        {
            if (bossPrefab == null || dayNight == null)
            {
                return;
            }

            if (ActiveBoss != null)
            {
                return;
            }

            bool newDayAvailable = dayNight.CurrentDay > LastSpawnedDay;
            bool nightCondition = !spawnAtNightOnly || dayNight.IsNight;

            if (forceSpawnRequested || (newDayAvailable && nightCondition))
            {
                SpawnBoss();
                forceSpawnRequested = false;
            }
        }

        public void RequestQuestSpawn()
        {
            forceSpawnRequested = true;
        }

        public void SpawnBoss(bool updateSpawnDay = true)
        {
            if (bossPrefab == null || ActiveBoss != null)
            {
                return;
            }

            Transform spawn = spawnPoint != null ? spawnPoint : transform;
            ActiveBoss = Instantiate(bossPrefab, spawn.position, spawn.rotation);
            ActiveBoss.Initialize(target, eventHub, dayNight);
            ActiveBoss.BossDied += OnBossDied;
            if (updateSpawnDay)
            {
                LastSpawnedDay = dayNight != null ? dayNight.CurrentDay : LastSpawnedDay;
            }
        }

        public BossSpawnerState CaptureState()
        {
            return new BossSpawnerState
            {
                LastSpawnedDay = LastSpawnedDay,
                BossAlive = ActiveBoss != null
            };
        }

        public void RestoreState(BossSpawnerState state)
        {
            LastSpawnedDay = Mathf.Max(0, state.LastSpawnedDay);

            if (ActiveBoss != null)
            {
                ActiveBoss.BossDied -= OnBossDied;
                Destroy(ActiveBoss.gameObject);
                ActiveBoss = null;
            }

            if (state.BossAlive)
            {
                SpawnBoss(false);
            }
        }

        private void OnBossDied(BossAI boss)
        {
            if (boss != null)
            {
                boss.BossDied -= OnBossDied;
            }

            ActiveBoss = null;
        }
    }
}
