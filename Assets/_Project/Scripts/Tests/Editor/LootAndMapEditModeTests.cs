using System.Collections.Generic;
using System.Reflection;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
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

        [Test]
        public void DroppedPickup_CanCollectIntoInventory()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_InfectedResidue.asset");
            Assert.IsNotNull(item);

            GameObject player = new GameObject("Player");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                InventoryComponent inventory = player.AddComponent<InventoryComponent>();
                InvokeLifecycle(inventory, "Awake");
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
                SerializedObject interactorSo = new SerializedObject(interactor);
                SetObject(interactorSo, "inventory", inventory);
                interactorSo.ApplyModifiedPropertiesWithoutUndo();

                WorldItemPickup pickup = pickupObject.AddComponent<WorldItemPickup>();
                pickup.Configure(item, 2);

                Assert.IsTrue(pickup.TryCollect(interactor));
                Assert.AreEqual(2, inventory.GetItemCount(item.ItemId));
            }
            finally
            {
                Object.DestroyImmediate(player);
                if (pickupObject != null)
                {
                    Object.DestroyImmediate(pickupObject);
                }
            }
        }

        [Test]
        public void PlayerInteractor_ProximityFallbackFindsNearbyPickup()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_InfectedResidue.asset");
            Assert.IsNotNull(item);

            GameObject player = new GameObject("Player");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                InventoryComponent inventory = player.AddComponent<InventoryComponent>();
                InvokeLifecycle(inventory, "Awake");
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();

                SerializedObject interactorSo = new SerializedObject(interactor);
                SetObject(interactorSo, "inventory", inventory);
                int interactableLayer = LayerMask.NameToLayer("Interactable");
                Assert.GreaterOrEqual(interactableLayer, 0, "Interactable layer is missing.");
                SetLayerMask(interactorSo, "interactionMask", 1 << interactableLayer);
                interactorSo.ApplyModifiedPropertiesWithoutUndo();

                pickupObject.transform.position = new Vector3(1f, 0f, 0f);
                WorldItemPickup pickup = pickupObject.AddComponent<WorldItemPickup>();
                pickup.Configure(item, 1);
                Physics.SyncTransforms();

                InvokeLifecycle(interactor, "Awake");
                InvokeLifecycle(interactor, "FindInteractable");

                Assert.AreSame(pickup, interactor.CurrentInteractable);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void InventoryUi_BuildsReadableSummary()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_InfectedResidue.asset");
            Assert.IsNotNull(item);

            GameObject player = new GameObject("InventoryPlayer");
            GameObject canvasObject = new GameObject("Canvas");
            try
            {
                InventoryComponent inventory = player.AddComponent<InventoryComponent>();
                InvokeLifecycle(inventory, "Awake");
                inventory.AddItemAndReturnAccepted(item, 3);

                canvasObject.AddComponent<Canvas>();
                InventoryUIController ui = canvasObject.AddComponent<InventoryUIController>();
                ui.Bind(inventory, null);

                string summary = ui.BuildInventorySummary();
                StringAssert.Contains("Infected Residue x3", summary);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CharacterCustomization_ClampsInvalidIndexes()
        {
            CharacterCustomizationData data = new CharacterCustomizationData
            {
                SkinToneIndex = 99,
                HairStyleIndex = -4,
                HairColorIndex = 99,
                OutfitColorIndex = -2
            };

            CharacterCustomizationData clamped = CharacterCustomizationState.Clamp(data);

            Assert.That(clamped.SkinToneIndex, Is.InRange(0, CharacterCustomizationState.SkinTones.Length - 1));
            Assert.That(clamped.HairStyleIndex, Is.InRange(0, CharacterCustomizationState.HairStyleNames.Length - 1));
            Assert.That(clamped.HairColorIndex, Is.InRange(0, CharacterCustomizationState.HairColors.Length - 1));
            Assert.That(clamped.OutfitColorIndex, Is.InRange(0, CharacterCustomizationState.OutfitColors.Length - 1));
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

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = null;
            System.Type type = target.GetType();
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.IsNotNull(method, $"Missing method {methodName} on {target.GetType().Name}.");
            method.Invoke(target, null);
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.objectReferenceValue = value;
        }

        private static void SetLayerMask(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.intValue = value;
        }
    }
}
