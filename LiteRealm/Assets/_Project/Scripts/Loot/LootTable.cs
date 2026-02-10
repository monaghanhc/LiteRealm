using System;
using System.Collections.Generic;
using LiteRealm.Inventory;
using UnityEngine;

namespace LiteRealm.Loot
{
    [Serializable]
    public class LootEntry
    {
        public ItemDefinition Item;
        [Min(1)] public int MinAmount = 1;
        [Min(1)] public int MaxAmount = 1;
        [Min(0f)] public float Weight = 1f;
        public bool Guaranteed;
    }

    [Serializable]
    public struct LootRollResult
    {
        public ItemDefinition Item;
        public int Amount;
    }

    [CreateAssetMenu(fileName = "LootTable", menuName = "LiteRealm/Loot/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

        public List<LootRollResult> Roll(int rollCount)
        {
            Dictionary<ItemDefinition, int> totals = new Dictionary<ItemDefinition, int>();

            for (int i = 0; i < entries.Count; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || !entry.Guaranteed)
                {
                    continue;
                }

                int amount = UnityEngine.Random.Range(entry.MinAmount, entry.MaxAmount + 1);
                AddToTotals(totals, entry.Item, amount);
            }

            List<LootEntry> weighted = new List<LootEntry>();
            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || entry.Guaranteed || entry.Weight <= 0f)
                {
                    continue;
                }

                weighted.Add(entry);
                totalWeight += entry.Weight;
            }

            int rolls = Mathf.Max(0, rollCount);
            for (int i = 0; i < rolls; i++)
            {
                if (weighted.Count == 0 || totalWeight <= 0f)
                {
                    break;
                }

                float pick = UnityEngine.Random.value * totalWeight;
                float current = 0f;

                for (int j = 0; j < weighted.Count; j++)
                {
                    LootEntry candidate = weighted[j];
                    current += candidate.Weight;
                    if (pick > current)
                    {
                        continue;
                    }

                    int amount = UnityEngine.Random.Range(candidate.MinAmount, candidate.MaxAmount + 1);
                    AddToTotals(totals, candidate.Item, amount);
                    break;
                }
            }

            List<LootRollResult> results = new List<LootRollResult>();
            foreach (KeyValuePair<ItemDefinition, int> kvp in totals)
            {
                results.Add(new LootRollResult
                {
                    Item = kvp.Key,
                    Amount = kvp.Value
                });
            }

            return results;
        }

        private void AddToTotals(Dictionary<ItemDefinition, int> totals, ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return;
            }

            totals.TryGetValue(item, out int existing);
            totals[item] = existing + amount;
        }
    }
}
