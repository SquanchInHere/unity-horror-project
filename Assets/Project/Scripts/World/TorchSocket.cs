using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TorchSocket : InteractableBase
{
    [Header("Required Item")]
    [SerializeField] private ItemDefinition torchItem;

    [Header("Prepared Lit Visual")]
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
        {
            return allowTakeBack ? "Take torch" : string.Empty;
        }

        if (torchItem == null)
            return "Torch socket is not configured";

        return interactor != null &&
               interactor.Inventory != null &&
               interactor.Inventory.HasItem(torchItem)
            ? "Insert torch"
            : "Torch required";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (interactor == null || torchItem == null)
            return;

        PlayerInventory inventory = interactor.Inventory;

        if (inventory == null)
            return;

        if (!IsOccupied)
        {
            if (!inventory.RemoveItem(torchItem, 1))
                return;

            SetOccupied(true, true);
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
        ApplyVisualState(occupied);

        if (invokeEvents)
        {
            if (occupied)
                onTorchInserted?.Invoke();
            else
                onTorchRemoved?.Invoke();
        }

        StateChanged?.Invoke(this);
    }

    private void ApplyVisualState(bool visible)
    {
        if (insertedTorchVisual == null)
        {
            Debug.LogError(
                "TorchSocket: Inserted Torch Visual is not assigned.",
                this
            );
            return;
        }

        if (insertedTorchVisual == gameObject)
        {
            Debug.LogError(
                "TorchSocket: Inserted Torch Visual cannot reference the socket GameObject itself.",
                this
            );
            return;
        }

        insertedTorchVisual.SetActive(visible);

        if (!visible)
            return;

        ParticleSystem[] particleSystems =
            insertedTorchVisual.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        Light[] lights = insertedTorchVisual.GetComponentsInChildren<Light>(true);

        foreach (Light torchLight in lights)
            torchLight.enabled = true;
    }
}
