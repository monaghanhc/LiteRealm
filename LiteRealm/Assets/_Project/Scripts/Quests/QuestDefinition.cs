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
        [SerializeField] private List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private List<QuestRewardDefinition> rewards = new List<QuestRewardDefinition>();

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => objectives;
        public IReadOnlyList<QuestRewardDefinition> Rewards => rewards;
    }
}
