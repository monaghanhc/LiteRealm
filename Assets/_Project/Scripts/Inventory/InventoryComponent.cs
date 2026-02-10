using System;
using System.Collections.Generic;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Inventory
{
    public class InventoryComponent : MonoBehaviour
    {
        [SerializeField] [Min(1)] private int slotCount = 20;
        [SerializeField] [Range(1, 10)] private int hotbarSlotCount = 4;

        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public event Action InventoryChanged;

        public int SlotCount => slots.Count;
        public int HotbarSlotCount => Mathf.Clamp(hotbarSlotCount, 1, Mathf.Max(1, slots.Count));
        public IReadOnlyList<InventorySlot> Slots => slots;

        private void Awake()
        {
            EnsureSlotCount();
        }

        public bool AddItem(ItemDefinition item, int amount)
        {
            return AddItemAndReturnAccepted(item, amount) == amount;
        }

        public int AddItemAndReturnAccepted(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return 0;
            }

            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.Item == item && slot.Quantity < item.MaxStack)
                {
                    remaining = slot.Add(item, remaining);
                    if (remaining <= 0)
                    {
                        InventoryChanged?.Invoke();
                        return amount;
                    }
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    remaining = slot.Add(item, remaining);
                    if (remaining <= 0)
                    {
                        InventoryChanged?.Invoke();
                        return amount;
                    }
                }
            }

            int accepted = amount - remaining;
            if (accepted > 0)
            {
                InventoryChanged?.Invoke();
            }

            return accepted;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            int remaining = amount;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty || slot.Item.ItemId != itemId)
                {
                    continue;
                }

                int removed = slot.Remove(remaining);
                remaining -= removed;
                if (remaining <= 0)
                {
                    InventoryChanged?.Invoke();
                    return true;
                }
            }

            InventoryChanged?.Invoke();
            return false;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.Item.ItemId == itemId)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        public bool TryUseSlot(int slotIndex, PlayerStats playerStats, AudioSource audioSource = null)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return false;
            }

            InventorySlot slot = slots[slotIndex];
            if (slot.IsEmpty || !slot.Item.Consumable || playerStats == null)
            {
                return false;
            }

            ItemDefinition item = slot.Item;
            playerStats.RestoreFromConsumable(item.HealthRestore, item.StaminaRestore, item.HungerRestore, item.ThirstRestore);

            if (audioSource != null && item.UseSfx != null)
            {
                audioSource.PlayOneShot(item.UseSfx);
            }

            slot.Remove(1);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool IsHotbarSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < HotbarSlotCount;
        }

        public List<InventorySlotState> CaptureState()
        {
            List<InventorySlotState> states = new List<InventorySlotState>();
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                states.Add(new InventorySlotState
                {
                    SlotIndex = i,
                    ItemId = slot.Item.ItemId,
                    Quantity = slot.Quantity
                });
            }

            return states;
        }

        public void RestoreState(List<InventorySlotState> states, ItemDatabase itemDatabase)
        {
            EnsureSlotCount();
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
            }

            if (states == null || itemDatabase == null)
            {
                InventoryChanged?.Invoke();
                return;
            }

            for (int i = 0; i < states.Count; i++)
            {
                InventorySlotState state = states[i];
                if (state.SlotIndex < 0 || state.SlotIndex >= slots.Count)
                {
                    continue;
                }

                ItemDefinition item = itemDatabase.GetById(state.ItemId);
                if (item == null)
                {
                    continue;
                }

                InventorySlot slot = slots[state.SlotIndex];
                slot.Item = item;
                slot.Quantity = Mathf.Clamp(state.Quantity, 1, item.MaxStack);
            }

            InventoryChanged?.Invoke();
        }

        public void ForceNotifyChanged()
        {
            InventoryChanged?.Invoke();
        }

        private void EnsureSlotCount()
        {
            slotCount = Mathf.Max(1, slotCount);
            hotbarSlotCount = Mathf.Clamp(hotbarSlotCount, 1, slotCount);
            if (slots == null)
            {
                slots = new List<InventorySlot>();
            }

            while (slots.Count < slotCount)
            {
                slots.Add(new InventorySlot());
            }

            if (slots.Count > slotCount)
            {
                slots.RemoveRange(slotCount, slots.Count - slotCount);
            }
        }
    }
}
