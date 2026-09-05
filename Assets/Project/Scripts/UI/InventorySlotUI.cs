using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private GameObject selection;
    [SerializeField] private Sprite missingIcon;

    private bool hasItem;
    private bool allowRightButtonDrag;

    public int SlotIndex { get; private set; } = -1;
    public Sprite CurrentIcon => icon != null ? icon.sprite : null;

    public event Action<int, PointerEventData> DragStarted;
    public event Action<PointerEventData> DragMoved;
    public event Action DragFinished;
    public event Action<int> DropReceived;

    public Button Button
    {
        get
        {
            if (button == null)
                button = GetComponent<Button>();

            return button;
        }
    }

    private void Awake()
    {
        if (icon != null)
            icon.raycastTarget = false;

        if (amountText != null)
            amountText.raycastTarget = false;

        if (numberText != null)
            numberText.raycastTarget = false;
    }

    public void Initialize(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public void SetSlotNumber(int slotNumber)
    {
        if (numberText != null)
            numberText.text = slotNumber.ToString();
    }

    public void Render(InventorySlotData slot)
    {
        ItemDefinition item = slot != null && !slot.IsEmpty
            ? slot.Item
            : null;
        int amount = slot != null && !slot.IsEmpty
            ? slot.Amount
            : 0;

        Render(item, amount);
    }

    public void Render(ItemDefinition item, int amount)
    {
        hasItem = item != null && amount > 0;

        if (icon != null)
        {
            icon.sprite = hasItem
                ? item.Icon != null
                    ? item.Icon
                    : missingIcon
                : null;

            icon.enabled = hasItem && icon.sprite != null;
        }

        if (amountText != null)
        {
            amountText.text = hasItem && amount > 1
                ? amount.ToString()
                : string.Empty;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selection != null)
            selection.SetActive(isSelected);
    }

    public void SetRightButtonDragEnabled(bool enabled)
    {
        allowRightButtonDrag = enabled;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!hasItem || !IsSupportedDragButton(eventData.button))
        {
            return;
        }

        DragStarted?.Invoke(SlotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!hasItem || !IsSupportedDragButton(eventData.button))
        {
            return;
        }

        DragMoved?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsSupportedDragButton(eventData.button))
            DragFinished?.Invoke();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (IsSupportedDragButton(eventData.button))
            DropReceived?.Invoke(SlotIndex);
    }

    private bool IsSupportedDragButton(
        PointerEventData.InputButton inputButton)
    {
        return inputButton == PointerEventData.InputButton.Left ||
               (allowRightButtonDrag &&
                inputButton == PointerEventData.InputButton.Right);
    }
}
