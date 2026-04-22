using System;
using System.Collections.Generic;
using LiteRealm.Inventory;
using UnityEngine;

namespace LiteRealm.Quests
{
    [Serializable]
    public class QuestObjectiveDefinition
    {
        public QuestType Type;
        public string TargetId;
        [Min(1)] public int RequiredCount = 1;
        [Min(0)] public int ExperienceReward;
        public bool Optional;
        [TextArea] public string Description;
    }

    [Serializable]
    public class QuestRewardDefinition
    {
        public ItemDefinition Item;
        [Min(1)] public int Amount = 1;
        [Min(0)] public int Experience;
    }

    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "LiteRealm/Quests/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId = "quest.id";
        [SerializeField] private string title = "New Quest";
        [SerializeField] [TextArea] private string description = "";
        [SerializeField] private string storyAct = "Act I";
        [SerializeField] private string contractType = "Survival Contract";
        [SerializeField] [Min(1)] private int requiredLevel = 1;
        [SerializeField] [Range(1, 5)] private int riskRating = 1;
        [SerializeField] private string locationHint = "";
        [SerializeField] private List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private List<QuestRewardDefinition> rewards = new List<QuestRewardDefinition>();

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public string StoryAct => storyAct;
        public string ContractType => contractType;
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
        public int RiskRating => Mathf.Clamp(riskRating, 1, 5);
        public string LocationHint => locationHint;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => objectives;
        public IReadOnlyList<QuestRewardDefinition> Rewards => rewards;
    }
}
