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
        public List<bool> ObjectiveRewardsClaimed = new List<bool>();
    }

    [Serializable]
    public class QuestManagerState
    {
        public List<QuestRuntime> ActiveQuests = new List<QuestRuntime>();
        public List<string> CompletedQuestIds = new List<string>();
        public int TotalExperience;
        public int CurrentLevel = 1;
    }

    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private BossSpawnManager bossSpawnManager;
        [SerializeField] [Min(0)] private int totalExperience;

        [Header("Leveling")]
        [SerializeField] [Min(1)] private int startingLevel = 1;
        [SerializeField] [Min(25)] private int baseExperienceToLevel = 250;
        [SerializeField] [Min(1.01f)] private float levelExperienceGrowth = 1.42f;
        [SerializeField] private string[] rankTitles =
        {
            "Drifter",
            "Runner",
            "Scout",
            "Operator",
            "Warden",
            "Specter",
            "Signal Breaker"
        };

        private readonly List<QuestRuntime> activeQuests = new List<QuestRuntime>();
        private readonly HashSet<string> completedQuestIds = new HashSet<string>();

        public event Action QuestsChanged;
        public event Action<int, int> ExperienceChanged;
        public event Action<int, string> LevelChanged;

        public IReadOnlyList<QuestRuntime> ActiveQuests => activeQuests;
        public int TotalExperience => Mathf.Max(0, totalExperience);
        public int CurrentLevel { get; private set; } = 1;
        public string CurrentRankTitle => GetRankTitle(CurrentLevel);
        public int ExperienceForCurrentLevel => GetExperienceRequiredForLevel(CurrentLevel);
        public int ExperienceForNextLevel => GetExperienceRequiredForLevel(CurrentLevel + 1);
        public int ExperienceIntoCurrentLevel => Mathf.Max(0, TotalExperience - ExperienceForCurrentLevel);
        public int ExperienceNeededForNextLevel => Mathf.Max(0, ExperienceForNextLevel - TotalExperience);

        private void Awake()
        {
            CurrentLevel = Mathf.Max(1, startingLevel);
            RecalculateLevel(false);
        }

        private void OnEnable()
        {
            if (eventHub != null)
            {
                eventHub.EnemyKilled += OnEnemyKilled;
                eventHub.BossKilled += OnBossKilled;
                eventHub.ItemCollected += OnItemCollected;
                eventHub.ObjectiveSignaled += OnObjectiveSignaled;
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
                eventHub.ObjectiveSignaled -= OnObjectiveSignaled;
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

            if (!MeetsLevelRequirement(definition))
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
                Progress = new List<int>(),
                ObjectiveRewardsClaimed = new List<bool>()
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
                runtime.ObjectiveRewardsClaimed.Add(false);

                if (objective.Type == QuestType.DefeatBoss && bossSpawnManager != null)
                {
                    bossSpawnManager.RequestQuestSpawn();
                }
            }

            activeQuests.Add(runtime);
            EvaluateObjectiveRewards(runtime, definition);
            QuestsChanged?.Invoke();
            return true;
        }

        public bool MeetsLevelRequirement(QuestDefinition definition)
        {
            return definition == null || CurrentLevel >= definition.RequiredLevel;
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
                if (objective.Type != QuestType.RetrieveItem || objective.Optional || inventory == null)
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
                if (objective.Type == QuestType.RetrieveItem && !objective.Optional && inventory != null)
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

            AwardExperience(gainedExperience);

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
                TotalExperience = totalExperience,
                CurrentLevel = this.CurrentLevel
            };
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestRuntime clone = new QuestRuntime
                {
                    QuestId = runtime.QuestId,
                    Progress = new List<int>(runtime.Progress),
                    ObjectiveRewardsClaimed = new List<bool>(runtime.ObjectiveRewardsClaimed)
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
                CurrentLevel = Mathf.Max(1, startingLevel);
                QuestsChanged?.Invoke();
                ExperienceChanged?.Invoke(totalExperience, 0);
                LevelChanged?.Invoke(CurrentLevel, CurrentRankTitle);
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
                        Progress = new List<int>(),
                        ObjectiveRewardsClaimed = new List<bool>()
                    };

                    for (int j = 0; j < definition.Objectives.Count; j++)
                    {
                        int progress = runtime.Progress != null && j < runtime.Progress.Count ? runtime.Progress[j] : 0;
                        bool claimed = runtime.ObjectiveRewardsClaimed != null
                                       && j < runtime.ObjectiveRewardsClaimed.Count
                                       && runtime.ObjectiveRewardsClaimed[j];
                        validRuntime.Progress.Add(Mathf.Clamp(progress, 0, definition.Objectives[j].RequiredCount));
                        validRuntime.ObjectiveRewardsClaimed.Add(claimed);
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
            RecalculateLevel(false);
            RecalculateRetrieveObjectives();
            QuestsChanged?.Invoke();
            ExperienceChanged?.Invoke(totalExperience, 0);
            LevelChanged?.Invoke(CurrentLevel, CurrentRankTitle);
        }

        public string BuildQuestLogText()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine($"{CurrentRankTitle}  Level {CurrentLevel}");
            builder.AppendLine($"XP {TotalExperience}/{ExperienceForNextLevel}  Next: {ExperienceNeededForNextLevel}");

            if (activeQuests.Count == 0)
            {
                builder.AppendLine();
                builder.AppendLine("No active contracts.");
                return builder.ToString();
            }

            builder.AppendLine();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestRuntime runtime = activeQuests[i];
                QuestDefinition definition = questDatabase != null ? questDatabase.GetById(runtime.QuestId) : null;
                if (definition == null)
                {
                    continue;
                }

                builder.AppendLine($"{definition.StoryAct} - {definition.Title}");
                builder.AppendLine($"{definition.ContractType} | Risk {definition.RiskRating}/5");
                if (!string.IsNullOrWhiteSpace(definition.LocationHint))
                {
                    builder.AppendLine($"Location: {definition.LocationHint}");
                }

                for (int j = 0; j < definition.Objectives.Count; j++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[j];
                    int progress = runtime.Progress != null && j < runtime.Progress.Count ? runtime.Progress[j] : 0;
                    bool complete = progress >= objective.RequiredCount;
                    string marker = complete ? "[x]" : "[ ]";
                    string optional = objective.Optional ? " optional" : "";
                    string reward = objective.ExperienceReward > 0 ? $" +{objective.ExperienceReward} XP" : "";
                    string objectiveText = string.IsNullOrWhiteSpace(objective.Description)
                        ? $"{objective.Type}: {progress}/{objective.RequiredCount}"
                        : $"{objective.Description}: {progress}/{objective.RequiredCount}";
                    builder.AppendLine($"{marker} {objectiveText}{optional}{reward}");
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
                if (objective.Optional)
                {
                    continue;
                }

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
            bool changed = false;
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

                    int previous = runtime.Progress[j];
                    runtime.Progress[j] = Mathf.Min(objective.RequiredCount, runtime.Progress[j] + 1);
                    changed |= previous != runtime.Progress[j];
                }

                changed |= EvaluateObjectiveRewards(runtime, definition);
            }

            if (changed)
            {
                QuestsChanged?.Invoke();
            }
        }

        private void OnBossKilled(BossKilledEvent data)
        {
            bool changed = false;
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

                    int previous = runtime.Progress[j];
                    runtime.Progress[j] = objective.RequiredCount;
                    changed |= previous != runtime.Progress[j];
                }

                changed |= EvaluateObjectiveRewards(runtime, definition);
            }

            if (changed)
            {
                QuestsChanged?.Invoke();
            }
        }

        private void OnObjectiveSignaled(ObjectiveSignalEvent data)
        {
            if (string.IsNullOrWhiteSpace(data.ObjectiveId))
            {
                return;
            }

            bool changed = false;
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
                    if (objective.Type != QuestType.RecoverIntel
                        && objective.Type != QuestType.SecureLocation
                        && objective.Type != QuestType.ActivateSignal)
                    {
                        continue;
                    }

                    if (objective.TargetId != data.ObjectiveId)
                    {
                        continue;
                    }

                    int previous = runtime.Progress[j];
                    runtime.Progress[j] = Mathf.Min(objective.RequiredCount, runtime.Progress[j] + Mathf.Max(1, data.Amount));
                    changed |= previous != runtime.Progress[j];
                }

                changed |= EvaluateObjectiveRewards(runtime, definition);
            }

            if (changed)
            {
                QuestsChanged?.Invoke();
            }
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

                EvaluateObjectiveRewards(runtime, definition);
            }
        }

        private bool EvaluateObjectiveRewards(QuestRuntime runtime, QuestDefinition definition)
        {
            if (runtime == null || definition == null)
            {
                return false;
            }

            EnsureObjectiveRewardFlags(runtime, definition);
            bool changed = false;
            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[i];
                int progress = runtime.Progress != null && i < runtime.Progress.Count ? runtime.Progress[i] : 0;
                if (progress < objective.RequiredCount || runtime.ObjectiveRewardsClaimed[i])
                {
                    continue;
                }

                runtime.ObjectiveRewardsClaimed[i] = true;
                AwardExperience(objective.ExperienceReward);
                changed = true;
            }

            return changed;
        }

        private static void EnsureObjectiveRewardFlags(QuestRuntime runtime, QuestDefinition definition)
        {
            if (runtime.ObjectiveRewardsClaimed == null)
            {
                runtime.ObjectiveRewardsClaimed = new List<bool>();
            }

            while (runtime.ObjectiveRewardsClaimed.Count < definition.Objectives.Count)
            {
                runtime.ObjectiveRewardsClaimed.Add(false);
            }
        }

        private void AwardExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            totalExperience = Mathf.Max(0, totalExperience + amount);
            int previousLevel = CurrentLevel;
            RecalculateLevel(false);
            ExperienceChanged?.Invoke(totalExperience, amount);
            if (CurrentLevel != previousLevel)
            {
                LevelChanged?.Invoke(CurrentLevel, CurrentRankTitle);
            }
        }

        private void RecalculateLevel(bool allowLevelUpEvent)
        {
            int previous = CurrentLevel;
            int level = Mathf.Max(1, startingLevel);
            while (TotalExperience >= GetExperienceRequiredForLevel(level + 1))
            {
                level++;
            }

            CurrentLevel = level;
            if (allowLevelUpEvent && CurrentLevel != previous)
            {
                LevelChanged?.Invoke(CurrentLevel, CurrentRankTitle);
            }
        }

        private int GetExperienceRequiredForLevel(int level)
        {
            level = Mathf.Max(1, level);
            if (level <= 1)
            {
                return 0;
            }

            int total = 0;
            int perLevel = Mathf.Max(25, baseExperienceToLevel);
            for (int i = 2; i <= level; i++)
            {
                total += perLevel;
                perLevel = Mathf.CeilToInt(perLevel * Mathf.Max(1.01f, levelExperienceGrowth));
            }

            return total;
        }

        private string GetRankTitle(int level)
        {
            if (rankTitles == null || rankTitles.Length == 0)
            {
                return "Survivor";
            }

            int index = Mathf.Clamp(level - 1, 0, rankTitles.Length - 1);
            return string.IsNullOrWhiteSpace(rankTitles[index]) ? "Survivor" : rankTitles[index];
        }
    }
}
