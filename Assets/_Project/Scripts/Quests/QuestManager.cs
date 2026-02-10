using System;
using System.Collections.Generic;
using LiteRealm.AI;
using LiteRealm.Core;
using LiteRealm.Inventory;
using UnityEngine;

namespace LiteRealm.Quests
{
    [Serializable]
    public class QuestRuntime
    {
        public string QuestId;
        public List<int> Progress = new List<int>();
    }

    [Serializable]
    public class QuestManagerState
    {
        public List<QuestRuntime> ActiveQuests = new List<QuestRuntime>();
        public List<string> CompletedQuestIds = new List<string>();
        public int TotalExperience;
    }

    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private BossSpawnManager bossSpawnManager;
        [SerializeField] [Min(0)] private int totalExperience;

        private readonly List<QuestRuntime> activeQuests = new List<QuestRuntime>();
        private readonly HashSet<string> completedQuestIds = new HashSet<string>();

        public event Action QuestsChanged;
        public event Action<int, int> ExperienceChanged;

        public IReadOnlyList<QuestRuntime> ActiveQuests => activeQuests;
        public int TotalExperience => Mathf.Max(0, totalExperience);

        private void OnEnable()
        {
            if (eventHub != null)
            {
                eventHub.EnemyKilled += OnEnemyKilled;
                eventHub.BossKilled += OnBossKilled;
                eventHub.ItemCollected += OnItemCollected;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged += OnInventoryChanged;
            }
        }

        private void OnDisable()
        {
            if (eventHub != null)
            {
                eventHub.EnemyKilled -= OnEnemyKilled;
                eventHub.BossKilled -= OnBossKilled;
                eventHub.ItemCollected -= OnItemCollected;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -= OnInventoryChanged;
            }
        }

