using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChestLootEntry
{
    public ItemDefinition item;

    [Min(0)]
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}

[RequireComponent(typeof(Collider))]
public class LootChest : InteractableBase
{
    [Header("Lid")]
    [SerializeField] private Transform lidHinge;
    [SerializeField] private float openAngle = -105.0f;
    [SerializeField] private float rotationSpeed = 120.0f;

    [Header("Lock")]
    [SerializeField] private bool startsLocked;
    [SerializeField] private ItemDefinition keyItem;
    [SerializeField] private ItemDefinition lockpickItem;
    [SerializeField] private bool consumeUnlockItem;

    [Header("Storage")]
    [Min(1)]
    [SerializeField] private int capacity = 8;
    [SerializeField] private List<ChestLootEntry> contents = new();
    [SerializeField] private ChestInventoryUI chestInventoryUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip lockedClip;

    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private bool isMoving;

    public bool IsOpen { get; private set; }
    public bool IsLocked { get; private set; }
    public int Capacity => capacity;
    public IReadOnlyList<ChestLootEntry> Contents => contents;
    public event Action ContentsChanged;

    private void Awake()
    {
        if (lidHinge == null)
            lidHinge = transform;

        closedRotation = lidHinge.localRotation;
        openedRotation = closedRotation * Quaternion.Euler(openAngle, 0.0f, 0.0f);
        IsLocked = startsLocked;
        NormalizeContents();
    }

    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);

        if (!Application.isPlaying)
            NormalizeContents();
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (isMoving)
            return "Сундук открывается";

        if (IsLocked)
        {
            return CanUnlock(interactor.Inventory)
                ? "Открыть замок сундука"
                : "Сундук заперт";
        }

        return IsOpen ? "Открыть хранилище" : "Открыть сундук";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        PlayerInventory inventory = interactor.Inventory;

        if (inventory == null || isMoving)
            return;

        if (IsLocked && !TryUnlock(inventory))
        {
            PlayClip(lockedClip);
            return;
        }

        if (!IsOpen)
        {
            IsOpen = true;
            PlayClip(openClip);
            MonsterNoise.Emit(transform.position, 8.0f);
            StartCoroutine(RotateLid(openedRotation));
        }

        if (chestInventoryUI == null)
        {
            Debug.LogError(
                "LootChest: назначь общий ChestInventoryUI из GameCanvas.",
                this
            );
            return;
        }

        chestInventoryUI.Open(this);
    }

    public ChestLootEntry GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= contents.Count)
            return null;

        return contents[slotIndex];
    }

    public bool TakeSlot(int slotIndex, PlayerInventory playerInventory)
    {
        ChestLootEntry slot = GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || playerInventory == null)
            return false;

        ItemDefinition item = slot.item;
        int oldAmount = slot.amount;
        playerInventory.TryAdd(item, oldAmount, out int notAdded);

        if (notAdded == oldAmount)
            return false;

        if (notAdded <= 0)
            slot.Clear();
        else
            slot.amount = notAdded;

        ContentsChanged?.Invoke();
        return true;
    }

    public bool StorePlayerSlot(int playerSlotIndex, PlayerInventory playerInventory)
    {
        if (playerInventory == null)
            return false;

        InventorySlotData playerSlot = playerInventory.GetSlot(playerSlotIndex);

        if (playerSlot == null || playerSlot.IsEmpty)
            return false;

        ItemDefinition item = playerSlot.Item;
        int amount = playerSlot.Amount;
        int notStored = AddToChest(item, amount);
        int stored = amount - notStored;

        if (stored <= 0)
            return false;

        playerInventory.RemoveFromSlot(playerSlotIndex, stored);
        ContentsChanged?.Invoke();
        return true;
    }

    public void RestoreState(
        bool open,
        bool locked,
        IReadOnlyList<InventorySlotSaveData> savedContents,
        ItemDatabase itemDatabase)
    {
        StopAllCoroutines();
        isMoving = false;
        IsOpen = open;
        IsLocked = locked;
        lidHinge.localRotation = open ? openedRotation : closedRotation;

        NormalizeContents();

        foreach (ChestLootEntry entry in contents)
            entry.Clear();

        if (savedContents != null && itemDatabase != null)
        {
            int count = Mathf.Min(savedContents.Count, contents.Count);

            for (int i = 0; i < count; i++)
            {
                InventorySlotSaveData savedItem = savedContents[i];

                if (savedItem == null ||
                    string.IsNullOrWhiteSpace(savedItem.itemId) ||
                    savedItem.amount <= 0)
                {
                    continue;
                }

                ItemDefinition item = itemDatabase.FindById(savedItem.itemId);

                if (item == null)
                {
                    Debug.LogWarning(
                        $"LootChest: Item Id '{savedItem.itemId}' отсутствует в ItemDatabase.",
                        this
                    );
                    continue;
                }

                contents[i].item = item;
                contents[i].amount = Mathf.Min(savedItem.amount, item.MaxStack);
            }
        }

        ContentsChanged?.Invoke();
    }

    private int AddToChest(ItemDefinition item, int amount)
    {
        int remaining = Mathf.Max(0, amount);

        if (item == null || remaining <= 0)
            return remaining;

        foreach (ChestLootEntry slot in contents)
        {
            if (slot.IsEmpty || slot.item != item)
                continue;

            int freeSpace = item.MaxStack - slot.amount;
            int added = Mathf.Min(freeSpace, remaining);
            slot.amount += added;
            remaining -= added;

            if (remaining <= 0)
                return 0;
        }

        foreach (ChestLootEntry slot in contents)
        {
            if (!slot.IsEmpty)
                continue;

            int added = Mathf.Min(item.MaxStack, remaining);
            slot.item = item;
            slot.amount = added;
            remaining -= added;

            if (remaining <= 0)
                return 0;
        }

        return remaining;
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
            if (contents[i] == null)
                contents[i] = new ChestLootEntry();
        }
    }

    private bool CanUnlock(PlayerInventory inventory)
    {
        return inventory != null &&
               ((keyItem != null && inventory.HasItem(keyItem)) ||
                (lockpickItem != null && inventory.HasItem(lockpickItem)));
    }

    private bool TryUnlock(PlayerInventory inventory)
    {
        if (!CanUnlock(inventory))
            return false;

        ItemDefinition usedItem =
            keyItem != null && inventory.HasItem(keyItem)
                ? keyItem
                : lockpickItem;

        if (consumeUnlockItem)
            inventory.RemoveItem(usedItem, 1);

        IsLocked = false;
        return true;
    }

    private IEnumerator RotateLid(Quaternion target)
    {
        isMoving = true;

        while (Quaternion.Angle(lidHinge.localRotation, target) > 0.1f)
        {
            lidHinge.localRotation = Quaternion.RotateTowards(
                lidHinge.localRotation,
                target,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        lidHinge.localRotation = target;
        isMoving = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
