using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChestInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    [Header("Slot grids")]
    [SerializeField] private Transform playerGrid;
    [SerializeField] private Transform chestGrid;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Player")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerInputReader input;

    [Header("Drag and drop")]
    [Min(16.0f)]
    [SerializeField] private float dragIconSize = 72.0f;

    private InventorySlotUI[] playerViews;
    private InventorySlotUI[] chestViews;
    private LootChest currentChest;

    private Canvas rootCanvas;
    private RectTransform dragIconRect;
    private Image dragIconImage;
    private ContainerType dragSourceType;
    private int dragSourceIndex = -1;
    private int dragAmount;
    private bool dragAllowsSwap;
    private bool initialized;

    private bool IsDragging => dragSourceIndex >= 0;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (input == null && playerInventory != null)
            input = playerInventory.GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        closeButton.onClick.AddListener(Close);
        playerInventory.Changed += Refresh;

        CreatePlayerViews();
        CreateDragIcon();

        initialized = true;
        panel.SetActive(false);
    }

    private void Update()
    {
        if (currentChest == null || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (playerInventory != null)
            playerInventory.Changed -= Refresh;

        DetachCurrentChest();
    }

    public void Open(LootChest chest)
    {
        if (!initialized || chest == null)
            return;

        DetachCurrentChest();

        currentChest = chest;
        currentChest.ContentsChanged += Refresh;

        CreateChestViews();
        EndDrag();

        panel.SetActive(true);
        input.SetGameplayEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
    }

    public void Close()
    {
        if (!initialized || currentChest == null)
            return;

        LootChest chestToClose = currentChest;

        DetachCurrentChest();
        EndDrag();
        panel.SetActive(false);

        input.SetGameplayEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        chestToClose.CloseStorage();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (panel == null)
        {
            Debug.LogError(
                "ChestInventoryUI: Panel is not assigned.",
                this
            );
            valid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError(
                "ChestInventoryUI: Close Button is not assigned.",
                this
            );
            valid = false;
        }

        if (playerGrid == null || chestGrid == null || slotPrefab == null)
        {
            Debug.LogError(
                "ChestInventoryUI: Player Grid, Chest Grid, or Slot Prefab is not assigned.",
                this
            );
            valid = false;
        }

        if (playerInventory == null || input == null)
        {
            Debug.LogError(
                "ChestInventoryUI: PlayerInventory or PlayerInputReader was not found.",
                this
            );
            valid = false;
        }

        return valid;
    }

    private void CreatePlayerViews()
    {
        ClearGrid(playerGrid);
        playerViews = new InventorySlotUI[PlayerInventory.SlotCount];

        for (int i = 0; i < playerViews.Length; i++)
        {
            InventorySlotUI view = Instantiate(slotPrefab, playerGrid);
            ConfigureView(view, ContainerType.Player, i);
            view.gameObject.name = $"PlayerChestSlot_{i + 1}";
            playerViews[i] = view;
        }
    }

    private void CreateChestViews()
    {
        ClearGrid(chestGrid);
        chestViews = new InventorySlotUI[currentChest.Capacity];

        for (int i = 0; i < chestViews.Length; i++)
        {
            InventorySlotUI view = Instantiate(slotPrefab, chestGrid);
            ConfigureView(view, ContainerType.Chest, i);
            view.gameObject.name = $"ChestSlot_{i + 1}";
            chestViews[i] = view;
        }
    }

    private void ConfigureView(
        InventorySlotUI view,
        ContainerType containerType,
        int slotIndex)
    {
        view.Initialize(slotIndex);
        view.SetSlotNumber(slotIndex + 1);
        view.SetSelected(false);
        view.SetRightButtonDragEnabled(true);

        view.DragStarted += (_, eventData) =>
            BeginDrag(containerType, slotIndex, eventData);
        view.DragMoved += MoveDragIcon;
        view.DragFinished += EndDrag;
        view.DropReceived += _ => DropOn(containerType, slotIndex);
    }

    private void CreateDragIcon()
    {
        rootCanvas = panel.GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError(
                "ChestInventoryUI: Canvas was not found above Panel.",
                panel
            );
            return;
        }

        GameObject dragObject = new GameObject(
            "ChestDragIcon",
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

    private void BeginDrag(
        ContainerType sourceType,
        int sourceIndex,
        PointerEventData eventData)
    {
        if (!TryGetSlotContent(
                sourceType,
                sourceIndex,
                out _,
                out int sourceAmount) ||
            dragIconRect == null)
        {
            return;
        }

        bool moveSingleItem =
            eventData.button == PointerEventData.InputButton.Right;

        dragSourceType = sourceType;
        dragSourceIndex = sourceIndex;
        dragAmount = moveSingleItem ? 1 : sourceAmount;
        dragAllowsSwap = !moveSingleItem;

        dragIconImage.sprite = GetView(sourceType, sourceIndex)?.CurrentIcon;
        dragIconImage.enabled = dragIconImage.sprite != null;
        dragIconRect.gameObject.SetActive(true);
        dragIconRect.SetAsLastSibling();

        SetSelected(sourceType, sourceIndex, true);
        MoveDragIcon(eventData);
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (!IsDragging || dragIconRect == null || rootCanvas == null)
            return;

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

    private void DropOn(ContainerType targetType, int targetIndex)
    {
        if (!IsDragging || currentChest == null)
            return;

        if (dragSourceType == targetType && dragSourceIndex == targetIndex)
            return;

        if (dragSourceType == ContainerType.Player &&
            targetType == ContainerType.Player)
        {
            playerInventory.TryTransferBetweenSlots(
                dragSourceIndex,
                targetIndex,
                dragAmount,
                dragAllowsSwap
            );
        }
        else if (dragSourceType == ContainerType.Chest &&
                 targetType == ContainerType.Chest)
        {
            currentChest.TryTransferBetweenSlots(
                dragSourceIndex,
                targetIndex,
                dragAmount,
                dragAllowsSwap
            );
        }
        else if (dragSourceType == ContainerType.Player)
        {
            currentChest.TryStoreFromPlayer(
                playerInventory,
                dragSourceIndex,
                targetIndex,
                dragAmount,
                dragAllowsSwap
            );
        }
        else
        {
            currentChest.TryTakeToPlayer(
                playerInventory,
                dragSourceIndex,
                targetIndex,
                dragAmount,
                dragAllowsSwap
            );
        }
    }

    private void EndDrag()
    {
        if (dragIconRect != null)
            dragIconRect.gameObject.SetActive(false);

        ClearSelection(playerViews);
        ClearSelection(chestViews);

        dragSourceIndex = -1;
        dragAmount = 0;
        dragAllowsSwap = false;
    }

    private void Refresh()
    {
        if (playerViews != null)
        {
            for (int i = 0; i < playerViews.Length; i++)
            {
                if (playerViews[i] != null)
                    playerViews[i].Render(playerInventory.GetSlot(i));
            }
        }

        if (currentChest == null || chestViews == null)
            return;

        for (int i = 0; i < chestViews.Length; i++)
        {
            if (chestViews[i] == null)
                continue;

            ChestLootEntry slot = currentChest.GetSlot(i);

            if (slot == null || slot.IsEmpty)
                chestViews[i].Render(null, 0);
            else
                chestViews[i].Render(slot.item, slot.amount);
        }
    }

    private bool TryGetSlotContent(
        ContainerType containerType,
        int slotIndex,
        out ItemDefinition item,
        out int amount)
    {
        item = null;
        amount = 0;

        if (containerType == ContainerType.Player)
        {
            InventorySlotData slot = playerInventory.GetSlot(slotIndex);

            if (slot == null || slot.IsEmpty)
                return false;

            item = slot.Item;
            amount = slot.Amount;
            return true;
        }

        ChestLootEntry chestSlot = currentChest?.GetSlot(slotIndex);

        if (chestSlot == null || chestSlot.IsEmpty)
            return false;

        item = chestSlot.item;
        amount = chestSlot.amount;
        return true;
    }

    private InventorySlotUI GetView(
        ContainerType containerType,
        int slotIndex)
    {
        InventorySlotUI[] views = containerType == ContainerType.Player
            ? playerViews
            : chestViews;

        if (views == null || slotIndex < 0 || slotIndex >= views.Length)
            return null;

        return views[slotIndex];
    }

    private void SetSelected(
        ContainerType containerType,
        int slotIndex,
        bool selected)
    {
        GetView(containerType, slotIndex)?.SetSelected(selected);
    }

    private void DetachCurrentChest()
    {
        if (currentChest != null)
            currentChest.ContentsChanged -= Refresh;

        currentChest = null;
    }

    private static void ClearSelection(InventorySlotUI[] views)
    {
        if (views == null)
            return;

        foreach (InventorySlotUI view in views)
        {
            if (view != null)
                view.SetSelected(false);
        }
    }

    private static void ClearGrid(Transform grid)
    {
        if (grid == null)
            return;

        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);
    }
}
