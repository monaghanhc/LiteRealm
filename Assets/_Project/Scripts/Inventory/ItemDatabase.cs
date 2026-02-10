using System.Collections.Generic;
using UnityEngine;

namespace LiteRealm.Inventory
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "LiteRealm/Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> lookup;

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition GetById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            EnsureLookup();
            lookup.TryGetValue(itemId, out ItemDefinition item);
            return item;
        }

        private void OnValidate()
        {
            lookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, ItemDefinition>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                lookup[item.ItemId] = item;
            }
        }
    }
}
