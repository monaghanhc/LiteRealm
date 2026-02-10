using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Loot
{
    public class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] [Min(1)] private int quantity = 1;
        [SerializeField] private bool spinInPlace = true;
        [SerializeField] private float spinSpeed = 50f;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        private void Awake()
        {
            EnsureInteractionLayerAndTag();
        }

        private void Update()
        {
            if (!spinInPlace)
            {
                return;
            }

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        public void Configure(ItemDefinition definition, int amount)
        {
            EnsureInteractionLayerAndTag();
            item = definition;
            quantity = Mathf.Max(1, amount);
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (item == null)
            {
                return string.Empty;
            }

            return $"Press E to pick up {item.DisplayName} x{quantity}";
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null || interactor.Inventory == null || item == null || quantity <= 0)
            {
                return;
            }

            int accepted = interactor.Inventory.AddItemAndReturnAccepted(item, quantity);
            if (accepted <= 0)
            {
                return;
            }

            quantity -= accepted;
            interactor.EventHub?.RaiseItemCollected(new ItemCollectedEvent
            {
                ItemId = item.ItemId,
                Amount = accepted,
                Collector = interactor.gameObject
            });

            if (quantity <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void EnsureInteractionLayerAndTag()
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }

            if (gameObject.CompareTag("Untagged"))
            {
                try
                {
                    gameObject.tag = "Loot";
                }
                catch
                {
                    // Tag may not exist in project settings yet; safe to ignore.
                }
            }
        }
    }
}
