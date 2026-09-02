using UnityEngine;
using UnityEngine.InputSystem;

public class ChestInventoryUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Panel")]
    [SerializeField] private GameObject chestPanel;
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private Transform chestSlotsContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    private LootChest currentChest;
    private InventorySlotUI[] playerViews;
    private InventorySlotUI[] chestViews;

    public bool IsOpen => currentChest != null;

    private void Start()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (input == null && playerInventory != null)
            input = playerInventory.GetComponent<PlayerInputReader>();

        CreatePlayerSlots();

        if (playerInventory != null)
            playerInventory.Changed += Refresh;

        if (chestPanel != null)
            chestPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.Changed -= Refresh;

        UnsubscribeChest();
    }

    public void Open(LootChest chest)
    {
        if (chest == null || playerInventory == null || input == null)
        {
            Debug.LogError(
                "ChestInventoryUI: не назначены Chest, PlayerInventory или PlayerInputReader.",
                this
            );
            return;
        }

        UnsubscribeChest();
        currentChest = chest;
        currentChest.ContentsChanged += Refresh;
        RebuildChestSlots();

        if (chestPanel != null)
            chestPanel.SetActive(true);

        input.SetGameplayEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        UnsubscribeChest();

        if (chestPanel != null)
            chestPanel.SetActive(false);

        if (input != null)
            input.SetGameplayEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CreatePlayerSlots()
    {
        if (slotPrefab == null || playerSlotsContainer == null)
        {
            Debug.LogError(
                "ChestInventoryUI: назначь Slot Prefab и Player Slots Container.",
                this
            );
            playerViews = new InventorySlotUI[0];
            return;
        }

        playerViews = new InventorySlotUI[PlayerInventory.SlotCount];

        for (int i = 0; i < playerViews.Length; i++)
        {
            int capturedIndex = i;
            InventorySlotUI view = Instantiate(slotPrefab, playerSlotsContainer);
            view.gameObject.name = $"PlayerSlot_{i + 1}";
            view.Initialize(i);
            view.SetSlotNumber(i + 1);
            view.SetSelected(false);
            view.Button.onClick.AddListener(
                () => StorePlayerSlot(capturedIndex)
            );
            playerViews[i] = view;
        }
    }

    private void RebuildChestSlots()
    {
        if (slotPrefab == null || chestSlotsContainer == null || currentChest == null)
        {
            Debug.LogError(
                "ChestInventoryUI: назначь Slot Prefab и Chest Slots Container.",
                this
            );
            chestViews = new InventorySlotUI[0];
            return;
        }

        if (chestViews != null)
        {
            foreach (InventorySlotUI view in chestViews)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
        }

        chestViews = new InventorySlotUI[currentChest.Capacity];

        for (int i = 0; i < chestViews.Length; i++)
        {
            int capturedIndex = i;
            InventorySlotUI view = Instantiate(slotPrefab, chestSlotsContainer);
            view.gameObject.name = $"ChestSlot_{i + 1}";
            view.Initialize(i);
            view.SetSlotNumber(i + 1);
            view.SetSelected(false);
            view.Button.onClick.AddListener(
                () => TakeChestSlot(capturedIndex)
            );
            chestViews[i] = view;
        }
    }

    private void StorePlayerSlot(int slotIndex)
    {
        if (currentChest != null)
            currentChest.StorePlayerSlot(slotIndex, playerInventory);
    }

    private void TakeChestSlot(int slotIndex)
    {
        if (currentChest != null)
            currentChest.TakeSlot(slotIndex, playerInventory);
    }

    private void Refresh()
    {
        if (playerViews != null && playerInventory != null)
        {
            for (int i = 0; i < playerViews.Length; i++)
                playerViews[i]?.Render(playerInventory.GetSlot(i));
        }

        if (chestViews == null || currentChest == null)
            return;

        for (int i = 0; i < chestViews.Length; i++)
        {
            ChestLootEntry entry = currentChest.GetSlot(i);
            InventorySlotData renderData = new InventorySlotData();

            if (entry != null && !entry.IsEmpty)
                renderData.Set(entry.item, entry.amount);

            chestViews[i]?.Render(renderData);
        }
    }

    private void UnsubscribeChest()
    {
        if (currentChest != null)
            currentChest.ContentsChanged -= Refresh;

        currentChest = null;
    }
}
