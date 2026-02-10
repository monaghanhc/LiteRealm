using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class LootUIController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text itemsText;
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button closeButton;

        private LootContainer currentContainer;
        private InventoryComponent inventory;
        private GameEventHub eventHub;

        private void Awake()
        {
            if (takeAllButton != null)
            {
                takeAllButton.onClick.AddListener(TakeAll);
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

        public void OpenContainer(LootContainer container, InventoryComponent targetInventory, GameEventHub hub)
        {
            currentContainer = container;
            inventory = targetInventory;
            eventHub = hub;

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

            currentContainer = null;
            inventory = null;
            eventHub = null;
        }

        private void TakeAll()
        {
            if (currentContainer == null || inventory == null)
            {
                return;
            }

            currentContainer.TransferAllLoot(inventory, eventHub);
            Refresh();
        }

        private void Refresh()
        {
            if (currentContainer == null)
            {
                Close();
                return;
            }

            if (titleText != null)
            {
                titleText.text = $"Container: {currentContainer.ContainerId}";
            }

            if (itemsText != null)
            {
                if (currentContainer.CurrentLoot.Count == 0)
                {
                    itemsText.text = "Empty";
                }
                else
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder();
                    for (int i = 0; i < currentContainer.CurrentLoot.Count; i++)
                    {
                        ContainerLootStack stack = currentContainer.CurrentLoot[i];
                        if (stack?.Item == null || stack.Amount <= 0)
                        {
                            continue;
                        }

                        builder.AppendLine($"- {stack.Item.DisplayName} x{stack.Amount}");
                    }

                    itemsText.text = builder.ToString();
                }
            }
        }
    }
}
