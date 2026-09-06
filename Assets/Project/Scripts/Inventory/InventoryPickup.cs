using UnityEngine;

public class InventoryPickup : InteractableBase
{
    [SerializeField] private ItemDefinition item;

    [Min(1)]
    [SerializeField] private int amount = 1;

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (item == null)
            return "Unknown item";

        return $"Take: {item.DisplayName}";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (item == null || interactor == null)
            return;

        PlayerInventory inventory = interactor.Inventory;

        if (inventory == null)
            return;

        inventory.TryAdd(item, amount, out int notAdded);

        if (notAdded <= 0)
        {
            Destroy(gameObject);
            return;
        }
        
        amount = notAdded;
        Debug.Log("There is not enough space in the inventory.", this);
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }
}
