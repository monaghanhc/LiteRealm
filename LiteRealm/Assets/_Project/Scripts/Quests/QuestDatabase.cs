using System.Collections.Generic;
using UnityEngine;

namespace LiteRealm.Quests
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "LiteRealm/Quests/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        [SerializeField] private List<QuestDefinition> quests = new List<QuestDefinition>();

        private Dictionary<string, QuestDefinition> lookup;

        public IReadOnlyList<QuestDefinition> Quests => quests;

        public QuestDefinition GetById(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return null;
            }

            BuildLookupIfNeeded();
            lookup.TryGetValue(questId, out QuestDefinition definition);
            return definition;
        }

        private void OnValidate()
        {
            lookup = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, QuestDefinition>();
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    continue;
                }

                lookup[quest.QuestId] = quest;
            }
        }
    }
}
