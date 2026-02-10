using System;
using System.Collections.Generic;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Loot
{
    [Serializable]
    public struct LootStackState
    {
        public string ItemId;
        public int Amount;
    }

    [Serializable]
    public class LootContainerState
    {
        public string ContainerId;
        public bool Opened;
        public List<LootStackState> RemainingLoot = new List<LootStackState>();
    }

    [Serializable]
    public class ContainerLootStack
    {
        public ItemDefinition Item;
        public int Amount;
    }

    public class LootContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string containerId = "container.001";
        [SerializeField] private LootTable lootTable;
        [SerializeField] [Min(0)] private int rollCount = 3;
        [SerializeField] private bool singleUse = true;

        [Header("State")]
        [SerializeField] private bool startsOpened;

        private readonly List<ContainerLootStack> currentLoot = new List<ContainerLootStack>();
        private bool generated;
        private bool opened;

        public string ContainerId => containerId;
        public LootTable LootTable => lootTable;
        public IReadOnlyList<ContainerLootStack> CurrentLoot => currentLoot;
        public bool Opened => opened;
        public bool IsDepleted => currentLoot.Count == 0;

        private void Awake()
        {
            opened = startsOpened;
            if (opened)
            {
                generated = true;
            }
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (singleUse && opened && IsDepleted)
            {
                return "Container is empty";
            }

            return "Press E to open container";
        }

        public void Interact(PlayerInteractor interactor)
        {
            EnsureLootGenerated();
            opened = true;

            if (interactor != null && interactor.LootUI != null)
            {
                interactor.LootUI.OpenContainer(this, interactor.Inventory, interactor.EventHub);
                return;
            }

            if (interactor != null)
            {
                TransferAllLoot(interactor.Inventory, interactor.EventHub);
            }
        }

        public int TransferAllLoot(InventoryComponent inventory, GameEventHub eventHub)
        {
            if (inventory == null)
            {
                return 0;
            }

            int moved = 0;
            for (int i = currentLoot.Count - 1; i >= 0; i--)
            {
                ContainerLootStack stack = currentLoot[i];
                if (stack.Item == null || stack.Amount <= 0)
                {
                    currentLoot.RemoveAt(i);
                    continue;
                }

                int accepted = inventory.AddItemAndReturnAccepted(stack.Item, stack.Amount);
                if (accepted <= 0)
                {
                    continue;
                }

                moved += accepted;
                stack.Amount -= accepted;

                eventHub?.RaiseItemCollected(new ItemCollectedEvent
                {
                    ItemId = stack.Item.ItemId,
                    Amount = accepted,
                    Collector = inventory.gameObject
                });

                if (stack.Amount <= 0)
                {
                    currentLoot.RemoveAt(i);
                }
            }

            return moved;
        }

        public LootContainerState CaptureState()
        {
            LootContainerState state = new LootContainerState
            {
                ContainerId = containerId,
                Opened = opened
            };

            for (int i = 0; i < currentLoot.Count; i++)
            {
                ContainerLootStack stack = currentLoot[i];
                if (stack.Item == null || stack.Amount <= 0)
                {
                    continue;
                }

                state.RemainingLoot.Add(new LootStackState
                {
                    ItemId = stack.Item.ItemId,
                    Amount = stack.Amount
                });
            }

            return state;
        }

        public void RestoreState(LootContainerState state, ItemDatabase itemDatabase)
        {
            if (state == null || itemDatabase == null)
            {
                return;
            }

            opened = state.Opened;
            generated = true;
            currentLoot.Clear();

            if (state.RemainingLoot == null)
            {
                return;
            }

            for (int i = 0; i < state.RemainingLoot.Count; i++)
            {
                LootStackState entry = state.RemainingLoot[i];
                ItemDefinition item = itemDatabase.GetById(entry.ItemId);
                if (item == null || entry.Amount <= 0)
                {
                    continue;
                }

                currentLoot.Add(new ContainerLootStack
                {
                    Item = item,
                    Amount = entry.Amount
                });
            }
        }

        private void EnsureLootGenerated()
        {
            if (generated)
            {
                return;
            }

            generated = true;
            currentLoot.Clear();

            if (lootTable == null)
            {
                return;
            }

            List<LootRollResult> rolled = lootTable.Roll(rollCount);
            for (int i = 0; i < rolled.Count; i++)
            {
                LootRollResult result = rolled[i];
                if (result.Item == null || result.Amount <= 0)
                {
                    continue;
                }

                currentLoot.Add(new ContainerLootStack
                {
                    Item = result.Item,
                    Amount = result.Amount
                });
            }
        }
    }
}
