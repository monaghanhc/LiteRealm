using System;

namespace LiteRealm.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemDefinition Item;
        public int Quantity;

        public bool IsEmpty => Item == null || Quantity <= 0;

        public void Clear()
        {
            Item = null;
            Quantity = 0;
        }

        public int Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            if (IsEmpty)
            {
                Item = item;
            }

            if (Item != item)
            {
                return amount;
            }

            int room = Item.MaxStack - Quantity;
            int toAdd = room > 0 ? Math.Min(room, amount) : 0;
            Quantity += toAdd;
            return amount - toAdd;
        }

        public int Remove(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return 0;
            }

            int removed = Math.Min(amount, Quantity);
            Quantity -= removed;
            if (Quantity <= 0)
            {
                Clear();
            }

            return removed;
        }
    }

    [Serializable]
    public struct InventorySlotState
    {
        public int SlotIndex;
        public string ItemId;
        public int Quantity;
    }
}
