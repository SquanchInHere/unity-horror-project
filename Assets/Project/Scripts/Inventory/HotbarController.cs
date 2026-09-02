using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerInputReader))]
public class HotbarController : MonoBehaviour
{
    [Header("Held item")]
    [SerializeField] private Transform heldItemRoot;
    [SerializeField] private int startingSlot;

    [Header("Initial shortcuts")]
    [SerializeField]
    private ItemDefinition[] startingAssignments =
        new ItemDefinition[PlayerInventory.HotbarSlotCount];

    private PlayerInventory inventory;
    private PlayerInputReader input;

    private readonly ItemDefinition[] assignments =
        new ItemDefinition[PlayerInventory.HotbarSlotCount];

    private readonly GameObject[] spawnedObjects =
        new GameObject[PlayerInventory.HotbarSlotCount];

    private readonly ItemDefinition[] spawnedDefinitions =
        new ItemDefinition[PlayerInventory.HotbarSlotCount];

    public int SelectedIndex { get; private set; }

    public event Action<int> SelectionChanged;
    public event Action AssignmentsChanged;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        input = GetComponent<PlayerInputReader>();
        SelectedIndex = Mathf.Clamp(
            startingSlot,
            0,
            PlayerInventory.HotbarSlotCount - 1
        );

        for (int i = 0; i < assignments.Length; i++)
        {
            assignments[i] = startingAssignments != null &&
                             i < startingAssignments.Length
                ? startingAssignments[i]
                : null;
        }
    }

    private void Start()
    {
        if (heldItemRoot == null)
        {
            Debug.LogError(
                "HotbarController: No Held Item Root assigned.",
                this
            );
            enabled = false;
            return;
        }

        inventory.Changed += OnInventoryChanged;
        ValidateAssignments();
        RefreshHeldObjects();
        ApplySelection();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= OnInventoryChanged;
    }

    private void Update()
    {
        int requestedIndex = input.ConsumeHotbarIndex();

        if (requestedIndex >= 0)
            SelectSlot(requestedIndex);
    }

    public ItemDefinition GetAssignedItem(int hotbarIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= assignments.Length)
            return null;

        return assignments[hotbarIndex];
    }

    public bool AssignFromInventorySlot(
        int hotbarIndex,
        int inventorySlotIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= assignments.Length)
            return false;

        InventorySlotData inventorySlot =
            inventory.GetSlot(inventorySlotIndex);

        if (inventorySlot == null || inventorySlot.IsEmpty)
            return false;

        ItemDefinition item = inventorySlot.Item;

        for (int i = 0; i < assignments.Length; i++)
        {
            if (i != hotbarIndex && assignments[i] == item)
                assignments[i] = null;
        }

        assignments[hotbarIndex] = item;
        RefreshHeldObjects();
        AssignmentsChanged?.Invoke();
        return true;
    }

    public void ClearAssignment(int hotbarIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= assignments.Length)
            return;

        assignments[hotbarIndex] = null;
        RefreshHeldObjects();
        AssignmentsChanged?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= PlayerInventory.HotbarSlotCount)
            return;

        SelectedIndex = index;
        ApplySelection();
        SelectionChanged?.Invoke(SelectedIndex);
    }

    public List<string> GetAssignmentItemIds()
    {
        List<string> ids = new(PlayerInventory.HotbarSlotCount);

        foreach (ItemDefinition item in assignments)
            ids.Add(item != null ? item.ItemId : string.Empty);

        return ids;
    }

    public void RestoreAssignments(
        IReadOnlyList<string> itemIds,
        ItemDatabase itemDatabase)
    {
        for (int i = 0; i < assignments.Length; i++)
        {
            assignments[i] = null;

            if (itemIds == null ||
                i >= itemIds.Count ||
                string.IsNullOrWhiteSpace(itemIds[i]) ||
                itemDatabase == null)
            {
                continue;
            }

            ItemDefinition item = itemDatabase.FindById(itemIds[i]);

            if (item != null && inventory.HasItem(item))
                assignments[i] = item;
        }

        RefreshHeldObjects();
        AssignmentsChanged?.Invoke();
    }

    private void OnInventoryChanged()
    {
        ValidateAssignments();
        RefreshHeldObjects();
        AssignmentsChanged?.Invoke();
    }

    private bool ValidateAssignments()
    {
        bool changed = false;

        for (int i = 0; i < assignments.Length; i++)
        {
            ItemDefinition item = assignments[i];

            if (item != null && !inventory.HasItem(item))
            {
                assignments[i] = null;
                changed = true;
            }
        }

        return changed;
    }

    private void RefreshHeldObjects()
    {
        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            ItemDefinition definition = assignments[i];

            if (definition != null && !inventory.HasItem(definition))
                definition = null;

            if (spawnedDefinitions[i] == definition)
                continue;

            if (spawnedObjects[i] != null)
                DestroyHeldObject(spawnedObjects[i]);

            spawnedObjects[i] = null;
            spawnedDefinitions[i] = definition;

            if (definition == null || definition.HeldPrefab == null)
                continue;

            GameObject heldSlot = new GameObject(
                $"HeldSlot_{i + 1}_{definition.DisplayName}"
            );
            heldSlot.SetActive(false);
            heldSlot.transform.SetParent(heldItemRoot, false);
            heldSlot.transform.localPosition = Vector3.zero;
            heldSlot.transform.localRotation = Quaternion.identity;
            heldSlot.transform.localScale = Vector3.one;

            GameObject heldModel = Instantiate(
                definition.HeldPrefab,
                heldSlot.transform,
                false
            );
            heldModel.name = $"Model_{definition.DisplayName}";
            heldModel.transform.localPosition = Vector3.zero;
            heldModel.transform.localRotation = Quaternion.identity;

            HeldItemPlacement placement =
                heldModel.GetComponentInChildren<HeldItemPlacement>(true);

            if (placement != null)
                placement.ApplyTo(heldSlot.transform);

            PrepareAsHeldObject(heldModel);
            spawnedObjects[i] = heldSlot;
        }

        ApplySelection();
    }

    private void ApplySelection()
    {
        for (int i = 0; i < spawnedObjects.Length; i++)
        {
            if (spawnedObjects[i] != null)
                spawnedObjects[i].SetActive(i == SelectedIndex);
        }
    }

    private static void PrepareAsHeldObject(GameObject heldObject)
    {
        foreach (Rigidbody body in
                 heldObject.GetComponentsInChildren<Rigidbody>(true))
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        foreach (Collider itemCollider in
                 heldObject.GetComponentsInChildren<Collider>(true))
        {
            itemCollider.enabled = false;
        }

        foreach (InteractableBase interactable in
                 heldObject.GetComponentsInChildren<InteractableBase>(true))
        {
            interactable.enabled = false;
        }

        foreach (Animator animator in
                 heldObject.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
        }

        foreach (ParticleSystem particles in
                 heldObject.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    private static void DestroyHeldObject(GameObject heldObject)
    {
        heldObject.SetActive(false);

        foreach (ParticleSystem particles in
                 heldObject.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        foreach (AudioSource audioSource in
                 heldObject.GetComponentsInChildren<AudioSource>(true))
        {
            audioSource.Stop();
        }

        Destroy(heldObject);
    }
}
