using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TorchSocket : InteractableBase
{
    [Header("Required item")]
    [SerializeField] private ItemDefinition torchItem;

    [Header("Prepared visual")]
    [SerializeField] private GameObject insertedTorchVisual;
    [SerializeField] private bool startsOccupied;
    [SerializeField] private bool allowTakeBack;

    [Header("Events")]
    [SerializeField] private UnityEvent onTorchInserted;
    [SerializeField] private UnityEvent onTorchRemoved;

    public bool IsOccupied { get; private set; }
    public event Action<TorchSocket> StateChanged;

    private void Awake()
    {
        SetOccupied(startsOccupied, false);
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (IsOccupied)
            return allowTakeBack ? "Забрать факел" : "Факел установлен";

        if (torchItem == null)
            return "Факельница не настроена";

        return interactor.Inventory != null &&
               interactor.Inventory.HasItem(torchItem)
            ? "Вставить факел"
            : "Нужен факел";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        PlayerInventory inventory = interactor.Inventory;

        if (inventory == null || torchItem == null)
            return;

        if (!IsOccupied)
        {
            if (!inventory.RemoveItem(torchItem, 1))
                return;

            SetOccupied(true, true);
            MonsterNoise.Emit(transform.position, 5.0f);
            return;
        }

        if (!allowTakeBack)
            return;

        bool added = inventory.TryAdd(torchItem, 1, out int notAdded);

        if (added && notAdded == 0)
            SetOccupied(false, true);
    }

    public void RestoreState(bool occupied)
    {
        SetOccupied(occupied, false);
    }

    private void SetOccupied(bool occupied, bool invokeEvents)
    {
        IsOccupied = occupied;

        if (insertedTorchVisual != null)
            insertedTorchVisual.SetActive(occupied);

        if (invokeEvents)
        {
            if (occupied)
                onTorchInserted?.Invoke();
            else
                onTorchRemoved?.Invoke();
        }

        StateChanged?.Invoke(this);
    }
}
