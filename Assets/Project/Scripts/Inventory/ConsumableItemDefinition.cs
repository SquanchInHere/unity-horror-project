using UnityEngine;

[CreateAssetMenu(
    fileName = "Consumable_",
    menuName = "Interact Items/Consumable Item Definition")]
public class ConsumableItemDefinition : ItemDefinition
{
    [Header("Immediate effects")]
    [Min(0.0f)]
    [SerializeField] private float hungerRestored;

    [Min(0.0f)]
    [SerializeField] private float staminaRestored;

    [Min(0.0f)]
    [SerializeField] private float healthRestored;

    [Header("Status effects")]
    [SerializeField] private bool appliesPoison;
    [SerializeField] private bool curesPoison;

    public float HungerRestored => hungerRestored;
    public float StaminaRestored => staminaRestored;
    public float HealthRestored => healthRestored;
    public bool AppliesPoison => appliesPoison;
    public bool CuresPoison => curesPoison;
}
