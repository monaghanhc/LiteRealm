using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.World
{
    public class WorldObjectiveInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string objectiveId = "objective.id";
        [SerializeField] private string promptText = "Press E to search";
        [SerializeField] [TextArea] private string interactionMessage = "Objective updated.";
        [SerializeField] [Min(1)] private int amount = 1;
        [SerializeField] private bool oneUseOnly = true;
        [SerializeField] private GameEventHub eventHub;

        private bool used;

        private void Awake()
        {
            if (eventHub == null)
            {
                eventHub = FindFirstObjectByType<GameEventHub>();
            }

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            return used && oneUseOnly ? string.Empty : promptText;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (used && oneUseOnly)
            {
                return;
            }

            used = true;
            eventHub?.RaiseObjectiveSignaled(new ObjectiveSignalEvent
            {
                ObjectiveId = objectiveId,
                Amount = Mathf.Max(1, amount),
                Sender = gameObject,
                Instigator = interactor != null ? interactor.gameObject : null
            });

            if (!string.IsNullOrWhiteSpace(interactionMessage))
            {
                Debug.Log($"[Objective] {interactionMessage}", this);
            }
        }
    }
}
