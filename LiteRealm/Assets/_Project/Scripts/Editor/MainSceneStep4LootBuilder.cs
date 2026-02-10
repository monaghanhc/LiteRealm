#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using LiteRealm.Saving;
using LiteRealm.UI;
using LiteRealm.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.EditorTools
{
    public static class MainSceneStep4LootBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ItemsDirectory = "Assets/_Project/ScriptableObjects/Items";
        private const string LootTablesDirectory = "Assets/_Project/ScriptableObjects/LootTables";
        private const string LootPrefabsDirectory = "Assets/_Project/Prefabs/Loot";

        private const string WaterItemPath = ItemsDirectory + "/Item_WaterBottle.asset";
        private const string FoodItemPath = ItemsDirectory + "/Item_CannedFood.asset";
        private const string MedkitItemPath = ItemsDirectory + "/Item_Medkit.asset";
        private const string ScrapItemPath = ItemsDirectory + "/Item_Scrap.asset";
        private const string AmmoItemPath = ItemsDirectory + "/Item_RifleAmmo.asset";
        private const string BossTokenItemPath = ItemsDirectory + "/Item_BossToken.asset";
        private const string ItemDatabasePath = ItemsDirectory + "/ItemDatabase.asset";

        private const string ContainerLootTablePath = LootTablesDirectory + "/LootTable_Container_Common.asset";
        private const string ZombieLootTablePath = LootTablesDirectory + "/LootTable_Zombie_Common.asset";
        private const string BossLootTablePath = LootTablesDirectory + "/LootTable_Boss_Special.asset";

        private const string WorldPickupPrefabPath = LootPrefabsDirectory + "/WorldItemPickup.prefab";
        private const string LootContainerPrefabPath = LootPrefabsDirectory + "/LootCrate.prefab";

        private const string ZombiePrefabPath = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";
        private const string BossPrefabPath = "Assets/_Project/Prefabs/Enemies/Boss.prefab";

        [MenuItem("Tools/LiteRealm/Scenes/Apply Step 4 (Looting Loop)")]
        public static void ApplyStep4()
        {
            MainSceneStep3SurvivalBuilder.ApplyStep3();
            EnsureDirectory(ItemsDirectory);
            EnsureDirectory(LootTablesDirectory);
            EnsureDirectory(LootPrefabsDirectory);

            ItemDefinition water = GetOrCreateItem(
                WaterItemPath,
                "item.consumable.water_bottle",
                "Water Bottle",
                "Clean water that restores thirst.",
                ItemRarity.Common,
                10,
                true,
                0f,
                0f,
                0f,
                35f,
                false);

            ItemDefinition food = GetOrCreateItem(
                FoodItemPath,
                "item.consumable.canned_food",
                "Canned Food",
                "Shelf-stable food that restores hunger.",
                ItemRarity.Common,
                10,
                true,
                0f,
                0f,
                30f,
                0f,
                false);

            ItemDefinition medkit = GetOrCreateItem(
                MedkitItemPath,
                "item.consumable.medkit",
                "Medkit",
                "Emergency medical kit that restores health.",
                ItemRarity.Uncommon,
                5,
                true,
                45f,
                0f,
                0f,
                0f,
                false);

            ItemDefinition scrap = GetOrCreateItem(
                ScrapItemPath,
                "item.material.scrap",
                "Scrap Metal",
                "Salvage material used for crafting later.",
                ItemRarity.Common,
                30,
                false,
                0f,
                0f,
                0f,
                0f,
                false);

            ItemDefinition ammo = GetOrCreateItem(
                AmmoItemPath,
                "item.ammo.rifle",
                "Rifle Ammo",
                "Standard rifle rounds.",
                ItemRarity.Common,
                60,
                false,
                0f,
                0f,
                0f,
                0f,
                false);

            ItemDefinition bossToken = GetOrCreateItem(
                BossTokenItemPath,
                "item.special.boss_token",
                "Mutant Core Token",
                "A volatile token dropped by the Alpha boss.",
                ItemRarity.Epic,
                5,
                false,
                0f,
                0f,
                0f,
                0f,
                true);

            List<ItemDefinition> allItems = new List<ItemDefinition> { water, food, medkit, scrap, ammo, bossToken };
            ItemDatabase itemDatabase = GetOrCreateItemDatabase(allItems);

            LootTable containerTable = GetOrCreateLootTable(ContainerLootTablePath, new[]
            {
                new LootEntrySeed(scrap, 1, 3, 0f, true),
                new LootEntrySeed(water, 1, 2, 1.1f, false),
                new LootEntrySeed(food, 1, 2, 1.1f, false),
                new LootEntrySeed(medkit, 1, 1, 0.35f, false),
                new LootEntrySeed(ammo, 8, 20, 0.9f, false)
            });

            LootTable zombieTable = GetOrCreateLootTable(ZombieLootTablePath, new[]
            {
                new LootEntrySeed(scrap, 1, 2, 0f, true),
                new LootEntrySeed(water, 1, 1, 0.45f, false),
                new LootEntrySeed(food, 1, 1, 0.45f, false),
                new LootEntrySeed(ammo, 4, 12, 0.5f, false)
            });

            LootTable bossTable = GetOrCreateLootTable(BossLootTablePath, new[]
            {
                new LootEntrySeed(medkit, 1, 2, 0f, true),
                new LootEntrySeed(ammo, 16, 32, 1f, false),
                new LootEntrySeed(food, 2, 3, 0.8f, false)
            });

            GameObject pickupPrefab = GetOrCreateWorldPickupPrefab();
            AssignPickupPrefab(allItems, pickupPrefab);

            GameObject lootCratePrefab = GetOrCreateLootCratePrefab(containerTable);
            ConfigureEnemyDropPrefabs(zombieTable, bossTable, bossToken, pickupPrefab);

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            GameObject appRoot = GetOrCreateRoot(scene, "__App");
            GameObject worldRoot = GetOrCreateRoot(scene, "World");
            GameObject canvasRoot = GetOrCreateRoot(scene, "UI Canvas");

            EnsureCanvasComponents(canvasRoot);
            LootUIController lootUi = EnsureLootUi(canvasRoot);
            ConfigurePlayerReferences(scene, lootUi);
            PlaceWorldContainers(scene, worldRoot.transform, lootCratePrefab, containerTable);
            ConfigureSaveAndDebug(scene, appRoot, itemDatabase);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Step 4 looting loop setup applied.");
        }

        [InitializeOnLoadMethod]
        private static void AutoApplyIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                Scene active = SceneManager.GetActiveScene();
                if (active.IsValid() && active.isDirty)
                {
                    return;
                }

                bool missing = !File.Exists(MainScenePath)
                               || !File.Exists(WaterItemPath)
                               || !File.Exists(FoodItemPath)
                               || !File.Exists(MedkitItemPath)
                               || !File.Exists(ContainerLootTablePath)
                               || !File.Exists(LootContainerPrefabPath);

                if (missing)
                {
                    ApplyStep4();
                }
            };
        }

        private static ItemDefinition GetOrCreateItem(
            string path,
            string itemId,
            string displayName,
            string description,
            ItemRarity rarity,
            int maxStack,
            bool consumable,
            float health,
            float stamina,
            float hunger,
            float thirst,
            bool questItem)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject so = new SerializedObject(item);
            SetString(so, "itemId", itemId);
            SetString(so, "displayName", displayName);
            SetString(so, "description", description);
            SetInt(so, "rarity", (int)rarity);
            SetInt(so, "maxStack", Mathf.Max(1, maxStack));
            SetBool(so, "consumable", consumable);
            SetFloat(so, "healthRestore", health);
            SetFloat(so, "staminaRestore", stamina);
            SetFloat(so, "hungerRestore", hunger);
            SetFloat(so, "thirstRestore", thirst);
            SetBool(so, "questItem", questItem);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static ItemDatabase GetOrCreateItemDatabase(List<ItemDefinition> items)
        {
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(database, ItemDatabasePath);
            }

            SerializedObject so = new SerializedObject(database);
            SerializedProperty itemList = so.FindProperty("items");
            if (itemList != null)
            {
                itemList.arraySize = items.Count;
                for (int i = 0; i < items.Count; i++)
                {
                    itemList.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static LootTable GetOrCreateLootTable(string path, LootEntrySeed[] entries)
        {
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LootTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            SerializedObject so = new SerializedObject(table);
            SerializedProperty entryList = so.FindProperty("entries");
            if (entryList != null)
            {
                entryList.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    LootEntrySeed seed = entries[i];
                    SerializedProperty entry = entryList.GetArrayElementAtIndex(i);
                    SerializedProperty item = entry.FindPropertyRelative("Item");
                    SerializedProperty minAmount = entry.FindPropertyRelative("MinAmount");
                    SerializedProperty maxAmount = entry.FindPropertyRelative("MaxAmount");
                    SerializedProperty weight = entry.FindPropertyRelative("Weight");
                    SerializedProperty guaranteed = entry.FindPropertyRelative("Guaranteed");

                    if (item != null)
                    {
                        item.objectReferenceValue = seed.Item;
                    }

                    if (minAmount != null)
                    {
                        minAmount.intValue = Mathf.Max(1, seed.MinAmount);
                    }

                    if (maxAmount != null)
                    {
                        maxAmount.intValue = Mathf.Max(seed.MinAmount, seed.MaxAmount);
                    }

                    if (weight != null)
                    {
                        weight.floatValue = Mathf.Max(0f, seed.Weight);
                    }

                    if (guaranteed != null)
                    {
                        guaranteed.boolValue = seed.Guaranteed;
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static GameObject GetOrCreateWorldPickupPrefab()
        {
            if (!File.Exists(WorldPickupPrefabPath))
            {
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                root.name = "WorldItemPickup";
                root.transform.localScale = Vector3.one * 0.45f;
                root.AddComponent<WorldItemPickup>();
                PrefabUtility.SaveAsPrefabAsset(root, WorldPickupPrefabPath);
                Object.DestroyImmediate(root);
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(WorldPickupPrefabPath);
            try
            {
                WorldItemPickup pickup = prefabRoot.GetComponent<WorldItemPickup>();
                if (pickup == null)
                {
                    pickup = prefabRoot.AddComponent<WorldItemPickup>();
                }

                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer >= 0)
                {
                    prefabRoot.layer = interactableLayer;
                }

                if (ProjectDoctorRunner.HasTag("Loot"))
                {
                    prefabRoot.tag = "Loot";
                }

                Renderer renderer = prefabRoot.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (renderer.sharedMaterial == null)
                    {
                        renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    }

                    renderer.sharedMaterial.color = new Color(0.92f, 0.84f, 0.35f);
                }
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, WorldPickupPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(WorldPickupPrefabPath);
        }

        private static void AssignPickupPrefab(List<ItemDefinition> items, GameObject pickupPrefab)
        {
            if (pickupPrefab == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null)
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(item);
                SetObject(so, "worldPickupPrefab", pickupPrefab);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
        }

        private static GameObject GetOrCreateLootCratePrefab(LootTable containerLootTable)
        {
            if (!File.Exists(LootContainerPrefabPath))
            {
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
                root.name = "LootCrate";
                root.transform.localScale = new Vector3(1f, 0.8f, 1f);
                root.AddComponent<LootContainer>();
                PrefabUtility.SaveAsPrefabAsset(root, LootContainerPrefabPath);
                Object.DestroyImmediate(root);
            }

            GameObject instanceRoot = PrefabUtility.LoadPrefabContents(LootContainerPrefabPath);
            try
            {
                LootContainer container = instanceRoot.GetComponent<LootContainer>();
                if (container == null)
                {
                    container = instanceRoot.AddComponent<LootContainer>();
                }

                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer >= 0)
                {
                    instanceRoot.layer = interactableLayer;
                }

                if (ProjectDoctorRunner.HasTag("Loot"))
                {
                    instanceRoot.tag = "Loot";
                }

                SerializedObject so = new SerializedObject(container);
                SetString(so, "containerId", "container.prefab.loot_crate");
                SetObject(so, "lootTable", containerLootTable);
                SetInt(so, "rollCount", 3);
                SetBool(so, "singleUse", true);
                SetBool(so, "startsOpened", false);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(instanceRoot, LootContainerPrefabPath);
                PrefabUtility.UnloadPrefabContents(instanceRoot);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(LootContainerPrefabPath);
        }

        private static void ConfigureEnemyDropPrefabs(
            LootTable zombieTable,
            LootTable bossTable,
            ItemDefinition bossToken,
            GameObject fallbackPickup)
        {
            ConfigureDropperOnPrefab(ZombiePrefabPath, zombieTable, 1, fallbackPickup, null);
            ConfigureDropperOnPrefab(BossPrefabPath, bossTable, 1, fallbackPickup, bossToken);
        }

        private static void ConfigureDropperOnPrefab(
            string prefabPath,
            LootTable table,
            int rollCount,
            GameObject fallbackPickup,
            ItemDefinition guaranteedItem)
        {
            if (!File.Exists(prefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                LootDropper dropper = root.GetComponent<LootDropper>();
                if (dropper == null)
                {
                    dropper = root.AddComponent<LootDropper>();
                }

                SerializedObject so = new SerializedObject(dropper);
                SetObject(so, "lootTable", table);
                SetInt(so, "rollCount", Mathf.Max(0, rollCount));
                SetObject(so, "fallbackPickupPrefab", fallbackPickup);

                SerializedProperty guaranteed = so.FindProperty("guaranteedDrops");
                if (guaranteed != null)
                {
                    if (guaranteedItem == null)
                    {
                        guaranteed.arraySize = 0;
                    }
                    else
                    {
                        guaranteed.arraySize = 1;
                        SerializedProperty entry = guaranteed.GetArrayElementAtIndex(0);
                        SerializedProperty item = entry.FindPropertyRelative("Item");
                        SerializedProperty amount = entry.FindPropertyRelative("Amount");
                        if (item != null)
                        {
                            item.objectReferenceValue = guaranteedItem;
                        }

                        if (amount != null)
                        {
                            amount.intValue = 1;
                        }
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePlayerReferences(Scene scene, LootUIController lootUi)
        {
            GameObject player = FindPlayer(scene);
            if (player == null)
            {
                return;
            }

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            if (inventory == null)
            {
                inventory = player.AddComponent<InventoryComponent>();
            }

            SerializedObject inventorySo = new SerializedObject(inventory);
            SetInt(inventorySo, "slotCount", 24);
            SetInt(inventorySo, "hotbarSlotCount", 4);
            inventorySo.ApplyModifiedPropertiesWithoutUndo();

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor != null)
            {
                GameEventHub eventHub = FindInScene<GameEventHub>(scene);
                SerializedObject interactorSo = new SerializedObject(interactor);
                SetObject(interactorSo, "inventory", inventory);
                SetObject(interactorSo, "lootUI", lootUi);
                SetObject(interactorSo, "eventHub", eventHub);

                Camera sceneCamera = FindMainCamera(scene);
                if (sceneCamera != null)
                {
                    SetObject(interactorSo, "interactionCamera", sceneCamera);
                }

                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer >= 0)
                {
                    SetInt(interactorSo, "interactionMask", 1 << interactableLayer);
                }

                interactorSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SurvivalHUDController hud = FindInScene<SurvivalHUDController>(scene);
            if (hud != null)
            {
                SerializedObject hudSo = new SerializedObject(hud);
                SetObject(hudSo, "inventory", inventory);
                hudSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static LootUIController EnsureLootUi(GameObject canvasRoot)
        {
            GameObject panel = GetOrCreateUiChild(canvasRoot.transform, "LootPanel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 340f);
            panelRect.anchoredPosition = Vector2.zero;

            Image background = GetOrAddComponent<Image>(panel);
            background.color = new Color(0.06f, 0.08f, 0.1f, 0.9f);

            Text title = CreateOrGetText(
                panel.transform,
                "TitleText",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(360f, 30f),
                new Vector2(0f, -20f),
                20,
                TextAnchor.MiddleCenter,
                Color.white);
            title.text = "Container";

            Text items = CreateOrGetText(
                panel.transform,
                "ItemsText",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-40f, 200f),
                new Vector2(0f, -60f),
                16,
                TextAnchor.UpperLeft,
                Color.white);
            items.text = "Loot";

            Button takeAll = CreateOrGetButton(panel.transform, "TakeAllButton", "Take All", new Vector2(-90f, 26f));
            Button close = CreateOrGetButton(panel.transform, "CloseButton", "Close", new Vector2(90f, 26f));

            LootUIController controller = panel.GetComponent<LootUIController>();
            if (controller == null)
            {
                controller = panel.AddComponent<LootUIController>();
            }

            SerializedObject so = new SerializedObject(controller);
            SetObject(so, "root", panel);
            SetObject(so, "titleText", title);
            SetObject(so, "itemsText", items);
            SetObject(so, "takeAllButton", takeAll);
            SetObject(so, "closeButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static void PlaceWorldContainers(
            Scene scene,
            Transform worldRoot,
            GameObject containerPrefab,
            LootTable containerTable)
        {
            if (containerPrefab == null)
            {
                return;
            }

            Transform root = GetOrCreateChild(worldRoot, "LootContainerSpawns").transform;
            Terrain terrain = FindInScene<Terrain>(scene);

            Vector3[] positions =
            {
                new Vector3(102f, 0f, 126f),
                new Vector3(226f, 0f, 205f),
                new Vector3(352f, 0f, 292f),
                new Vector3(494f, 0f, 492f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                string name = $"Crate_{i + 1:00}";
                Transform existing = root.Find(name);
                GameObject crate = existing != null
                    ? existing.gameObject
                    : (PrefabUtility.InstantiatePrefab(containerPrefab) as GameObject);

                if (crate == null)
                {
                    continue;
                }

                crate.name = name;
                crate.transform.SetParent(root, true);
                crate.transform.position = ToSurface(terrain, positions[i], 0.45f);
                crate.transform.rotation = Quaternion.Euler(0f, 35f * i, 0f);

                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer >= 0)
                {
                    crate.layer = interactableLayer;
                }

                if (ProjectDoctorRunner.HasTag("Loot"))
                {
                    crate.tag = "Loot";
                }

                LootContainer container = crate.GetComponent<LootContainer>();
                if (container == null)
                {
                    container = crate.AddComponent<LootContainer>();
                }

                SerializedObject so = new SerializedObject(container);
                SetString(so, "containerId", $"container.world.{i + 1:00}");
                SetObject(so, "lootTable", containerTable);
                SetInt(so, "rollCount", 3);
                SetBool(so, "singleUse", true);
                SetBool(so, "startsOpened", false);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureSaveAndDebug(Scene scene, GameObject appRoot, ItemDatabase itemDatabase)
        {
            GameObject player = FindPlayer(scene);
            PlayerStats playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
            InventoryComponent inventory = player != null ? player.GetComponent<InventoryComponent>() : null;
            CharacterController playerController = player != null ? player.GetComponent<CharacterController>() : null;

            SaveSystem saveSystem = FindInScene<SaveSystem>(scene);
            if (saveSystem == null)
            {
                GameObject saveRoot = GetOrCreateChild(appRoot.transform, "SaveSystem");
                saveSystem = GetOrAddComponent<SaveSystem>(saveRoot);
            }

            if (saveSystem != null)
            {
                LootContainer[] containers = Object.FindObjectsOfType<LootContainer>(true);
                LiteRealm.Quests.QuestManager questManager = FindInScene<LiteRealm.Quests.QuestManager>(scene);
                DayNightCycleManager dayNight = FindInScene<DayNightCycleManager>(scene);
                BossSpawnManager bossSpawnManager = FindInScene<BossSpawnManager>(scene);

                SerializedObject so = new SerializedObject(saveSystem);
                SetObject(so, "playerRoot", player != null ? player.transform : null);
                SetObject(so, "playerCharacterController", playerController);
                SetObject(so, "playerStats", playerStats);
                SetObject(so, "inventory", inventory);
                SetObject(so, "itemDatabase", itemDatabase);
                SetObject(so, "questManager", questManager);
                SetObject(so, "dayNightCycle", dayNight);
                SetObject(so, "bossSpawnManager", bossSpawnManager);

                SerializedProperty containerArray = so.FindProperty("lootContainers");
                if (containerArray != null)
                {
                    containerArray.arraySize = containers.Length;
                    for (int i = 0; i < containers.Length; i++)
                    {
                        containerArray.GetArrayElementAtIndex(i).objectReferenceValue = containers[i];
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            DebugPanelController debug = FindInScene<DebugPanelController>(scene);
            if (debug != null)
            {
                SerializedObject so = new SerializedObject(debug);
                SetObject(so, "inventory", inventory);
                SetObject(so, "itemDatabase", itemDatabase);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Text CreateOrGetText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPos,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject go = GetOrCreateUiChild(parent, name);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPos;

            Text text = GetOrAddComponent<Text>(go);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static Button CreateOrGetButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            GameObject go = GetOrCreateUiChild(parent, name);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(140f, 34f);
            rect.anchoredPosition = anchoredPos;

            Image image = GetOrAddComponent<Image>(go);
            image.color = new Color(0.18f, 0.27f, 0.31f, 0.95f);

            Button button = GetOrAddComponent<Button>(go);
            Text text = CreateOrGetText(
                go.transform,
                "Label",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                15,
                TextAnchor.MiddleCenter,
                Color.white);
            text.text = label;
            return button;
        }

        private static void EnsureCanvasComponents(GameObject canvasRoot)
        {
            Canvas canvas = GetOrAddComponent<Canvas>(canvasRoot);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GetOrAddComponent<CanvasScaler>(canvasRoot);
            GetOrAddComponent<GraphicRaycaster>(canvasRoot);
        }

        private static GameObject FindPlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform transform = transforms[j];
                    if (transform.name == "Player" || transform.CompareTag("Player"))
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Camera camera = roots[i].GetComponentInChildren<Camera>(true);
                if (camera != null && (camera.name == "Main Camera" || camera.CompareTag("MainCamera")))
                {
                    return camera;
                }
            }

            return Camera.main;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return new GameObject(name);
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject GetOrCreateUiChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRect = existing.GetComponent<RectTransform>();
                if (existingRect != null)
                {
                    return existing.gameObject;
                }

                existing.name = name + "_Legacy";
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAddComponent<T>(GameObject go)
            where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        private static Vector3 ToSurface(Terrain terrain, Vector3 point, float offset)
        {
            if (terrain == null)
            {
                point.y += offset;
                return point;
            }

            point.y = terrain.SampleHeight(point) + terrain.transform.position.y + offset;
            return point;
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private readonly struct LootEntrySeed
        {
            public readonly ItemDefinition Item;
            public readonly int MinAmount;
            public readonly int MaxAmount;
            public readonly float Weight;
            public readonly bool Guaranteed;

            public LootEntrySeed(ItemDefinition item, int minAmount, int maxAmount, float weight, bool guaranteed)
            {
                Item = item;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
                Weight = weight;
                Guaranteed = guaranteed;
            }
        }
    }
}
#endif
