#if UNITY_EDITOR
using System.Reflection;
using LiteRealm.Core;
using LiteRealm.Quests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LiteRealm.Tests.Editor
{
    public class QuestManagerLevelingEditModeTests
    {
        [Test]
        public void ObjectiveSignal_AwardsObjectiveExperienceOnce_AndUnlocksLevelGate()
        {
            QuestDefinition intelQuest = CreateQuest(
                "quest.tests.intel",
                "Recover Test Intel",
                1,
                new ObjectiveSeed(QuestType.RecoverIntel, "intel.tests.cache", 2, 60));
            QuestDefinition lockedQuest = CreateQuest(
                "quest.tests.locked",
                "Locked Follow Up",
                2,
                new ObjectiveSeed(QuestType.SecureLocation, "secure.tests.outpost", 1, 0));
            QuestDatabase database = CreateDatabase(intelQuest, lockedQuest);
            GameObject root = null;

            try
            {
                QuestManager manager = CreateManager(database, out GameEventHub hub, out root);
                int levelChangedCount = 0;
                manager.LevelChanged += (_, _) => levelChangedCount++;

                Assert.IsFalse(manager.MeetsLevelRequirement(lockedQuest), "Level 2 quest should start locked.");
                Assert.IsFalse(manager.AcceptQuest(lockedQuest), "Locked quest should not be accepted.");
                Assert.IsTrue(manager.AcceptQuest(intelQuest), "Level 1 intel quest should be accepted.");

                hub.RaiseObjectiveSignaled(new ObjectiveSignalEvent
                {
                    ObjectiveId = "intel.tests.cache",
                    Amount = 1
                });

                Assert.AreEqual(0, manager.TotalExperience, "Partial objective progress should not grant XP.");
                Assert.AreEqual(1, manager.CurrentLevel, "Partial objective progress should not level the player.");
                Assert.IsFalse(manager.IsQuestReadyToTurnIn(intelQuest.QuestId), "Quest should not be ready after partial progress.");

                hub.RaiseObjectiveSignaled(new ObjectiveSignalEvent
                {
                    ObjectiveId = "intel.tests.cache",
                    Amount = 10
                });

                Assert.AreEqual(60, manager.TotalExperience, "Completing the objective should grant objective XP once.");
                Assert.AreEqual(2, manager.CurrentLevel, "Objective XP should push the manager through the configured level threshold.");
                Assert.AreEqual(1, levelChangedCount, "Level change event should fire once for the level-up.");
                Assert.IsTrue(manager.IsQuestReadyToTurnIn(intelQuest.QuestId), "Required objective should complete the quest.");

                hub.RaiseObjectiveSignaled(new ObjectiveSignalEvent
                {
                    ObjectiveId = "intel.tests.cache",
                    Amount = 10
                });

                Assert.AreEqual(60, manager.TotalExperience, "Repeated objective signals must not duplicate objective XP.");
                Assert.AreEqual(1, levelChangedCount, "Duplicate objective signals must not raise another level event.");
                Assert.IsTrue(manager.AcceptQuest(lockedQuest), "Level 2 quest should unlock after objective XP levels the player.");
            }
            finally
            {
                DestroyTestObjects(root, database, intelQuest, lockedQuest);
            }
        }

        [Test]
        public void CaptureAndRestore_PreservesObjectiveRewardClaims_AndPreventsDuplicateRewards()
        {
            QuestDefinition quest = CreateQuest(
                "quest.tests.restore",
                "Restore Intel",
                1,
                new ObjectiveSeed(QuestType.ActivateSignal, "signal.tests.relay", 1, 60));
            QuestDatabase database = CreateDatabase(quest);
            GameObject root = null;
            GameObject restoredRoot = null;

            try
            {
                QuestManager manager = CreateManager(database, out GameEventHub hub, out root);
                Assert.IsTrue(manager.AcceptQuest(quest));

                hub.RaiseObjectiveSignaled(new ObjectiveSignalEvent
                {
                    ObjectiveId = "signal.tests.relay",
                    Amount = 1
                });

                QuestManagerState state = manager.CaptureState();
                Assert.AreEqual(60, state.TotalExperience);
                Assert.AreEqual(2, state.CurrentLevel);
                Assert.AreEqual(1, state.ActiveQuests.Count);
                Assert.IsTrue(state.ActiveQuests[0].ObjectiveRewardsClaimed[0], "Captured state should remember claimed objective XP.");

                QuestManager restored = CreateManager(database, out GameEventHub restoredHub, out restoredRoot);
                restored.RestoreState(state);

                Assert.AreEqual(60, restored.TotalExperience, "Restored manager should keep saved XP.");
                Assert.AreEqual(2, restored.CurrentLevel, "Restored manager should recalculate level from saved XP.");
                Assert.AreEqual(1, restored.ActiveQuests.Count);
                Assert.AreEqual(1, restored.ActiveQuests[0].Progress[0]);
                Assert.IsTrue(restored.ActiveQuests[0].ObjectiveRewardsClaimed[0], "Restored runtime should keep claimed objective reward flag.");

                restoredHub.RaiseObjectiveSignaled(new ObjectiveSignalEvent
                {
                    ObjectiveId = "signal.tests.relay",
                    Amount = 1
                });

                Assert.AreEqual(60, restored.TotalExperience, "Restored claimed objective should not grant duplicate XP.");
            }
            finally
            {
                DestroyTestObjects(root, restoredRoot, database, quest);
            }
        }

        [Test]
        public void NpcQuestGiver_ReturnsLockedByLevel_WhenNextQuestRequiresHigherLevel()
        {
            QuestDefinition lockedQuest = CreateQuest(
                "quest.tests.npc.locked",
                "Locked NPC Contract",
                2,
                new ObjectiveSeed(QuestType.RecoverIntel, "intel.tests.locked", 1, 0));
            QuestDatabase database = CreateDatabase(lockedQuest);
            GameObject managerRoot = null;
            GameObject npcRoot = null;

            try
            {
                QuestManager manager = CreateManager(database, out _, out managerRoot);
                npcRoot = new GameObject("NPC Quest Giver Test");
                NPCQuestGiver giver = npcRoot.AddComponent<NPCQuestGiver>();
                SetQuestLine(giver, lockedQuest);

                NpcQuestState state = giver.GetState(manager, out QuestDefinition currentQuest);

                Assert.AreEqual(NpcQuestState.LockedByLevel, state);
                Assert.AreSame(lockedQuest, currentQuest);
            }
            finally
            {
                DestroyTestObjects(managerRoot, npcRoot, database, lockedQuest);
            }
        }

        private static QuestManager CreateManager(QuestDatabase database, out GameEventHub hub, out GameObject root)
        {
            root = new GameObject("Quest Manager Test Root");
            root.SetActive(false);
            hub = root.AddComponent<GameEventHub>();
            QuestManager manager = root.AddComponent<QuestManager>();

            SerializedObject so = new SerializedObject(manager);
            SetObject(so, "questDatabase", database);
            SetObject(so, "eventHub", hub);
            SetInt(so, "startingLevel", 1);
            SetInt(so, "baseExperienceToLevel", 50);
            SetFloat(so, "levelExperienceGrowth", 1.1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeQuestManagerLifecycle(manager, "Awake");
            InvokeQuestManagerLifecycle(manager, "OnEnable");
            return manager;
        }

        private static QuestDefinition CreateQuest(string questId, string title, int requiredLevel, params ObjectiveSeed[] objectives)
        {
            QuestDefinition quest = ScriptableObject.CreateInstance<QuestDefinition>();
            SerializedObject so = new SerializedObject(quest);
            SetString(so, "questId", questId);
            SetString(so, "title", title);
            SetString(so, "storyAct", "Test Act");
            SetString(so, "contractType", "Test Contract");
            SetInt(so, "requiredLevel", requiredLevel);
            SetInt(so, "riskRating", 1);

            SerializedProperty objectiveArray = so.FindProperty("objectives");
            objectiveArray.arraySize = objectives.Length;
            for (int i = 0; i < objectives.Length; i++)
            {
                SerializedProperty objective = objectiveArray.GetArrayElementAtIndex(i);
                objective.FindPropertyRelative("Type").enumValueIndex = (int)objectives[i].Type;
                objective.FindPropertyRelative("TargetId").stringValue = objectives[i].TargetId;
                objective.FindPropertyRelative("RequiredCount").intValue = objectives[i].RequiredCount;
                objective.FindPropertyRelative("ExperienceReward").intValue = objectives[i].ExperienceReward;
                objective.FindPropertyRelative("Optional").boolValue = objectives[i].Optional;
                objective.FindPropertyRelative("Description").stringValue = objectives[i].Type.ToString();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return quest;
        }

        private static QuestDatabase CreateDatabase(params QuestDefinition[] quests)
        {
            QuestDatabase database = ScriptableObject.CreateInstance<QuestDatabase>();
            SerializedObject so = new SerializedObject(database);
            SerializedProperty questArray = so.FindProperty("quests");
            questArray.arraySize = quests.Length;
            for (int i = 0; i < quests.Length; i++)
            {
                questArray.GetArrayElementAtIndex(i).objectReferenceValue = quests[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static void SetQuestLine(NPCQuestGiver giver, params QuestDefinition[] quests)
        {
            SerializedObject so = new SerializedObject(giver);
            SerializedProperty questLine = so.FindProperty("questLine");
            questLine.arraySize = quests.Length;
            for (int i = 0; i < quests.Length; i++)
            {
                questLine.GetArrayElementAtIndex(i).objectReferenceValue = quests[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.stringValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.floatValue = value;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Missing serialized property {propertyName}.");
            property.objectReferenceValue = value;
        }

        private static void InvokeQuestManagerLifecycle(QuestManager manager, string methodName)
        {
            MethodInfo method = typeof(QuestManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing QuestManager lifecycle method {methodName}.");
            method.Invoke(manager, null);
        }

        private static void DestroyTestObjects(params Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                Object obj = objects[i];
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        private readonly struct ObjectiveSeed
        {
            public readonly QuestType Type;
            public readonly string TargetId;
            public readonly int RequiredCount;
            public readonly int ExperienceReward;
            public readonly bool Optional;

            public ObjectiveSeed(QuestType type, string targetId, int requiredCount, int experienceReward, bool optional = false)
            {
                Type = type;
                TargetId = targetId;
                RequiredCount = requiredCount;
                ExperienceReward = experienceReward;
                Optional = optional;
            }
        }
    }
}
#endif
