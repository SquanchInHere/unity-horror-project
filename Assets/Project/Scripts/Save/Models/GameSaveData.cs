using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public string sceneName;
    public Vector3SaveData playerPosition = new();
    public float playerYaw;
    public float playerHealth;
    public int selectedHotbarIndex;
    public List<string> hotbarItemIds = new();
    public bool monsterReleased;
    public List<InventorySlotSaveData> inventory = new();
    public List<string> collectedObjectIds = new();
    public List<TorchSaveData> torches = new();
    public List<DoorSaveData> doors = new();
    public List<TorchSocketSaveData> torchSockets = new();
    public List<ChestSaveData> chests = new();
}