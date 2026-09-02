using UnityEngine;

[CreateAssetMenu(fileName = "Item_", menuName = "Interact Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private ItemCategory category;
    [SerializeField] private Sprite icon;
    [Min(1)]
    [SerializeField] private int maxStack = 1;
    [SerializeField] private GameObject worldPrefab;
    [SerializeField] private GameObject heldPrefab;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public ItemCategory Category => category;
    public Sprite Icon => icon;
    public int MaxStack => Mathf.Max(1, maxStack);
    public GameObject WorldPrefab => worldPrefab;
    public GameObject HeldPrefab => heldPrefab;
}
