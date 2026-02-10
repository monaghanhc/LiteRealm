using System.Collections.Generic;
using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Quests
{
    public enum NpcQuestState
    {
        None,
        Offer,
        InProgress,
        ReadyToTurnIn,
        AllCompleted
    }

    public class NPCQuestGiver : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName = "Survivor";
        [SerializeField] [TextArea] private string greeting = "Need supplies? I have work for you.";
        [SerializeField] private List<QuestDefinition> questLine = new List<QuestDefinition>();

        public string NpcName => npcName;
        public string Greeting => greeting;

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            return "Press E to talk";
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null || interactor.DialogueUI == null)
            {
                return;
            }

            interactor.DialogueUI.OpenNpc(this, interactor.QuestManager);
        }

        public NpcQuestState GetState(QuestManager questManager, out QuestDefinition currentQuest)
        {
            currentQuest = null;
            if (questManager == null)
            {
                return NpcQuestState.None;
            }

            for (int i = 0; i < questLine.Count; i++)
            {
                QuestDefinition quest = questLine[i];
                if (quest == null)
                {
                    continue;
                }

                if (questManager.IsQuestCompleted(quest.QuestId))
                {
                    continue;
                }

                currentQuest = quest;

                if (questManager.IsQuestActive(quest.QuestId))
                {
                    bool ready = questManager.IsQuestReadyToTurnIn(quest.QuestId);
                    return ready ? NpcQuestState.ReadyToTurnIn : NpcQuestState.InProgress;
                }

                return NpcQuestState.Offer;
            }

            return NpcQuestState.AllCompleted;
        }

        public bool AcceptCurrentQuest(QuestManager questManager)
        {
            NpcQuestState state = GetState(questManager, out QuestDefinition quest);
            return state == NpcQuestState.Offer && questManager.AcceptQuest(quest);
        }

        public bool TryTurnInCurrentQuest(QuestManager questManager, out string message)
        {
            message = "No quest to turn in.";
            NpcQuestState state = GetState(questManager, out QuestDefinition quest);
            if (state != NpcQuestState.ReadyToTurnIn || quest == null)
            {
                return false;
            }

            return questManager.TryTurnInQuest(quest.QuestId, out message);
        }
    }
}
