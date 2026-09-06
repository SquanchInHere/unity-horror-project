using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractiveDoor : InteractableBase
{
    [Header("Door Movement")]
    [SerializeField] private OpenObject openObject;

    [Header("Lock")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private ItemDefinition keyItem;
    [SerializeField] private ItemDefinition lockpickItem;

    [Header("Interaction")]
    [Tooltip("Disable this for a door controlled only by a torch socket or another puzzle.")]
    [SerializeField] private bool allowPlayerInteraction = true;

    [Header("Prompts")]
    [SerializeField] private string openPrompt = "Open door";
    [SerializeField] private string closePrompt = "Close door";
    [SerializeField] private string unlockWithKeyPrompt = "Unlock with key";
    [SerializeField] private string unlockWithLockpickPrompt = "Unlock with lockpick";
    [SerializeField] private string lockedPrompt = "Locked";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip unlockSound;

    public bool IsOpen => openObject != null && openObject.IsOpen;
    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (openObject == null)
            openObject = GetComponentInParent<OpenObject>();

        if (openObject == null)
        {
            Debug.LogError(
                "InteractiveDoor: Open Object is not assigned and was not found in a parent.",
                this
            );
        }

        IsLocked = startsLocked;
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        if (!base.CanInteract(interactor) ||
            !allowPlayerInteraction ||
            openObject == null ||
            openObject.IsAnimating)
        {
            return false;
        }

        return IsLocked ||
               (IsOpen ? openObject.CanClose : openObject.CanOpen);
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        // PlayerInteractor currently uses an empty prompt to ignore an interaction.
        if (!allowPlayerInteraction || openObject == null)
            return string.Empty;

        if (openObject.IsAnimating)
            return "Wait";

        if (IsLocked)
        {
            ItemDefinition availableItem = FindAvailableUnlockItem(
                interactor != null ? interactor.Inventory : null
            );

            if (availableItem == keyItem)
                return unlockWithKeyPrompt;

            if (availableItem == lockpickItem)
                return unlockWithLockpickPrompt;

            return lockedPrompt;
        }

        if (IsOpen)
            return openObject.CanClose ? closePrompt : string.Empty;

        return openObject.CanOpen ? openPrompt : string.Empty;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        if (IsLocked)
        {
            if (!TryUnlockWithInventory(interactor.Inventory))
            {
                PlaySound(lockedSound);
                return;
            }

            openObject.Open();
            return;
        }

        if (IsOpen)
            openObject.Close();
        else
            openObject.Open();
    }

    public void Unlock()
    {
        if (!IsLocked)
            return;

        IsLocked = false;
        PlaySound(unlockSound);
    }

    public void UnlockAndOpen()
    {
        Unlock();

        if (openObject != null && !openObject.IsOpen)
            openObject.Open();
    }

    public void RestoreState(bool open, bool locked)
    {
        IsLocked = locked;

        if (openObject != null)
            openObject.RestoreState(open);
    }

    private bool TryUnlockWithInventory(PlayerInventory inventory)
    {
        ItemDefinition unlockItem = FindAvailableUnlockItem(inventory);

        if (unlockItem == null)
            return false;

        if (!inventory.RemoveItem(unlockItem, 1))
            return false;

        IsLocked = false;
        PlaySound(unlockSound);
        return true;
    }

    private ItemDefinition FindAvailableUnlockItem(PlayerInventory inventory)
    {
        if (inventory == null)
            return null;

        // A matching key has priority when the player owns both items.
        if (keyItem != null && inventory.HasItem(keyItem))
            return keyItem;

        if (lockpickItem != null && inventory.HasItem(lockpickItem))
            return lockpickItem;

        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
