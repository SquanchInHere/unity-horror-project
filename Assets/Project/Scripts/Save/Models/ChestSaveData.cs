using System;
using System.Collections.Generic;

[Serializable]
public class ChestSaveData
{
    public string id;
    public bool isOpen;
    public bool isLocked;
    public List<InventorySlotSaveData> contents = new();
}
