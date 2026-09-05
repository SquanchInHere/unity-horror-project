using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OpenObject))]
public class LootChest : MonoBehaviour
{
    [Header("Storage")]
    [Min(1)]
    [SerializeField] private int capacity = 8;
    [SerializeField] private List<ChestLootEntry> contents = new();

    [Header("Interface")]
    [SerializeField] private ChestInventoryUI chestInventoryUI;

    private OpenObject openingObject;

    public int Capacity => capacity;
    public IReadOnlyList<ChestLootEntry> Contents => contents;

    public event Action ContentsChanged;

    private void Awake()
    {
        openingObject = GetComponent<OpenObject>();
        NormalizeContents();

        if (chestInventoryUI == null)
            chestInventoryUI = FindFirstObjectByType<ChestInventoryUI>();
    }

    private void OnEnable()
    {
        if (openingObject == null)
            openingObject = GetComponent<OpenObject>();

        if (openingObject != null)
            openingObject.Opened += OpenStorage;
    }

    private void OnDisable()
    {
        if (openingObject != null)
            openingObject.Opened -= OpenStorage;
    }

    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);

        if (!Application.isPlaying)
            NormalizeContents();
    }

    public ChestLootEntry GetSlot(int index)
    {
        if (index < 0 || index >= contents.Count)
            return null;

        return contents[index];
    }

    public bool TryTransferBetweenSlots(
        int sourceIndex,
        int targetIndex,
        int amount,
        bool allowSwap)
    {
        ChestLootEntry source = GetSlot(sourceIndex);
        ChestLootEntry target = GetSlot(targetIndex);

        if (source == null ||
            target == null ||
            sourceIndex == targetIndex ||
            !TryCalculateTransfer(
                source.item,
                source.amount,
                target.item,
                target.amount,
                amount,
                allowSwap,
                out ItemDefinition newSourceItem,
                out int newSourceAmount,
                out ItemDefinition newTargetItem,
                out int newTargetAmount))
        {
            return false;
        }

        source.Set(newSourceItem, newSourceAmount);
        target.Set(newTargetItem, newTargetAmount);
        ContentsChanged?.Invoke();
        return true;
    }

    public bool TryStoreFromPlayer(
        PlayerInventory playerInventory,
        int playerSlotIndex,
        int chestSlotIndex,
        int amount,
        bool allowSwap)
    {
        InventorySlotData playerSlot =
            playerInventory?.GetSlot(playerSlotIndex);
        ChestLootEntry chestSlot = GetSlot(chestSlotIndex);

        if (playerSlot == null || chestSlot == null ||
            !TryCalculateTransfer(
                playerSlot.Item,
                playerSlot.Amount,
                chestSlot.item,
                chestSlot.amount,
                amount,
                allowSwap,
                out ItemDefinition newPlayerItem,
                out int newPlayerAmount,
                out ItemDefinition newChestItem,
                out int newChestAmount))
        {
            return false;
        }

        chestSlot.Set(newChestItem, newChestAmount);
        playerInventory.TrySetSlot(
            playerSlotIndex,
            newPlayerItem,
            newPlayerAmount
        );

        ContentsChanged?.Invoke();
        return true;
    }

    public bool TryTakeToPlayer(
        PlayerInventory playerInventory,
        int chestSlotIndex,
        int playerSlotIndex,
        int amount,
        bool allowSwap)
    {
        ChestLootEntry chestSlot = GetSlot(chestSlotIndex);
        InventorySlotData playerSlot =
            playerInventory?.GetSlot(playerSlotIndex);

        if (chestSlot == null || playerSlot == null ||
            !TryCalculateTransfer(
                chestSlot.item,
                chestSlot.amount,
                playerSlot.Item,
                playerSlot.Amount,
                amount,
                allowSwap,
                out ItemDefinition newChestItem,
                out int newChestAmount,
                out ItemDefinition newPlayerItem,
                out int newPlayerAmount))
        {
            return false;
        }

        chestSlot.Set(newChestItem, newChestAmount);
        playerInventory.TrySetSlot(
            playerSlotIndex,
            newPlayerItem,
            newPlayerAmount
        );

        ContentsChanged?.Invoke();
        return true;
    }

    private static bool TryCalculateTransfer(
        ItemDefinition sourceItem,
        int sourceAmount,
        ItemDefinition targetItem,
        int targetAmount,
        int requestedAmount,
        bool allowSwap,
        out ItemDefinition newSourceItem,
        out int newSourceAmount,
        out ItemDefinition newTargetItem,
        out int newTargetAmount)
    {
        newSourceItem = sourceItem;
        newSourceAmount = sourceAmount;
        newTargetItem = targetItem;
        newTargetAmount = targetAmount;

        if (sourceItem == null ||
            sourceAmount <= 0 ||
            requestedAmount <= 0)
        {
            return false;
        }

        int amountToMove = Mathf.Min(requestedAmount, sourceAmount);
        bool targetIsEmpty = targetItem == null || targetAmount <= 0;

        if (targetIsEmpty)
        {
            newSourceAmount = sourceAmount - amountToMove;
            newSourceItem = newSourceAmount > 0 ? sourceItem : null;
            newTargetItem = sourceItem;
            newTargetAmount = amountToMove;
            return true;
        }

        if (targetItem == sourceItem)
        {
            int freeSpace = Mathf.Max(
                0,
                sourceItem.MaxStack - targetAmount
            );
            int movedAmount = Mathf.Min(freeSpace, amountToMove);

            if (movedAmount <= 0)
                return false;

            newSourceAmount = sourceAmount - movedAmount;
            newSourceItem = newSourceAmount > 0 ? sourceItem : null;
            newTargetAmount = targetAmount + movedAmount;
            return true;
        }

        if (!allowSwap || amountToMove < sourceAmount)
            return false;

        newSourceItem = targetItem;
        newSourceAmount = targetAmount;
        newTargetItem = sourceItem;
        newTargetAmount = sourceAmount;
        return true;
    }

    public bool CloseStorage()
    {
        return openingObject != null && openingObject.Close();
    }

    private void OpenStorage()
    {
        if (chestInventoryUI == null)
            chestInventoryUI = FindFirstObjectByType<ChestInventoryUI>();

        if (chestInventoryUI == null)
        {
            Debug.LogError(
                "LootChest: ChestInventoryUI was not found in the scene.",
                this
            );
            return;
        }

        chestInventoryUI.Open(this);
    }

    private void NormalizeContents()
    {
        contents ??= new List<ChestLootEntry>();

        while (contents.Count < capacity)
            contents.Add(new ChestLootEntry());

        if (contents.Count > capacity)
            contents.RemoveRange(capacity, contents.Count - capacity);

        for (int i = 0; i < contents.Count; i++)
        {
            contents[i] ??= new ChestLootEntry();
            contents[i].Normalize();
        }
    }
}
