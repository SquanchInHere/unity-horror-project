using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 8;
    public const int HotbarSlotCount = 3;

    [SerializeField] private List<StartingInventoryEntry> startingItems = new();

    private readonly List<InventorySlotData> slots = new(SlotCount);

    public event Action Changed;

    public IReadOnlyList<InventorySlotData> Slots => slots;

    private void Awake()
    {
        slots.Clear();

        for (int i = 0; i < SlotCount; i++)
            slots.Add(new InventorySlotData());

        AddStartingItems();
    }

    private void Start()
    {
        if (startingItems.Count == 0)
        {
            Debug.LogWarning(
                "PlayerInventory: Starting Items is empty. Your inventory will start the game with no items.",
                this
            );
        }
    }

    public InventorySlotData GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    public bool TrySetSlot(
        int index,
        ItemDefinition item,
        int amount)
    {
        InventorySlotData slot = GetSlot(index);

        if (slot == null)
            return false;

        slot.Set(item, amount);
        Changed?.Invoke();
        return true;
    }

    public bool HasItem(ItemDefinition item, int requiredAmount = 1)
    {
        if (item == null || requiredAmount <= 0)
            return false;

        int total = 0;

        foreach (InventorySlotData slot in slots)
        {
            if (!slot.IsEmpty && slot.Item == item)
                total += slot.Amount;
        }

        return total >= requiredAmount;
    }

    public int GetTotalAmount(ItemDefinition item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (InventorySlotData slot in slots)
        {
            if (!slot.IsEmpty && slot.Item == item)
                total += slot.Amount;
        }

        return total;
    }

    public bool TryAdd(ItemDefinition item, int amount, out int notAdded)
    {
        notAdded = Mathf.Max(0, amount);

        if (item == null || amount <= 0)
            return false;

        for (int i = 0; i < slots.Count && notAdded > 0; i++)
        {
            InventorySlotData slot = slots[i];

            if (!slot.IsEmpty && slot.Item == item)
                notAdded = slot.AddAmount(notAdded);
        }

        PutIntoEmptySlots(item, ref notAdded, 0, SlotCount);

        if (notAdded < amount)
            Changed?.Invoke();

        return notAdded == 0;
    }

    public bool RemoveItem(ItemDefinition item, int amount)
    {
        if (!HasItem(item, amount))
            return false;

        int remaining = amount;

        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlotData slot = slots[i];

            if (slot.IsEmpty || slot.Item != item)
                continue;

            int removed = Mathf.Min(slot.Amount, remaining);
            int newAmount = slot.Amount - removed;
            remaining -= removed;

            if (newAmount <= 0)
                slot.Clear();
            else
                slot.Set(item, newAmount);
        }

        Changed?.Invoke();
        return true;
    }

    public int RemoveFromSlot(int slotIndex, int requestedAmount)
    {
        InventorySlotData slot = GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || requestedAmount <= 0)
            return 0;

        int removed = Mathf.Min(slot.Amount, requestedAmount);
        int remaining = slot.Amount - removed;

        if (remaining <= 0)
            slot.Clear();
        else
            slot.Set(slot.Item, remaining);

        Changed?.Invoke();
        return removed;
    }

    public void SwapSlots(int firstIndex, int secondIndex)
    {
        if (firstIndex == secondIndex ||
            firstIndex < 0 || firstIndex >= slots.Count ||
            secondIndex < 0 || secondIndex >= slots.Count)
        {
            return;
        }

        InventorySlotData first = slots[firstIndex];
        InventorySlotData second = slots[secondIndex];

        ItemDefinition firstItem = first.Item;
        int firstAmount = first.Amount;

        first.Set(second.Item, second.Amount);
        second.Set(firstItem, firstAmount);

        Changed?.Invoke();
    }

    public bool TryTransferBetweenSlots(
        int sourceIndex,
        int targetIndex,
        int amount,
        bool allowSwap)
    {
        InventorySlotData source = GetSlot(sourceIndex);
        InventorySlotData target = GetSlot(targetIndex);

        if (source == null ||
            target == null ||
            sourceIndex == targetIndex ||
            !TryCalculateTransfer(
                source.Item,
                source.Amount,
                target.Item,
                target.Amount,
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
        Changed?.Invoke();
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

    public void RestoreFromSave(
        IReadOnlyList<InventorySlotSaveData> savedSlots,
        ItemDatabase itemDatabase)
    {
        foreach (InventorySlotData slot in slots)
            slot.Clear();

        if (savedSlots != null && itemDatabase != null)
        {
            int count = Mathf.Min(savedSlots.Count, slots.Count);

            for (int i = 0; i < count; i++)
            {
                InventorySlotSaveData savedSlot = savedSlots[i];

                if (savedSlot == null ||
                    string.IsNullOrWhiteSpace(savedSlot.itemId) ||
                    savedSlot.amount <= 0)
                {
                    continue;
                }

                ItemDefinition item = itemDatabase.FindById(savedSlot.itemId);

                if (item == null)
                {
                    Debug.LogWarning(
                        $"PlayerInventory: Item Id '{savedSlot.itemId}' is missing from ItemDatabase.",
                        this
                    );
                    continue;
                }

                slots[i].Set(item, savedSlot.amount);
            }
        }

        Changed?.Invoke();
    }

    private void PutIntoEmptySlots(
        ItemDefinition item,
        ref int remaining,
        int startIndex,
        int endIndex)
    {
        for (int i = startIndex; i < endIndex && remaining > 0; i++)
        {
            if (!slots[i].IsEmpty)
                continue;

            int amountForSlot = Mathf.Min(item.MaxStack, remaining);
            slots[i].Set(item, amountForSlot);
            remaining -= amountForSlot;
        }
    }

    private void AddStartingItems()
    {
        foreach (StartingInventoryEntry entry in startingItems)
        {
            if (entry == null || entry.item == null || entry.amount <= 0)
                continue;

            int slotIndex = Mathf.Clamp(entry.slotNumber - 1, 0, SlotCount - 1);
            InventorySlotData slot = slots[slotIndex];

            if (slot.IsEmpty)
            {
                int amountForSlot = Mathf.Min(entry.amount, entry.item.MaxStack);
                slot.Set(entry.item, amountForSlot);

                int remaining = entry.amount - amountForSlot;

                if (remaining > 0)
                    TryAdd(entry.item, remaining, out _);
            }
            else
            {
                TryAdd(entry.item, entry.amount, out _);
            }
        }

        Changed?.Invoke();
    }
}
