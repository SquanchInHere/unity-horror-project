using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount;

    public ItemDefinition Item => item;
    public int Amount => amount;
    public bool IsEmpty => item == null || amount <= 0;

    public void Set(ItemDefinition newItem, int newAmount)
    {
        item = newItem;
        amount = newItem == null ? 0 : Mathf.Clamp(newAmount, 0, newItem.MaxStack);

        if (amount <= 0)
            Clear();
    }

    public int AddAmount(int value)
    {
        if (item == null || value <= 0)
            return value;

        int freeSpace = item.MaxStack - amount;
        int added = Mathf.Min(freeSpace, value);
        amount += added;
        return value - added;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}
