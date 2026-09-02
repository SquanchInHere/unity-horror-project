using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerInventory))]
public class InventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Inventory Slot")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Drag and drop")]
    [Min(16.0f)]
    [SerializeField] private float dragIconSize = 72.0f;

    private PlayerInputReader input;
    private PlayerInventory inventory;
    private InventorySlotUI[] slotViews;
    private Canvas rootCanvas;
    private RectTransform dragIconRect;
    private Image dragIconImage;
    private int draggedSlotIndex = -1;

    public bool IsOpen { get; private set; }
    public int DraggedSlotIndex => draggedSlotIndex;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        inventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        CreateSlots();
        CreateDragIcon();

        inventory.Changed += Refresh;
        SetOpen(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;
    }

    private void Update()
    {
        if (!input.ConsumeInventory())
            return;

        if (!input.GameplayEnabled && !IsOpen)
            return;

        SetOpen(!IsOpen);
    }

    private void CreateSlots()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogError(
                "InventoryUI: Slots Container or Slot Prefab are not assigned.",
                this
            );

            slotViews = new InventorySlotUI[0];
            return;
        }

        slotViews = new InventorySlotUI[PlayerInventory.SlotCount];

        for (int i = 0; i < PlayerInventory.SlotCount; i++)
        {
            InventorySlotUI slotView = Instantiate(slotPrefab, slotsContainer);

            slotView.gameObject.name = $"Slot_{i + 1}";
            slotView.Initialize(i);
            slotView.SetSlotNumber(i + 1);
            slotView.SetSelected(false);
            slotView.DragStarted += BeginDrag;
            slotView.DragMoved += MoveDragIcon;
            slotView.DragFinished += EndDrag;
            slotView.DropReceived += DropOnSlot;

            slotViews[i] = slotView;
        }
    }

    private void CreateDragIcon()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryUI: No Inventory Panel assigned.", this);
            return;
        }

        rootCanvas = inventoryPanel.GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError(
                "InventoryUI: Canvas not found above Inventory Panel.",
                inventoryPanel
            );
            return;
        }

        GameObject dragObject = new GameObject(
            "InventoryDragIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );

        dragIconRect = dragObject.GetComponent<RectTransform>();
        dragIconRect.SetParent(rootCanvas.transform, false);
        dragIconRect.sizeDelta = Vector2.one * dragIconSize;
        dragIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragIconRect.pivot = new Vector2(0.5f, 0.5f);
        dragIconRect.SetAsLastSibling();

        dragIconImage = dragObject.GetComponent<Image>();
        dragIconImage.preserveAspect = true;
        dragIconImage.raycastTarget = false;

        CanvasGroup canvasGroup = dragObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0.85f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        dragObject.SetActive(false);
    }

    private void BeginDrag(int slotIndex, PointerEventData eventData)
    {
        InventorySlotData slot = inventory.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || dragIconRect == null)
            return;

        draggedSlotIndex = slotIndex;
        dragIconImage.sprite = slotViews[slotIndex].CurrentIcon;
        dragIconImage.enabled = dragIconImage.sprite != null;
        dragIconRect.gameObject.SetActive(true);
        dragIconRect.SetAsLastSibling();
        slotViews[slotIndex].SetSelected(true);
        MoveDragIcon(eventData);
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (draggedSlotIndex < 0 ||
            dragIconRect == null ||
            rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            dragIconRect.localPosition = localPoint;
        }
    }

    private void DropOnSlot(int targetSlotIndex)
    {
        if (draggedSlotIndex < 0)
            return;

        inventory.SwapSlots(draggedSlotIndex, targetSlotIndex);
    }

    private void EndDrag()
    {
        draggedSlotIndex = -1;

        if (dragIconRect != null)
            dragIconRect.gameObject.SetActive(false);

        RefreshSelection();
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        EndDrag();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(open);

        input.SetGameplayEnabled(!open);

        Cursor.lockState = open
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = open;
    }

    private void Refresh()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].Render(inventory.GetSlot(i));
        }
    }

    private void RefreshSelection()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].SetSelected(i == draggedSlotIndex);
        }
    }
}
