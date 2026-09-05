using System;
using UnityEngine;

[Serializable]
public class ChestLootEntry
{
    public ItemDefinition item;

    [Min(1)]
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public void Set(ItemDefinition newItem, int newAmount)
    {
        item = newItem;
        amount = newItem == null
            ? 0
            : Mathf.Clamp(newAmount, 0, newItem.MaxStack);

        if (amount <= 0 && newItem == null)
            Clear();
    }

    public int AddAmount(int value)
    {
        if (item == null || value <= 0)
            return value;

        int freeSpace = Mathf.Max(0, item.MaxStack - amount);
        int added = Mathf.Min(freeSpace, value);
        amount += added;
        return value - added;
    }

    public void Normalize()
    {
        Set(item, amount);
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}
