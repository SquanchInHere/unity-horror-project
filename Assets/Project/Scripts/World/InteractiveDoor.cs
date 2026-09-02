using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractiveDoor : InteractableBase
{
    [Header("Door")]
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 90.0f;
    [SerializeField] private float rotationSpeed = 120.0f;
    [SerializeField] private bool canClose = true;

    [Header("Lock")]
    [SerializeField] private bool startsLocked;
    [SerializeField] private ItemDefinition keyItem;
    [SerializeField] private ItemDefinition lockpickItem;
    [SerializeField] private bool consumeUnlockItem;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip lockedClip;

    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private bool isMoving;

    public bool IsOpen { get; private set; }
    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (hinge == null)
            hinge = transform;

        closedRotation = hinge.localRotation;
        openedRotation = closedRotation * Quaternion.Euler(0.0f, openAngle, 0.0f);
        IsLocked = startsLocked;
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (isMoving)
            return "Дверь движется";

        if (IsLocked)
        {
            PlayerInventory inventory = interactor.Inventory;
            bool canUnlock =
                inventory != null &&
                ((keyItem != null && inventory.HasItem(keyItem)) ||
                 (lockpickItem != null && inventory.HasItem(lockpickItem)));

            return canUnlock
                ? "Открыть замок"
                : "Заперто: нужен ключ или отмычка";
        }

        if (IsOpen)
            return canClose ? "Закрыть дверь" : "Дверь открыта";

        return "Открыть дверь";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (isMoving)
            return;

        if (IsLocked && !TryUnlockWithInventory(interactor.Inventory))
        {
            PlayClip(lockedClip);
            return;
        }

        if (IsOpen && !canClose)
            return;

        SetOpen(!IsOpen, true);
    }

    public void Unlock()
    {
        IsLocked = false;
    }

    public void UnlockAndOpen()
    {
        IsLocked = false;

        if (!IsOpen && !isMoving)
            SetOpen(true, true);
    }

    public void RestoreState(bool open, bool locked)
    {
        StopAllCoroutines();
        isMoving = false;
        IsLocked = locked;
        IsOpen = open;
        hinge.localRotation = open ? openedRotation : closedRotation;
    }

    private bool TryUnlockWithInventory(PlayerInventory inventory)
    {
        if (inventory == null)
            return false;

        ItemDefinition usedItem = null;

        if (keyItem != null && inventory.HasItem(keyItem))
            usedItem = keyItem;
        else if (lockpickItem != null && inventory.HasItem(lockpickItem))
            usedItem = lockpickItem;

        if (usedItem == null)
            return false;

        if (consumeUnlockItem)
            inventory.RemoveItem(usedItem, 1);

        IsLocked = false;
        return true;
    }

    private void SetOpen(bool open, bool animated)
    {
        IsOpen = open;
        Quaternion target = open ? openedRotation : closedRotation;

        PlayClip(open ? openClip : closeClip);
        MonsterNoise.Emit(transform.position, 9.0f);

        if (animated)
            StartCoroutine(RotateDoor(target));
        else
            hinge.localRotation = target;
    }

    private IEnumerator RotateDoor(Quaternion target)
    {
        isMoving = true;

        while (Quaternion.Angle(hinge.localRotation, target) > 0.1f)
        {
            hinge.localRotation = Quaternion.RotateTowards(
                hinge.localRotation,
                target,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        hinge.localRotation = target;
        isMoving = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
