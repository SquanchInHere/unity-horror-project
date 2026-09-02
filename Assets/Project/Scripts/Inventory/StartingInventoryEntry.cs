using System;
using UnityEngine;

[Serializable]
public class StartingInventoryEntry
{
    public ItemDefinition item;

    [Min(1)]
    public int amount = 1;

    [Range(1, PlayerInventory.SlotCount)]
    public int slotNumber = 1;
}
