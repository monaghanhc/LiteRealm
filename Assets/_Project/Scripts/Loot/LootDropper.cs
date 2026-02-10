using System;
using System.Collections.Generic;
using LiteRealm.Core;
using LiteRealm.Inventory;
using UnityEngine;

namespace LiteRealm.Loot
{
    [Serializable]
    public class GuaranteedDrop
    {
        public ItemDefinition Item;
        [Min(1)] public int Amount = 1;
    }

    [RequireComponent(typeof(HealthComponent))]
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField] [Min(0)] private int rollCount = 2;
        [SerializeField] private List<GuaranteedDrop> guaranteedDrops = new List<GuaranteedDrop>();
        [SerializeField] private GameObject fallbackPickupPrefab;

        private HealthComponent health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied()
        {
            List<LootRollResult> results = new List<LootRollResult>();

            if (lootTable != null)
            {
                results.AddRange(lootTable.Roll(rollCount));
            }

            for (int i = 0; i < guaranteedDrops.Count; i++)
            {
                GuaranteedDrop drop = guaranteedDrops[i];
                if (drop == null || drop.Item == null || drop.Amount <= 0)
                {
                    continue;
                }

                results.Add(new LootRollResult
                {
                    Item = drop.Item,
                    Amount = drop.Amount
                });
            }

            for (int i = 0; i < results.Count; i++)
            {
                SpawnPickup(results[i], i);
            }
        }

        private void SpawnPickup(LootRollResult result, int index)
        {
            if (result.Item == null || result.Amount <= 0)
            {
                return;
            }

            Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.7f;
            offset.y = 0.2f;
            Vector3 spawnPosition = transform.position + offset + Vector3.right * (index * 0.1f);

            GameObject prefab = result.Item.WorldPickupPrefab != null
                ? result.Item.WorldPickupPrefab
                : fallbackPickupPrefab;

            WorldItemPickup pickup;
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab, spawnPosition, Quaternion.identity);
                pickup = go.GetComponent<WorldItemPickup>();
                if (pickup == null)
                {
                    pickup = go.AddComponent<WorldItemPickup>();
                }
            }
            else
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.position = spawnPosition;
                go.transform.localScale = new Vector3(0.35f, 0.2f, 0.35f);
                pickup = go.AddComponent<WorldItemPickup>();
            }

            pickup.Configure(result.Item, result.Amount);
        }
    }
}
