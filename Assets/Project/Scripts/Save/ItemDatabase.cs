using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Save/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new();

    private Dictionary<string, ItemDefinition> byId;

    public ItemDefinition FindById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        BuildLookupIfNeeded();
        byId.TryGetValue(itemId, out ItemDefinition item);
        return item;
    }

    private void BuildLookupIfNeeded()
    {
        if (byId != null)
            return;

        byId = new Dictionary<string, ItemDefinition>();

        foreach (ItemDefinition item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                continue;

            if (byId.ContainsKey(item.ItemId))
            {
                Debug.LogError(
                    $"ItemDatabase: повторяется Item Id '{item.ItemId}'.",
                    item
                );
                continue;
            }

            byId.Add(item.ItemId, item);
        }
    }

    private void OnValidate()
    {
        byId = null;
    }
}
