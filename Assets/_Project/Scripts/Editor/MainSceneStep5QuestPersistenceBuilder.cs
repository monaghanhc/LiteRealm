#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using LiteRealm.Quests;
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
    public static class MainSceneStep5QuestPersistenceBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string QuestsDirectory = "Assets/_Project/ScriptableObjects/Quests";

        private const string QuestKillPath = QuestsDirectory + "/Quest_KillZombies_01.asset";
        private const string QuestRetrievePath = QuestsDirectory + "/Quest_RetrieveSupplies_01.asset";
        private const string QuestBossPath = QuestsDirectory + "/Quest_DefeatBoss_01.asset";
        private const string QuestCabinTapePath = QuestsDirectory + "/Quest_CabinTape_02.asset";
        private const string QuestRoadTollPath = QuestsDirectory + "/Quest_RoadToll_03.asset";
        private const string QuestBlackPinePath = QuestsDirectory + "/Quest_BlackPine_04.asset";
        private const string QuestRelayPath = QuestsDirectory + "/Quest_RelayUnderGlass_05.asset";
        private const string QuestAshCrownPath = QuestsDirectory + "/Quest_AshCrown_06.asset";
        private const string QuestDatabasePath = QuestsDirectory + "/QuestDatabase.asset";
        private const string ItemDatabasePath = "Assets/_Project/ScriptableObjects/Items/ItemDatabase.asset";

        [MenuItem("Tools/LiteRealm/Scenes/Apply Step 5 (Quests + Persistence)")]
        public static void ApplyStep5()
        {
            MainSceneStep4LootBuilder.ApplyStep4();
            EnsureDirectory(QuestsDirectory);

            ItemDefinition medkit = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_Medkit.asset");
            ItemDefinition food = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_CannedFood.asset");
            ItemDefinition ammo = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_RifleAmmo.asset");
            ItemDefinition bossToken = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_BossToken.asset");

            QuestDefinition openingQuest = GetOrCreateQuest(
                QuestKillPath,
                "quest.story.marker12.01",
                "White Smoke at Marker 12",
                "Ranger Vale saw white smoke over the old road marker before the radios went quiet. Sweep the campsite, pull what water you can, and prove you can move under pressure before he trusts you with the real work.",
                "Act I: The Smoke Line",
                "Field Contract",
                1,
                1,
                "Campsite south of the lake",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.KillZombies, "zombie.basic", 3, "Clear infected around Marker 12", 80),
                    new QuestObjectiveSeed(QuestType.RetrieveItem, "item.consumable.water_bottle", 1, "Recover a sealed water bottle from the camp", 50)
                },
                new[]
                {
                    new QuestRewardSeed(ammo, 18, 180)
                });

            QuestDefinition cabinTapeQuest = GetOrCreateQuest(
                QuestCabinTapePath,
                "quest.story.cabin_tape.02",
                "The Cabin Tape",
                "A dead courier carved three numbers into the cabin door before he turned. Vale thinks a field recorder inside the cabin holds the first half of a route into Black Pine.",
                "Act I: The Smoke Line",
                "Recon Contract",
                2,
                2,
                "Cabin ridge northwest of spawn",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.RecoverIntel, "intel.cabin_tape", 1, "Recover the cracked field recorder in the cabin", 120),
                    new QuestObjectiveSeed(QuestType.KillZombies, "zombie.basic", 4, "Silence the infected drawn to the tape signal", 100)
                },
                new[]
                {
                    new QuestRewardSeed(medkit, 1, 150),
                    new QuestRewardSeed(ammo, 16, 0)
                });

            QuestDefinition roadTollQuest = GetOrCreateQuest(
                QuestRetrievePath,
                "quest.story.road_toll.03",
                "Old Road Toll",
                "The recorder points to an abandoned checkpoint where somebody has been sorting salvage by blood type and radio code. Pull the supplies before night turns the road into a feeding lane.",
                "Act I: The Smoke Line",
                "Supply Contract",
                2,
                2,
                "Ruined checkpoint near the stone walls",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.RetrieveItem, "item.material.scrap", 5, "Recover marked scrap from the checkpoint", 90),
                    new QuestObjectiveSeed(QuestType.RetrieveItem, "item.ammo.rifle", 20, "Stockpile rifle rounds for the next push", 90)
                },
                new[]
                {
                    new QuestRewardSeed(food, 2, 180),
                    new QuestRewardSeed(medkit, 1, 0)
                });

            QuestDefinition blackPineQuest = GetOrCreateQuest(
                QuestBlackPinePath,
                "quest.story.black_pine.04",
                "Black Pine Ledger",
                "A ledger in the ruins lists names of survivors traded to the thing in the clearing. Secure the ledger and leave no movement in the stones.",
                "Act II: The Black Pine Ledger",
                "Assault Contract",
                3,
                3,
                "Old ruins beyond the lake",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.RecoverIntel, "intel.black_pine_ledger", 1, "Recover the ledger from the ruins", 160),
                    new QuestObjectiveSeed(QuestType.KillZombies, "zombie.basic", 6, "Clear the ruins perimeter", 150),
                    new QuestObjectiveSeed(QuestType.SecureLocation, "secure.ruins_perimeter", 1, "Mark the ruins as secured", 100)
                },
                new[]
                {
                    new QuestRewardSeed(ammo, 28, 240)
                });

            QuestDefinition relayQuest = GetOrCreateQuest(
                QuestRelayPath,
                "quest.story.relay_glass.05",
                "Relay Under Glass",
                "The ledger says the old relay still broadcasts from beneath cracked solar glass. If you can bring it online, every dead channel in the valley will hear Vale's beacon.",
                "Act II: The Black Pine Ledger",
                "Signal Contract",
                4,
                4,
                "Hill relay overlooking the boss clearing",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.ActivateSignal, "signal.relay_uplink", 1, "Activate the emergency relay uplink", 220),
                    new QuestObjectiveSeed(QuestType.KillZombies, "zombie.basic", 8, "Hold the relay while the signal stabilizes", 180)
                },
                new[]
                {
                    new QuestRewardSeed(medkit, 2, 300),
                    new QuestRewardSeed(ammo, 32, 0)
                });

            QuestDefinition ashCrownQuest = GetOrCreateQuest(
                QuestBossPath,
                "quest.story.ash_crown.06",
                "The Ash Crown",
                "The relay woke something under the red pillars. Vale calls it the Ash Crown, a swollen signal node wearing a body. Kill it, take the core, and break the valley's nightmare loop.",
                "Act III: The Ash Crown",
                "Boss Contract",
                5,
                5,
                "Red-pillar clearing at night",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.ActivateSignal, "signal.boss_lure", 1, "Tune the lure beacon in the arena", 180),
                    new QuestObjectiveSeed(QuestType.DefeatBoss, "boss.alpha", 1, "Defeat the Ash Crown", 420)
                },
                new[]
                {
                    new QuestRewardSeed(bossToken, 1, 500),
                    new QuestRewardSeed(food, 3, 0)
                });

            QuestDefinition epilogueQuest = GetOrCreateQuest(
                QuestAshCrownPath,
                "quest.story.dead_air.07",
                "Dead Air",
                "With the core cooling in a jar, the valley is finally quiet enough to hear a human voice on the long band. Secure the last cache and decide whether LiteRealm becomes a refuge or another grave with better walls.",
                "Epilogue: Dead Air",
                "Recovery Contract",
                6,
                3,
                "Hidden cache beside the lake",
                new[]
                {
                    new QuestObjectiveSeed(QuestType.RecoverIntel, "intel.dead_air_cache", 1, "Open the hidden post-Crown cache", 240),
                    new QuestObjectiveSeed(QuestType.RetrieveItem, "item.special.boss_token", 1, "Bring Vale proof that the Crown is dead", 120)
                },
                new[]
                {
                    new QuestRewardSeed(ammo, 45, 600),
                    new QuestRewardSeed(medkit, 2, 0)
                });

            QuestDatabase questDatabase = GetOrCreateQuestDatabase(new[]
            {
                openingQuest,
                cabinTapeQuest,
                roadTollQuest,
                blackPineQuest,
                relayQuest,
                ashCrownQuest,
                epilogueQuest
            });

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            GameObject appRoot = GetOrCreateRoot(scene, "__App");
            GameObject worldRoot = GetOrCreateRoot(scene, "World");
            GameObject canvasRoot = GetOrCreateRoot(scene, "UI Canvas");

            EnsureCanvasComponents(canvasRoot);

            GameObject player = FindPlayer(scene);
            if (player == null)
            {
                Debug.LogError("Step 5 builder: missing Player in Main scene.");
                return;
            }

            GameEventHub hub = FindInScene<GameEventHub>(scene);
            if (hub == null)
            {
                GameObject hubGo = GetOrCreateChild(appRoot.transform, "GameEventHub");
                hub = GetOrAddComponent<GameEventHub>(hubGo);
            }
            EnsureObjectiveInteractables(worldRoot.transform, scene, hub);

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            if (inventory == null)
            {
                inventory = player.AddComponent<InventoryComponent>();
            }

            BossSpawnManager bossSpawnManager = FindInScene<BossSpawnManager>(scene);
            QuestManager questManager = EnsureQuestManager(appRoot, questDatabase, inventory, hub, bossSpawnManager);
            NPCQuestGiver npc = EnsureQuestNpc(worldRoot.transform, scene, new[]
            {
                openingQuest,
                cabinTapeQuest,
                roadTollQuest,
                blackPineQuest,
                relayQuest,
                ashCrownQuest,
                epilogueQuest
            });
            DialogueUIController dialogue = EnsureDialogueUi(canvasRoot);
            EnsureQuestLogUi(canvasRoot, questManager);

            ConfigurePlayerInteractor(player, questManager, dialogue);
            ConfigureSaveSystem(scene, appRoot, player, questManager);

            if (npc != null)
            {
                EditorUtility.SetDirty(npc);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Step 5 quest/persistence setup applied.");
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
                               || !File.Exists(QuestKillPath)
                               || !File.Exists(QuestRetrievePath)
                               || !File.Exists(QuestBossPath)
                               || !File.Exists(QuestCabinTapePath)
                               || !File.Exists(QuestBlackPinePath)
                               || !File.Exists(QuestRelayPath)
                               || !File.Exists(QuestAshCrownPath)
                               || !File.Exists(QuestDatabasePath);

                if (missing)
                {
                    ApplyStep5();
                }
            };
        }

        private static QuestDefinition GetOrCreateQuest(
            string path,
            string id,
            string title,
            string description,
            string storyAct,
            string contractType,
            int requiredLevel,
            int riskRating,
            string locationHint,
            QuestObjectiveSeed[] objectives,
            QuestRewardSeed[] rewards)
        {
            QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<QuestDefinition>();
                AssetDatabase.CreateAsset(quest, path);
            }

            SerializedObject so = new SerializedObject(quest);
            SetString(so, "questId", id);
            SetString(so, "title", title);
            SetString(so, "description", description);
            SetString(so, "storyAct", storyAct);
            SetString(so, "contractType", contractType);
            SetInt(so, "requiredLevel", Mathf.Max(1, requiredLevel));
            SetInt(so, "riskRating", Mathf.Clamp(riskRating, 1, 5));
            SetString(so, "locationHint", locationHint);

            SerializedProperty objectiveArray = so.FindProperty("objectives");
            if (objectiveArray != null)
            {
                objectiveArray.arraySize = objectives.Length;
                for (int i = 0; i < objectives.Length; i++)
                {
                    QuestObjectiveSeed seed = objectives[i];
                    SerializedProperty element = objectiveArray.GetArrayElementAtIndex(i);
                    SetIntRelative(element, "Type", (int)seed.Type);
                    SetStringRelative(element, "TargetId", seed.TargetId);
                    SetIntRelative(element, "RequiredCount", Mathf.Max(1, seed.RequiredCount));
                    SetIntRelative(element, "ExperienceReward", Mathf.Max(0, seed.ExperienceReward));
                    SetBoolRelative(element, "Optional", seed.Optional);
                    SetStringRelative(element, "Description", seed.Description);
                }
            }

            SerializedProperty rewardArray = so.FindProperty("rewards");
            if (rewardArray != null)
            {
                rewardArray.arraySize = rewards.Length;
                for (int i = 0; i < rewards.Length; i++)
                {
                    QuestRewardSeed seed = rewards[i];
                    SerializedProperty element = rewardArray.GetArrayElementAtIndex(i);
                    SetObjectRelative(element, "Item", seed.Item);
                    SetIntRelative(element, "Amount", Mathf.Max(1, seed.Amount));
                    SetIntRelative(element, "Experience", Mathf.Max(0, seed.Experience));
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quest);
            return quest;
        }

        private static QuestDatabase GetOrCreateQuestDatabase(QuestDefinition[] quests)
        {
            QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<QuestDatabase>();
                AssetDatabase.CreateAsset(database, QuestDatabasePath);
            }

            SerializedObject so = new SerializedObject(database);
            SerializedProperty questList = so.FindProperty("quests");
            if (questList != null)
            {
                questList.arraySize = quests.Length;
                for (int i = 0; i < quests.Length; i++)
                {
                    questList.GetArrayElementAtIndex(i).objectReferenceValue = quests[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static void EnsureObjectiveInteractables(Transform worldRoot, Scene scene, GameEventHub hub)
        {
            if (worldRoot == null)
            {
                return;
            }

            Terrain terrain = FindInScene<Terrain>(scene);
            Transform root = GetOrCreateChild(worldRoot, "StoryObjectiveProps").transform;
            Material intelMaterial = ProceduralArtKit.EnsureStandardMaterial(
                "Assets/_Project/Materials/M_Objective_Intel.mat",
                new Color(0.18f, 0.22f, 0.20f),
                0.45f,
                0.05f);
            Material signalMaterial = ProceduralArtKit.EnsureStandardMaterial(
                "Assets/_Project/Materials/M_Objective_RadioGlow.mat",
                new Color(0.09f, 0.20f, 0.22f),
                0.5f,
                0f,
                true,
                new Color(0.0f, 0.55f, 0.75f));

            ConfigureObjectiveProp(
                root,
                terrain,
                "Intel_CabinTape",
                PrimitiveType.Cube,
                new Vector3(97.5f, 0f, 127.4f),
                new Vector3(0.55f, 0.12f, 0.38f),
                intelMaterial,
                "intel.cabin_tape",
                "Press E to recover the cabin tape",
                "The recorder clicks twice, then plays a voice whispering coordinates through static.",
                hub);

            ConfigureObjectiveProp(
                root,
                terrain,
                "Intel_BlackPineLedger",
                PrimitiveType.Cube,
                new Vector3(358f, 0f, 288f),
                new Vector3(0.7f, 0.16f, 0.5f),
                intelMaterial,
                "intel.black_pine_ledger",
                "Press E to take the Black Pine ledger",
                "The ledger is packed with names, trade marks, and a red crown stamped beside the last page.",
                hub);

            ConfigureObjectiveProp(
                root,
                terrain,
                "Marker_RuinsPerimeter",
                PrimitiveType.Cylinder,
                new Vector3(363f, 0f, 294f),
                new Vector3(0.22f, 0.8f, 0.22f),
                signalMaterial,
                "secure.ruins_perimeter",
                "Press E to mark the ruins secure",
                "A blue flare burns over the stones. Vale will know the ruins are open.",
                hub);

            ConfigureObjectiveProp(
                root,
                terrain,
                "Signal_RelayUplink",
                PrimitiveType.Cylinder,
                new Vector3(470f, 0f, 438f),
                new Vector3(0.28f, 1.15f, 0.28f),
                signalMaterial,
                "signal.relay_uplink",
                "Press E to activate the relay uplink",
                "The relay coughs alive and pushes Vale's beacon across the dead channels.",
                hub);

            ConfigureObjectiveProp(
                root,
                terrain,
                "Signal_BossLure",
                PrimitiveType.Cylinder,
                new Vector3(500f, 0f, 488f),
                new Vector3(0.35f, 0.9f, 0.35f),
                signalMaterial,
                "signal.boss_lure",
                "Press E to tune the lure beacon",
                "The red pillars answer with a pulse from somewhere below the clearing.",
                hub);

            ConfigureObjectiveProp(
                root,
                terrain,
                "Intel_DeadAirCache",
                PrimitiveType.Cube,
                new Vector3(306f, 0f, 335f),
                new Vector3(1.0f, 0.45f, 0.7f),
                intelMaterial,
                "intel.dead_air_cache",
                "Press E to open the dead-air cache",
                "Inside the cache is a map of survivor routes and a note: build the refuge or burn the road.",
                hub);
        }

        private static void ConfigureObjectiveProp(
            Transform parent,
            Terrain terrain,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            string objectiveId,
            string prompt,
            string message,
            GameEventHub hub)
        {
            Transform existing = parent.Find(name);
            GameObject prop = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitive);
            prop.name = name;
            prop.transform.SetParent(parent, true);
            prop.transform.position = ToSurface(terrain, position, scale.y * 0.5f);
            prop.transform.localScale = scale;

            Renderer renderer = prop.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = prop.GetComponent<Collider>();
            if (collider == null)
            {
                collider = prop.AddComponent<BoxCollider>();
            }
            collider.isTrigger = false;

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                prop.layer = interactableLayer;
            }

            WorldObjectiveInteractable interactable = GetOrAddComponent<WorldObjectiveInteractable>(prop);
            SerializedObject so = new SerializedObject(interactable);
            SetString(so, "objectiveId", objectiveId);
            SetString(so, "promptText", prompt);
            SetString(so, "interactionMessage", message);
            SetObject(so, "eventHub", hub);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static QuestManager EnsureQuestManager(
            GameObject appRoot,
            QuestDatabase database,
            InventoryComponent inventory,
            GameEventHub eventHub,
            BossSpawnManager bossSpawnManager)
        {
            QuestManager manager = appRoot.GetComponentInChildren<QuestManager>(true);
            if (manager == null)
            {
                GameObject go = GetOrCreateChild(appRoot.transform, "QuestManager");
                manager = GetOrAddComponent<QuestManager>(go);
            }

            SerializedObject so = new SerializedObject(manager);
            SetObject(so, "questDatabase", database);
            SetObject(so, "inventory", inventory);
            SetObject(so, "eventHub", eventHub);
            SetObject(so, "bossSpawnManager", bossSpawnManager);
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static NPCQuestGiver EnsureQuestNpc(Transform worldRoot, Scene scene, QuestDefinition[] quests)
        {
            Transform npcRoot = GetOrCreateChild(worldRoot, "NPCs").transform;
            Transform npcTransform = npcRoot.Find("NPC_Survivor");
            GameObject npcObject;
            if (npcTransform == null)
            {
                npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npcObject.name = "NPC_Survivor";
                npcObject.transform.SetParent(npcRoot, true);
                npcObject.transform.localScale = new Vector3(1f, 1.1f, 1f);
            }
            else
            {
                npcObject = npcTransform.gameObject;
            }

            Terrain terrain = FindInScene<Terrain>(scene);
            npcObject.transform.position = ToSurface(terrain, new Vector3(216f, 0f, 214f), 1.0f);
            npcObject.transform.rotation = Quaternion.Euler(0f, 136f, 0f);

            if (ProjectDoctorRunner.HasTag("NPC"))
            {
                npcObject.tag = "NPC";
            }

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                npcObject.layer = interactableLayer;
            }

            NPCQuestGiver giver = GetOrAddComponent<NPCQuestGiver>(npcObject);
            SerializedObject so = new SerializedObject(giver);
            SetString(so, "npcName", "Ranger Vale");
            SetString(so, "greeting", "Keep your rifle dry and your pack light. Every contract out there either buys us another sunrise or tells us who sold the last one.");

            SerializedProperty questLine = so.FindProperty("questLine");
            if (questLine != null)
            {
                questLine.arraySize = quests.Length;
                for (int i = 0; i < quests.Length; i++)
                {
                    questLine.GetArrayElementAtIndex(i).objectReferenceValue = quests[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return giver;
        }

        private static DialogueUIController EnsureDialogueUi(GameObject canvasRoot)
        {
            GameObject panel = GetOrCreateUiChild(canvasRoot.transform, "DialoguePanel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600f, 380f);
            panelRect.anchoredPosition = Vector2.zero;

            Image background = GetOrAddComponent<Image>(panel);
            background.color = new Color(0.06f, 0.08f, 0.11f, 0.95f);

            Text npcName = CreateOrGetText(
                panel.transform,
                "NpcNameText",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(520f, 32f),
                new Vector2(0f, -20f),
                22,
                TextAnchor.MiddleCenter,
                Color.white);
            npcName.text = "NPC";

            Text body = CreateOrGetText(
                panel.transform,
                "BodyText",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, 110f),
                new Vector2(0f, -64f),
                16,
                TextAnchor.UpperLeft,
                Color.white);
            body.text = "Dialogue";

            Text questTitle = CreateOrGetText(
                panel.transform,
                "QuestTitleText",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, 28f),
                new Vector2(0f, -186f),
                18,
                TextAnchor.MiddleLeft,
                new Color(0.96f, 0.89f, 0.56f));
            questTitle.text = "Quest";

            Text questDescription = CreateOrGetText(
                panel.transform,
                "QuestDescriptionText",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, 120f),
                new Vector2(0f, -255f),
                15,
                TextAnchor.UpperLeft,
                Color.white);
            questDescription.text = "Quest description";

            Button accept = CreateOrGetButton(panel.transform, "AcceptButton", "Accept", new Vector2(-195f, 24f));
            Button decline = CreateOrGetButton(panel.transform, "DeclineButton", "Decline", new Vector2(-65f, 24f));
            Button turnIn = CreateOrGetButton(panel.transform, "TurnInButton", "Turn In", new Vector2(65f, 24f));
            Button close = CreateOrGetButton(panel.transform, "CloseButton", "Close", new Vector2(195f, 24f));

            DialogueUIController legacyController = panel.GetComponent<DialogueUIController>();
            if (legacyController != null)
            {
                Object.DestroyImmediate(legacyController);
            }

            DialogueUIController controller = canvasRoot.GetComponent<DialogueUIController>();
            if (controller == null)
            {
                controller = canvasRoot.AddComponent<DialogueUIController>();
            }

            SerializedObject so = new SerializedObject(controller);
            SetObject(so, "root", panel);
            SetObject(so, "npcNameText", npcName);
            SetObject(so, "bodyText", body);
            SetObject(so, "questTitleText", questTitle);
            SetObject(so, "questDescriptionText", questDescription);
            SetObject(so, "acceptButton", accept);
            SetObject(so, "declineButton", decline);
            SetObject(so, "turnInButton", turnIn);
            SetObject(so, "closeButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static QuestLogUIController EnsureQuestLogUi(GameObject canvasRoot, QuestManager questManager)
        {
            GameObject panel = GetOrCreateUiChild(canvasRoot.transform, "QuestLogPanel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.sizeDelta = new Vector2(360f, 420f);
            panelRect.anchoredPosition = new Vector2(-14f, 0f);

            Image background = GetOrAddComponent<Image>(panel);
            background.color = new Color(0.05f, 0.08f, 0.1f, 0.92f);

            Text title = CreateOrGetText(
                panel.transform,
                "TitleText",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(320f, 28f),
                new Vector2(0f, -18f),
                20,
                TextAnchor.MiddleCenter,
                Color.white);
            title.text = "Quest Log [J]";

            Text logText = CreateOrGetText(
                panel.transform,
                "QuestLogText",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -58f),
                new Vector2(0f, -16f),
                15,
                TextAnchor.UpperLeft,
                Color.white);
            logText.text = "No active quests.";

            QuestLogUIController legacyController = panel.GetComponent<QuestLogUIController>();
            if (legacyController != null)
            {
                Object.DestroyImmediate(legacyController);
            }

            QuestLogUIController controller = canvasRoot.GetComponent<QuestLogUIController>();
            if (controller == null)
            {
                controller = canvasRoot.AddComponent<QuestLogUIController>();
            }

            SerializedObject so = new SerializedObject(controller);
            SetObject(so, "root", panel);
            SetObject(so, "questLogText", logText);
            SetObject(so, "questManager", questManager);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static void ConfigurePlayerInteractor(GameObject player, QuestManager questManager, DialogueUIController dialogue)
        {
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(interactor);
            SetObject(so, "questManager", questManager);
            SetObject(so, "dialogueUI", dialogue);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSaveSystem(Scene scene, GameObject appRoot, GameObject player, QuestManager questManager)
        {
            SaveSystem saveSystem = FindInScene<SaveSystem>(scene);
            if (saveSystem == null)
            {
                GameObject saveRoot = GetOrCreateChild(appRoot.transform, "SaveSystem");
                saveSystem = GetOrAddComponent<SaveSystem>(saveRoot);
            }

            if (saveSystem == null)
            {
                return;
            }

            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            InventoryComponent inventory = player != null ? player.GetComponent<InventoryComponent>() : null;
            CharacterController controller = player != null ? player.GetComponent<CharacterController>() : null;
            ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            DayNightCycleManager dayNight = FindInScene<DayNightCycleManager>(scene);
            BossSpawnManager bossManager = FindInScene<BossSpawnManager>(scene);
            LootContainer[] containers = Object.FindObjectsOfType<LootContainer>(true);

            SerializedObject so = new SerializedObject(saveSystem);
            SetObject(so, "playerRoot", player != null ? player.transform : null);
            SetObject(so, "playerCharacterController", controller);
            SetObject(so, "playerStats", stats);
            SetObject(so, "inventory", inventory);
            SetObject(so, "itemDatabase", itemDatabase);
            SetObject(so, "questManager", questManager);
            SetObject(so, "dayNightCycle", dayNight);
            SetObject(so, "bossSpawnManager", bossManager);

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
            rect.sizeDelta = new Vector2(120f, 34f);
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

        private static Vector3 ToSurface(Terrain terrain, Vector3 point, float yOffset)
        {
            if (terrain == null)
            {
                point.y += yOffset;
                return point;
            }

            point.y = terrain.SampleHeight(point) + terrain.transform.position.y + yOffset;
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

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
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

        private static void SetStringRelative(SerializedProperty property, string relativePath, string value)
        {
            SerializedProperty child = property.FindPropertyRelative(relativePath);
            if (child != null)
            {
                child.stringValue = value;
            }
        }

        private static void SetIntRelative(SerializedProperty property, string relativePath, int value)
        {
            SerializedProperty child = property.FindPropertyRelative(relativePath);
            if (child != null)
            {
                child.intValue = value;
            }
        }

        private static void SetBoolRelative(SerializedProperty property, string relativePath, bool value)
        {
            SerializedProperty child = property.FindPropertyRelative(relativePath);
            if (child != null)
            {
                child.boolValue = value;
            }
        }

        private static void SetObjectRelative(SerializedProperty property, string relativePath, Object value)
        {
            SerializedProperty child = property.FindPropertyRelative(relativePath);
            if (child != null)
            {
                child.objectReferenceValue = value;
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private readonly struct QuestObjectiveSeed
        {
            public readonly QuestType Type;
            public readonly string TargetId;
            public readonly int RequiredCount;
            public readonly string Description;
            public readonly int ExperienceReward;
            public readonly bool Optional;

            public QuestObjectiveSeed(QuestType type, string targetId, int requiredCount, string description, int experienceReward, bool optional = false)
            {
                Type = type;
                TargetId = targetId;
                RequiredCount = requiredCount;
                Description = description;
                ExperienceReward = experienceReward;
                Optional = optional;
            }
        }

        private readonly struct QuestRewardSeed
        {
            public readonly ItemDefinition Item;
            public readonly int Amount;
            public readonly int Experience;

            public QuestRewardSeed(ItemDefinition item, int amount, int experience)
            {
                Item = item;
                Amount = amount;
                Experience = experience;
            }
        }
    }
}
#endif
