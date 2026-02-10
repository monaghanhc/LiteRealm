using System.Collections;
using LiteRealm.AI;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Player;
using LiteRealm.Quests;
using LiteRealm.Saving;
using LiteRealm.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class QuestPersistencePlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator QuestFlow_CompleteQuest_RewardGranted_SaveLoadRestoresState()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");

            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            Assert.IsNotNull(player, "Player missing in Main scene.");

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            Assert.IsNotNull(inventory, "InventoryComponent missing on player.");

            QuestManager questManager = Object.FindObjectOfType<QuestManager>();
            Assert.IsNotNull(questManager, "QuestManager missing in scene.");

            NPCQuestGiver npc = Object.FindObjectOfType<NPCQuestGiver>();
            Assert.IsNotNull(npc, "NPCQuestGiver missing in scene.");

            SaveSystem saveSystem = Object.FindObjectOfType<SaveSystem>();
            Assert.IsNotNull(saveSystem, "SaveSystem missing in scene.");

            DayNightCycleManager dayNight = Object.FindObjectOfType<DayNightCycleManager>();
            Assert.IsNotNull(dayNight, "DayNightCycleManager missing in scene.");

            NpcQuestState initialState = npc.GetState(questManager, out QuestDefinition offeredQuest);
            Assert.AreEqual(NpcQuestState.Offer, initialState, "Expected NPC to offer first quest.");
            Assert.IsNotNull(offeredQuest, "NPC did not provide an offered quest.");

            bool accepted = npc.AcceptCurrentQuest(questManager);
            Assert.IsTrue(accepted, "Failed to accept offered quest.");
            Assert.IsTrue(questManager.IsQuestActive(offeredQuest.QuestId), "Accepted quest is not active.");

            for (int i = 0; i < offeredQuest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = offeredQuest.Objectives[i];
                if (objective == null)
                {
                    continue;
                }

                if (objective.Type == QuestType.KillZombies)
                {
                    yield return KillZombies(player.transform.position, objective.RequiredCount);
                }
                else if (objective.Type == QuestType.RetrieveItem)
                {
                    ItemDefinition item = FindItemDefinitionById(objective.TargetId);
                    Assert.IsNotNull(item, $"Could not resolve item definition for quest target id {objective.TargetId}");
                    inventory.AddItem(item, objective.RequiredCount);
                    yield return null;
                }
                else if (objective.Type == QuestType.DefeatBoss)
                {
                    yield return KillBoss();
                }
            }

            yield return null;
            Assert.IsTrue(questManager.IsQuestReadyToTurnIn(offeredQuest.QuestId), "Quest did not reach turn-in ready state.");

            int xpBefore = questManager.TotalExperience;
            QuestRewardDefinition primaryReward = GetFirstRewardWithItem(offeredQuest);
            int rewardBefore = primaryReward != null ? inventory.GetItemCount(primaryReward.Item.ItemId) : 0;

            bool turnedIn = npc.TryTurnInCurrentQuest(questManager, out string turnInMessage);
            Assert.IsTrue(turnedIn, $"Quest turn-in failed: {turnInMessage}");
            Assert.IsTrue(questManager.IsQuestCompleted(offeredQuest.QuestId), "Quest should be marked completed.");

            if (primaryReward != null)
            {
                int rewardAfter = inventory.GetItemCount(primaryReward.Item.ItemId);
                Assert.Greater(rewardAfter, rewardBefore, "Quest reward item was not granted.");
            }

            Assert.Greater(questManager.TotalExperience, xpBefore, "Quest XP reward was not granted.");

            string activeQuestToPersist = string.Empty;
            NpcQuestState postTurnInState = npc.GetState(questManager, out QuestDefinition nextQuest);
            if (postTurnInState == NpcQuestState.Offer && nextQuest != null)
            {
                bool nextAccepted = npc.AcceptCurrentQuest(questManager);
                Assert.IsTrue(nextAccepted, "Failed to accept follow-up quest.");
                activeQuestToPersist = nextQuest.QuestId;
                Assert.IsTrue(questManager.IsQuestActive(activeQuestToPersist), "Follow-up quest should be active before save.");
            }

            Vector3 savedPosition = player.transform.position;
            float savedTime = dayNight.NormalizedTime;
            int savedRewardCount = primaryReward != null ? inventory.GetItemCount(primaryReward.Item.ItemId) : 0;

            bool saved = saveSystem.SaveGame();
            Assert.IsTrue(saved, "SaveGame() returned false.");

            player.transform.position = savedPosition + new Vector3(25f, 0f, 25f);
            dayNight.SetTimeNormalized(Mathf.Repeat(savedTime + 0.35f, 1f));

            if (primaryReward != null && savedRewardCount > 0)
            {
                inventory.RemoveItem(primaryReward.Item.ItemId, savedRewardCount);
            }

            bool loaded = saveSystem.LoadGame();
            Assert.IsTrue(loaded, "LoadGame() returned false.");
            yield return null;

            float positionDelta = Vector3.Distance(player.transform.position, savedPosition);
            Assert.Less(positionDelta, 0.35f, $"Player position did not restore. Delta={positionDelta:F3}");
            Assert.AreEqual(savedTime, dayNight.NormalizedTime, 0.01f, "World time did not restore from save.");

            if (primaryReward != null)
            {
                int restoredRewardCount = inventory.GetItemCount(primaryReward.Item.ItemId);
                Assert.AreEqual(savedRewardCount, restoredRewardCount, "Inventory reward count did not restore from save.");
            }

            if (!string.IsNullOrWhiteSpace(activeQuestToPersist))
            {
                Assert.IsTrue(questManager.IsQuestActive(activeQuestToPersist), "Active quest was not restored after load.");
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static QuestRewardDefinition GetFirstRewardWithItem(QuestDefinition quest)
        {
            if (quest == null || quest.Rewards == null)
            {
                return null;
            }

            for (int i = 0; i < quest.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];
                if (reward != null && reward.Item != null && reward.Amount > 0)
                {
                    return reward;
                }
            }

            return null;
        }

        private static IEnumerator KillZombies(Vector3 playerPosition, int requiredCount)
        {
            SpawnerZone spawner = Object.FindObjectOfType<SpawnerZone>();
            Assert.IsNotNull(spawner, "SpawnerZone missing in scene.");

            int needed = Mathf.Max(1, requiredCount);
            for (int i = 0; i < needed; i++)
            {
                spawner.SpawnZombie();
                yield return null;
                yield return new WaitForSeconds(0.25f);

                ZombieAI zombie = FindClosestAliveZombie(playerPosition);
                Assert.IsNotNull(zombie, "Could not find spawned zombie to kill.");

                HealthComponent health = zombie.GetComponent<HealthComponent>();
                Assert.IsNotNull(health, "Spawned zombie missing HealthComponent.");

                health.ApplyDamage(new DamageInfo(
                    9999f,
                    zombie.transform.position,
                    Vector3.up,
                    Vector3.forward,
                    null,
                    "tests.quest.kill"));

                yield return null;
                yield return new WaitForSeconds(0.15f);
            }
        }

        private static IEnumerator KillBoss()
        {
            BossSpawnManager bossManager = Object.FindObjectOfType<BossSpawnManager>();
            Assert.IsNotNull(bossManager, "BossSpawnManager missing in scene.");

            if (bossManager.ActiveBoss == null)
            {
                bossManager.RequestQuestSpawn();
                bossManager.SpawnBoss();
                yield return null;
                yield return new WaitForSeconds(0.3f);
            }

            BossAI boss = bossManager.ActiveBoss;
            Assert.IsNotNull(boss, "Boss was not spawned for defeat quest.");

            HealthComponent health = boss.GetComponent<HealthComponent>();
            Assert.IsNotNull(health, "Boss missing HealthComponent.");

            health.ApplyDamage(new DamageInfo(
                99999f,
                boss.transform.position,
                Vector3.up,
                Vector3.forward,
                null,
                "tests.quest.boss"));

            yield return null;
            yield return new WaitForSeconds(0.25f);
        }

        private static ItemDefinition FindItemDefinitionById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            ItemDefinition[] allItems = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemDefinition item = allItems[i];
                if (item != null && item.ItemId == itemId)
                {
                    return item;
                }
            }

            return null;
        }

        private static ZombieAI FindClosestAliveZombie(Vector3 from)
        {
            ZombieAI[] zombies = Object.FindObjectsOfType<ZombieAI>(true);
            ZombieAI best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < zombies.Length; i++)
            {
                ZombieAI zombie = zombies[i];
                if (zombie == null || zombie.IsDead || !zombie.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(from, zombie.transform.position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = zombie;
            }

            return best;
        }
    }
}
