using System.Collections.Generic;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LiteRealm.Tests.Editor
{
    public class LootAndMapEditModeTests
    {
        private const string ItemDatabasePath = "Assets/_Project/ScriptableObjects/Items/ItemDatabase.asset";
        private const string ZombieLootTablePath = "Assets/_Project/ScriptableObjects/LootTables/LootTable_Zombie_Common.asset";
        private const string ContainerLootTablePath = "Assets/_Project/ScriptableObjects/LootTables/LootTable_Container_Common.asset";

        [Test]
        public void ItemDatabase_ContainsZombieResourceAndLootableWeapons()
        {
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

            Assert.IsNotNull(database, $"Missing item database at {ItemDatabasePath}.");
            Assert.IsNotNull(database.GetById("item.material.infected_residue"));
            Assert.IsNotNull(database.GetById("item.weapon.ranger_rifle"));
            Assert.IsNotNull(database.GetById("item.weapon.pump_shotgun"));
            Assert.IsNotNull(database.GetById("item.weapon.service_pistol"));
        }

        [Test]
        public void ZombieLootTable_RollZero_StillDropsResource()
        {
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(ZombieLootTablePath);

            Assert.IsNotNull(table, $"Missing zombie loot table at {ZombieLootTablePath}.");
            HashSet<string> itemIds = RollItemIds(table, 0);

            Assert.Contains("item.material.infected_residue", new List<string>(itemIds));
            Assert.Contains("item.material.scrap", new List<string>(itemIds));
        }

        [Test]
        public void ContainerLootTable_HasGuaranteedSuppliesAndWeaponPool()
        {
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(ContainerLootTablePath);

            Assert.IsNotNull(table, $"Missing container loot table at {ContainerLootTablePath}.");
            HashSet<string> guaranteedIds = RollItemIds(table, 0);
            HashSet<string> poolIds = GetEntryItemIds(table);

            Assert.Contains("item.material.scrap", new List<string>(guaranteedIds));
            Assert.Contains("item.ammo.rifle", new List<string>(guaranteedIds));
            Assert.Contains("item.consumable.medkit", new List<string>(guaranteedIds));
            Assert.Contains("item.weapon.ranger_rifle", new List<string>(poolIds));
            Assert.Contains("item.weapon.pump_shotgun", new List<string>(poolIds));
            Assert.Contains("item.weapon.service_pistol", new List<string>(poolIds));
        }

        [Test]
        public void MapWorldPosition_ClampsIntoMapBounds()
        {
            GameObject host = new GameObject("MapTest");
            try
            {
                MapUIController map = host.AddComponent<MapUIController>();

                Vector2 center = map.WorldToMapAnchoredPosition(new Vector3(300f, 0f, 300f), new Vector2(720f, 720f));
                Vector2 farOutside = map.WorldToMapAnchoredPosition(new Vector3(9999f, 0f, -9999f), new Vector2(720f, 720f));

                Assert.That(center.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(center.y, Is.EqualTo(0f).Within(0.01f));
                Assert.That(farOutside.x, Is.EqualTo(360f).Within(0.01f));
                Assert.That(farOutside.y, Is.EqualTo(-360f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static HashSet<string> RollItemIds(LootTable table, int rollCount)
        {
            List<LootRollResult> results = table.Roll(rollCount);
            HashSet<string> itemIds = new HashSet<string>();
            for (int i = 0; i < results.Count; i++)
            {
                ItemDefinition item = results[i].Item;
                if (item != null)
                {
                    itemIds.Add(item.ItemId);
                }
            }

            return itemIds;
        }

        private static HashSet<string> GetEntryItemIds(LootTable table)
        {
            SerializedObject serializedTable = new SerializedObject(table);
            SerializedProperty entries = serializedTable.FindProperty("entries");
            HashSet<string> itemIds = new HashSet<string>();
            if (entries == null)
            {
                return itemIds;
            }

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty itemProperty = entry.FindPropertyRelative("Item");
                ItemDefinition item = itemProperty != null ? itemProperty.objectReferenceValue as ItemDefinition : null;
                if (item == null)
                {
                    continue;
                }

                itemIds.Add(item.ItemId);
            }

            return itemIds;
        }
    }
}
