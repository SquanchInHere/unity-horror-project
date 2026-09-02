using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private HotbarController hotbar;

    [Header("Automatic slot creation")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    private InventorySlotUI[] slotViews;

    private void Start()
    {
        if (inventory == null || hotbar == null)
        {
            Debug.LogError(
                "HotbarUI: No Inventory or Hotbar assigned.",
                this
            );
            enabled = false;
            return;
        }

        if (inventoryUI == null)
            inventoryUI = inventory.GetComponent<InventoryUI>();

        CreateSlots();

        inventory.Changed += Refresh;
        hotbar.AssignmentsChanged += Refresh;
        hotbar.SelectionChanged += OnSelectionChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;

        if (hotbar != null)
        {
            hotbar.AssignmentsChanged -= Refresh;
            hotbar.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void CreateSlots()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogError(
                "HotbarUI: No Slots Container or Slot Prefab assigned.",
                this
            );

            slotViews = new InventorySlotUI[0];
            return;
        }

        slotViews = new InventorySlotUI[PlayerInventory.HotbarSlotCount];

        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            int capturedIndex = i;
            InventorySlotUI slotView = Instantiate(
                slotPrefab,
                slotsContainer
            );

            slotView.gameObject.name = $"HotbarSlot_{i + 1}";
            slotView.Initialize(i);
            slotView.SetSlotNumber(i + 1);
            slotView.Button.onClick.AddListener(
                () => hotbar.SelectSlot(capturedIndex)
            );
            slotView.DropReceived +=
                targetIndex => AssignDraggedItem(targetIndex);

            slotViews[i] = slotView;
        }
    }

    private void AssignDraggedItem(int hotbarIndex)
    {
        if (inventoryUI == null || inventoryUI.DraggedSlotIndex < 0)
            return;

        hotbar.AssignFromInventorySlot(
            hotbarIndex,
            inventoryUI.DraggedSlotIndex
        );
    }

    private void Refresh()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
                continue;

            ItemDefinition item = hotbar.GetAssignedItem(i);
            int amount = inventory.GetTotalAmount(item);
            slotViews[i].Render(item, amount);
        }

        OnSelectionChanged(hotbar.SelectedIndex);
    }

    private void OnSelectionChanged(int selectedIndex)
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].SetSelected(i == selectedIndex);
        }
    }
}
