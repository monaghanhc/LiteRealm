using LiteRealm.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class DialogueUIController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text npcNameText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text questTitleText;
        [SerializeField] private Text questDescriptionText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button turnInButton;
        [SerializeField] private Button closeButton;

        private NPCQuestGiver currentNpc;
        private QuestManager questManager;

        private void Awake()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(Close);
            }

            if (turnInButton != null)
            {
                turnInButton.onClick.AddListener(OnTurnInClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void OpenNpc(NPCQuestGiver npc, QuestManager manager)
        {
            currentNpc = npc;
            questManager = manager;

            if (root != null)
            {
                root.SetActive(true);
            }

            Refresh();
        }

        public void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            currentNpc = null;
            questManager = null;
        }

        private void OnAcceptClicked()
        {
            if (currentNpc != null && questManager != null)
            {
                currentNpc.AcceptCurrentQuest(questManager);
                Refresh();
            }
        }

        private void OnTurnInClicked()
        {
            if (currentNpc != null && questManager != null)
            {
                bool turnedIn = currentNpc.TryTurnInCurrentQuest(questManager, out string message);
                if (!turnedIn && bodyText != null)
                {
                    bodyText.text = message;
                }

                Refresh();
            }
        }

        private void Refresh()
        {
            if (currentNpc == null || questManager == null)
            {
                return;
            }

            if (npcNameText != null)
            {
                npcNameText.text = currentNpc.NpcName;
            }

            NpcQuestState state = currentNpc.GetState(questManager, out QuestDefinition quest);

            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(state == NpcQuestState.Offer);
            }

            if (declineButton != null)
            {
                declineButton.gameObject.SetActive(state == NpcQuestState.Offer || state == NpcQuestState.InProgress || state == NpcQuestState.ReadyToTurnIn);
            }

            if (turnInButton != null)
            {
                turnInButton.gameObject.SetActive(state == NpcQuestState.ReadyToTurnIn);
            }

            if (questTitleText != null)
            {
                questTitleText.text = quest != null ? quest.Title : "";
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.text = BuildQuestDetails(quest);
            }

            if (bodyText == null)
            {
                return;
            }

            switch (state)
            {
                case NpcQuestState.Offer:
                    bodyText.text = currentNpc.Greeting + "\n\nDo you accept this quest?";
                    break;
                case NpcQuestState.InProgress:
                    bodyText.text = currentNpc.Greeting + "\n\nKeep going. Objectives are still incomplete.";
                    break;
                case NpcQuestState.ReadyToTurnIn:
                    bodyText.text = currentNpc.Greeting + "\n\nGreat work. I can complete this quest now.";
                    break;
                case NpcQuestState.AllCompleted:
                    bodyText.text = "Thanks again. I have no more tasks right now.";
                    break;
                default:
                    bodyText.text = currentNpc.Greeting;
                    break;
            }
        }

        private static string BuildQuestDetails(QuestDefinition quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine(quest.Description);
            builder.AppendLine();
            builder.AppendLine("Objectives:");
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];
                if (objective == null)
                {
                    continue;
                }

                string text = string.IsNullOrWhiteSpace(objective.Description)
                    ? $"{objective.Type} x{objective.RequiredCount}"
                    : $"{objective.Description} x{objective.RequiredCount}";
                builder.AppendLine($"- {text}");
            }

            bool hasRewards = false;
            for (int i = 0; i < quest.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];
                if (reward != null && ((reward.Item != null && reward.Amount > 0) || reward.Experience > 0))
                {
                    hasRewards = true;
                    break;
                }
            }

            if (!hasRewards)
            {
                return builder.ToString();
            }

            builder.AppendLine();
            builder.AppendLine("Rewards:");
            for (int i = 0; i < quest.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];
                if (reward == null)
                {
                    continue;
                }

                if (reward.Item != null && reward.Amount > 0)
                {
                    builder.AppendLine($"- {reward.Item.DisplayName} x{reward.Amount}");
                }

                if (reward.Experience > 0)
                {
                    builder.AppendLine($"- XP +{reward.Experience}");
                }
            }

            return builder.ToString();
        }
    }
}
