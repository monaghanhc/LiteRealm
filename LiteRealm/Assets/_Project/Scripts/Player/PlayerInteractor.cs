using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Quests;
using LiteRealm.UI;
using UnityEngine;

namespace LiteRealm.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float interactionDistance = 3.5f;
        [SerializeField] private LayerMask interactionMask = ~0;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode interactFallbackKey = KeyCode.E;

        [Header("References")]
        [SerializeField] private ExplorationInput input;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private InteractionPromptUI interactionPrompt;
        [SerializeField] private LootUIController lootUI;
        [SerializeField] private DialogueUIController dialogueUI;
        [SerializeField] private GameEventHub eventHub;

        private IInteractable currentInteractable;

        public InventoryComponent Inventory => inventory;
        public QuestManager QuestManager => questManager;
        public LootUIController LootUI => lootUI;
        public DialogueUIController DialogueUI => dialogueUI;
        public GameEventHub EventHub => eventHub;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<ExplorationInput>();
            }

            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }

        private void Update()
        {
            FindInteractable();
            if (ReadInteractPressedThisFrame() && currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }

        private void FindInteractable()
        {
            currentInteractable = null;
            if (interactionCamera == null)
            {
                interactionPrompt?.Hide();
                return;
            }

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                currentInteractable = FindInteractableOnHit(hit.collider);
                if (currentInteractable != null)
                {
                    string prompt = currentInteractable.GetInteractionPrompt(this);
                    if (!string.IsNullOrWhiteSpace(prompt))
                    {
                        interactionPrompt?.Show(prompt);
                        return;
                    }
                }
            }

            interactionPrompt?.Hide();
        }

        private static IInteractable FindInteractableOnHit(Collider hitCollider)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private bool ReadInteractPressedThisFrame()
        {
            if (input != null)
            {
                return input.InteractPressedThisFrame();
            }

            return Input.GetKeyDown(interactFallbackKey);
        }
    }
}
