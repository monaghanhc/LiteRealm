#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace LiteRealm.EditorTools
{
    public enum DoctorSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum InputHandlingMode
    {
        Unknown = -1,
        Old = 0,
        New = 1,
        Both = 2
    }

    [Serializable]
    public sealed class DoctorCheckResult
    {
        public string Code;
        public string Message;
        public DoctorSeverity Severity;
        public bool Passed;
        public string FixHint;
    }

    [Serializable]
    public sealed class DoctorReport
    {
        public DateTime GeneratedAtUtc = DateTime.UtcNow;
        public List<DoctorCheckResult> Results = new List<DoctorCheckResult>();

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Results.Count; i++)
                {
                    DoctorCheckResult result = Results[i];
                    if (result != null && !result.Passed && result.Severity == DoctorSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Results.Count; i++)
                {
                    DoctorCheckResult result = Results[i];
                    if (result != null && !result.Passed && result.Severity == DoctorSeverity.Warning)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasFailures => ErrorCount > 0 || WarningCount > 0;
    }

    public static class ProjectDoctorConstants
    {
        public const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        public const string InputSystemPackage = "com.unity.inputsystem";
        public const string AiNavigationPackage = "com.unity.ai.navigation";
        public const string UguiPackage = "com.unity.ugui";
        public const string TestFrameworkPackage = "com.unity.test-framework";
        public const string CinemachinePackage = "com.unity.cinemachine";

        public const string AutoRunPrefKey = "LiteRealm.ProjectDoctor.AutoRun";

        public static readonly string[] RequiredFolders =
        {
            "Assets/_Project",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Player",
            "Assets/_Project/Scripts/Camera",
            "Assets/_Project/Scripts/Combat",
            "Assets/_Project/Scripts/AI",
            "Assets/_Project/Scripts/Inventory",
            "Assets/_Project/Scripts/Loot",
            "Assets/_Project/Scripts/Quests",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/Saving",
            "Assets/_Project/Scripts/World",
            "Assets/_Project/Scripts/Editor",
            "Assets/_Project/Scripts/Tests",
            "Assets/_Project/Scripts/Tests/Editor",
            "Assets/_Project/Scripts/Tests/PlayMode",
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/ScriptableObjects/Quests",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/Materials",
            "Assets/_Project/Audio"
        };

        public static readonly string[] RequiredTags =
        {
            "Player",
            "Enemy",
            "NPC",
            "Interactable",
            "Loot"
        };

        public static readonly string[] RequiredLayers =
        {
            "Player",
            "Enemy",
            "NPC",
            "Interactable",
            "Loot"
        };

        public static readonly string[] RequiredPackageIds =
        {
            InputSystemPackage,
            AiNavigationPackage,
            UguiPackage,
            TestFrameworkPackage
        };

        public static readonly string[] OptionalPackageIds =
        {
            CinemachinePackage
        };

        public static readonly string[] RequiredMainSceneRootObjects =
        {
            "Terrain",
            "Player",
            "Directional Light",
            "EventSystem",
            "UI Canvas"
        };

        public static readonly string[] RequiredCombatPrefabPaths =
        {
            "Assets/_Project/Prefabs/Weapons/Rifle.prefab",
            "Assets/_Project/Prefabs/Enemies/Zombie.prefab",
            "Assets/_Project/Prefabs/Enemies/Boss.prefab",
            "Assets/_Project/Prefabs/Enemies/BossProjectile.prefab"
        };

        public static readonly string[] RequiredItemDefinitionPaths =
        {
            "Assets/_Project/ScriptableObjects/Items/Item_WaterBottle.asset",
            "Assets/_Project/ScriptableObjects/Items/Item_CannedFood.asset",
            "Assets/_Project/ScriptableObjects/Items/Item_Medkit.asset",
            "Assets/_Project/ScriptableObjects/Items/Item_Scrap.asset",
            "Assets/_Project/ScriptableObjects/Items/Item_RifleAmmo.asset",
            "Assets/_Project/ScriptableObjects/Items/Item_BossToken.asset"
        };

        public static readonly string[] RequiredLootTablePaths =
        {
            "Assets/_Project/ScriptableObjects/LootTables/LootTable_Container_Common.asset",
            "Assets/_Project/ScriptableObjects/LootTables/LootTable_Zombie_Common.asset",
            "Assets/_Project/ScriptableObjects/LootTables/LootTable_Boss_Special.asset"
        };

        public const string LootContainerPrefabPath = "Assets/_Project/Prefabs/Loot/LootCrate.prefab";

        public static readonly string[] RequiredQuestDefinitionPaths =
        {
            "Assets/_Project/ScriptableObjects/Quests/Quest_KillZombies_01.asset",
            "Assets/_Project/ScriptableObjects/Quests/Quest_RetrieveSupplies_01.asset",
            "Assets/_Project/ScriptableObjects/Quests/Quest_DefeatBoss_01.asset"
        };

        public const string QuestDatabasePath = "Assets/_Project/ScriptableObjects/Quests/QuestDatabase.asset";
    }
}
#endif
