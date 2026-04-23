using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
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

        [Header("Nearby Fallback")]
        [SerializeField] private bool useNearbyFallback = true;
        [SerializeField] [Min(0.5f)] private float nearbyInteractionRadius = 2.25f;
        [SerializeField] [Range(-1f, 1f)] private float nearbyForwardDotThreshold = -0.25f;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode interactFallbackKey = KeyCode.E;

        [Header("References")]
        [SerializeField] private ExplorationInput input;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private InteractionPromptUI interactionPrompt;
        [SerializeField] private LootUIController lootUI;
        [SerializeField] private DialogueUIController dialogueUI;
        [SerializeField] private GameEventHub eventHub;

        private readonly Collider[] nearbyHits = new Collider[32];
        private IInteractable currentInteractable;

        public InventoryComponent Inventory => inventory;
        public QuestManager QuestManager => questManager;
        public LootUIController LootUI => lootUI;
        public DialogueUIController DialogueUI => dialogueUI;
        public GameEventHub EventHub => eventHub;
        public IInteractable CurrentInteractable => currentInteractable;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<ExplorationInput>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<InventoryComponent>();
            }

            if (questManager == null)
            {
                questManager = FindObjectOfType<QuestManager>();
            }

            if (interactionPrompt == null)
            {
                interactionPrompt = FindObjectOfType<InteractionPromptUI>(true);
            }

            if (lootUI == null)
            {
                lootUI = FindObjectOfType<LootUIController>(true);
            }

            if (dialogueUI == null)
            {
                dialogueUI = FindObjectOfType<DialogueUIController>(true);
            }

            if (eventHub == null)
            {
                eventHub = FindObjectOfType<GameEventHub>();
            }

            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (playerStats != null && playerStats.IsDead)
            {
                currentInteractable = null;
                interactionPrompt?.Hide();
                return;
            }

            FindInteractable();
            if (ReadInteractPressedThisFrame() && currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }

        private void FindInteractable()
        {
            currentInteractable = null;

            if (TryFindRaycastInteractable(out IInteractable rayInteractable))
            {
                currentInteractable = rayInteractable;
            }
            else if (useNearbyFallback && TryFindNearbyInteractable(out IInteractable nearbyInteractable))
            {
                currentInteractable = nearbyInteractable;
            }

            if (currentInteractable != null)
            {
                string prompt = currentInteractable.GetInteractionPrompt(this);
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    interactionPrompt?.Show(prompt);
                    return;
                }
            }

            interactionPrompt?.Hide();
        }

        private bool TryFindRaycastInteractable(out IInteractable interactable)
        {
            interactable = null;
            if (interactionCamera == null)
            {
                return false;
            }

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                interactable = FindInteractableOnHit(hit.collider);
                return interactable != null;
            }

            return false;
        }

        private bool TryFindNearbyInteractable(out IInteractable interactable)
        {
            interactable = null;

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            int count = Physics.OverlapSphereNonAlloc(
                origin,
                nearbyInteractionRadius,
                nearbyHits,
                interactionMask,
                QueryTriggerInteraction.Collide);

            float bestScore = float.MaxValue;
            Vector3 forward = interactionCamera != null ? interactionCamera.transform.forward : transform.forward;

            for (int i = 0; i < count; i++)
            {
                Collider hit = nearbyHits[i];
                if (hit == null)
                {
                    continue;
                }

                IInteractable candidate = FindInteractableOnHit(hit);
                if (candidate == null)
                {
                    continue;
                }

                Transform candidateTransform = GetInteractableTransform(candidate);
                if (candidateTransform == null)
                {
                    continue;
                }

                Vector3 toCandidate = candidateTransform.position - origin;
                float distance = toCandidate.magnitude;
                if (distance > nearbyInteractionRadius)
                {
                    continue;
                }

                float forwardDot = toCandidate.sqrMagnitude > 0.001f
                    ? Vector3.Dot(forward.normalized, toCandidate.normalized)
                    : 1f;
                if (forwardDot < nearbyForwardDotThreshold && distance > 1.1f)
                {
                    continue;
                }

                string prompt = candidate.GetInteractionPrompt(this);
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    continue;
                }

                float score = distance - Mathf.Max(0f, forwardDot) * 0.35f;
                if (candidate is WorldItemPickup)
                {
                    score -= 0.5f;
                }

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                interactable = candidate;
            }

            return interactable != null;
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

        private static Transform GetInteractableTransform(IInteractable interactable)
        {
            Component component = interactable as Component;
            return component != null ? component.transform : null;
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