        public bool AcceptQuest(QuestDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.QuestId))
            {
                return false;
            }

            if (IsQuestActive(definition.QuestId) || completedQuestIds.Contains(definition.QuestId))
            {
                return false;
            }

            QuestRuntime runtime = new QuestRuntime
            {
                QuestId = definition.QuestId,
                Progress = new List<int>()
            };

            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[i];
                int startProgress = 0;
                if (objective.Type == QuestType.RetrieveItem && inventory != null)
                {
                    startProgress = inventory.GetItemCount(objective.TargetId);
                }

                runtime.Progress.Add(Mathf.Clamp(startProgress, 0, objective.RequiredCount));

                if (objective.Type == QuestType.DefeatBoss && bossSpawnManager != null)
                {
                    bossSpawnManager.RequestQuestSpawn();
                }
            }

            activeQuests.Add(runtime);
            QuestsChanged?.Invoke();
            return true;
        }

        public bool IsQuestActive(string questId)
        {
            return FindRuntime(questId) != null;
        }

        public bool IsQuestCompleted(string questId)
        {
            return completedQuestIds.Contains(questId);
        }

        public bool TryGetQuestDefinition(string questId, out QuestDefinition definition)
        {
            definition = questDatabase != null ? questDatabase.GetById(questId) : null;
            return definition != null;
        }

        public bool IsQuestReadyToTurnIn(string questId)
        {
            QuestRuntime runtime = FindRuntime(questId);
            if (runtime == null)
            {
                return false;
            }

            return IsRuntimeComplete(runtime);
        }

        public bool TryTurnInQuest(string questId, out string message)
        {
            message = "Quest not active.";
            QuestRuntime runtime = FindRuntime(questId);
            if (runtime == null)
            {
                return false;
            }

            QuestDefinition definition = questDatabase != null ? questDatabase.GetById(questId) : null;
            if (definition == null)
            {
                message = "Quest data missing.";
                return false;
            }

            if (!IsRuntimeComplete(runtime))
            {
                message = "Objectives are not complete yet.";
                return false;
            }

            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[i];
                if (objective.Type != QuestType.RetrieveItem || inventory == null)
                {
                    continue;
                }

                int available = inventory.GetItemCount(objective.TargetId);
                if (available < objective.RequiredCount)
                {
                    message = "Required items are missing from inventory.";
                    return false;
                }
            }

            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[i];
                if (objective.Type == QuestType.RetrieveItem && inventory != null)
                {
                    inventory.RemoveItem(objective.TargetId, objective.RequiredCount);
                }
            }

            for (int i = 0; i < definition.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = definition.Rewards[i];
                if (reward == null || reward.Item == null || reward.Amount <= 0 || inventory == null)
                {
                    continue;
                }

                inventory.AddItem(reward.Item, reward.Amount);
            }

            int gainedExperience = 0;
            for (int i = 0; i < definition.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = definition.Rewards[i];
                if (reward == null || reward.Experience <= 0)
                {
                    continue;
                }

                gainedExperience += reward.Experience;
            }

            if (gainedExperience > 0)
            {
                totalExperience += gainedExperience;
                ExperienceChanged?.Invoke(totalExperience, gainedExperience);
            }

            activeQuests.Remove(runtime);
            completedQuestIds.Add(questId);
            message = "Quest completed.";
            QuestsChanged?.Invoke();
            return true;
        }

        public QuestManagerState CaptureState()
        {
            QuestManagerState state = new QuestManagerState
            {
                TotalExperience = totalExperience
            };
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestRuntime clone = new QuestRuntime
                {
                    QuestId = runtime.QuestId,
                    Progress = new List<int>(runtime.Progress)
                };

                state.ActiveQuests.Add(clone);
            }

            foreach (string questId in completedQuestIds)
            {
                state.CompletedQuestIds.Add(questId);
            }

            return state;
        }

        public void RestoreState(QuestManagerState state)
        {
            activeQuests.Clear();
            completedQuestIds.Clear();
            totalExperience = 0;

            if (state == null)
            {
                QuestsChanged?.Invoke();
                ExperienceChanged?.Invoke(totalExperience, 0);
                return;
            }

            if (state.ActiveQuests != null)
            {
                for (int i = 0; i < state.ActiveQuests.Count; i++)
                {
                    QuestRuntime runtime = state.ActiveQuests[i];
                    QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                    if (definition == null)
                    {
                        continue;
                    }

                    QuestRuntime validRuntime = new QuestRuntime
                    {
                        QuestId = runtime.QuestId,
                        Progress = new List<int>()
                    };

                    for (int j = 0; j < definition.Objectives.Count; j++)
                    {
                        int progress = runtime.Progress != null && j < runtime.Progress.Count ? runtime.Progress[j] : 0;
                        validRuntime.Progress.Add(Mathf.Clamp(progress, 0, definition.Objectives[j].RequiredCount));
                    }

                    activeQuests.Add(validRuntime);
                }
            }

            if (state.CompletedQuestIds != null)
            {
                for (int i = 0; i < state.CompletedQuestIds.Count; i++)
                {
                    string questId = state.CompletedQuestIds[i];
                    if (!string.IsNullOrWhiteSpace(questId))
                    {
                        completedQuestIds.Add(questId);
                    }
                }
            }

            totalExperience = Mathf.Max(0, state.TotalExperience);
            RecalculateRetrieveObjectives();
            QuestsChanged?.Invoke();
            ExperienceChanged?.Invoke(totalExperience, 0);
        }

        public string BuildQuestLogText()
        {
            if (activeQuests.Count == 0)
            {
                return $"No active quests.\nXP: {TotalExperience}";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine($"XP: {TotalExperience}");
            builder.AppendLine();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                if (definition == null)
                {
                    continue;
                }

                builder.AppendLine($"{definition.Title}");
                for (int j = 0; j < definition.Objectives.Count; j++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[j];
                    int progress = runtime.Progress != null && j < runtime.Progress.Count ? runtime.Progress[j] : 0;
                    string objectiveText = string.IsNullOrWhiteSpace(objective.Description)
                        ? $"- {objective.Type}: {progress}/{objective.RequiredCount}"
                        : $"- {objective.Description}: {progress}/{objective.RequiredCount}";
                    builder.AppendLine(objectiveText);
                }

                if (IsRuntimeComplete(runtime))
                {
                    builder.AppendLine("Ready to turn in");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private QuestRuntime FindRuntime(string questId)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                if (activeQuests[i].QuestId == questId)
                {
                    return activeQuests[i];
                }
            }

            return null;
        }

        private bool IsRuntimeComplete(QuestRuntime runtime)
        {
            QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
            if (definition == null)
            {
                return false;
            }

            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[i];
                int progress = runtime.Progress != null && i < runtime.Progress.Count ? runtime.Progress[i] : 0;
                if (progress < objective.RequiredCount)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnEnemyKilled(EnemyKilledEvent data)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                if (definition == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.Objectives.Count; j++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[j];
                    if (objective.Type != QuestType.KillZombies)
                    {
                        continue;
                    }

                    bool idMatches = string.IsNullOrWhiteSpace(objective.TargetId) || objective.TargetId == data.EnemyId;
                    if (!idMatches)
                    {
                        continue;
                    }

                    runtime.Progress[j] = Mathf.Min(objective.RequiredCount, runtime.Progress[j] + 1);
                }
            }

            QuestsChanged?.Invoke();
        }

        private void OnBossKilled(BossKilledEvent data)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                if (definition == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.Objectives.Count; j++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[j];
                    if (objective.Type != QuestType.DefeatBoss)
                    {
                        continue;
                    }

                    bool idMatches = string.IsNullOrWhiteSpace(objective.TargetId) || objective.TargetId == data.BossId;
                    if (!idMatches)
                    {
                        continue;
                    }

                    runtime.Progress[j] = objective.RequiredCount;
                }
            }

            QuestsChanged?.Invoke();
        }

        private void OnItemCollected(ItemCollectedEvent _)
        {
            RecalculateRetrieveObjectives();
            QuestsChanged?.Invoke();
        }

        private void OnInventoryChanged()
        {
            RecalculateRetrieveObjectives();
            QuestsChanged?.Invoke();
        }

        private void RecalculateRetrieveObjectives()
        {
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                if (definition == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.Objectives.Count; j++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[j];
                    if (objective.Type != QuestType.RetrieveItem)
                    {
                        continue;
                    }

                    int count = inventory.GetItemCount(objective.TargetId);
                    runtime.Progress[j] = Mathf.Clamp(count, 0, objective.RequiredCount);
                }
            }
        }
    }
}
