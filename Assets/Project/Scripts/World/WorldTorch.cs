using UnityEngine;

public class WorldTorch : InteractableBase
{
    [Header("State")]
    [SerializeField] private bool startsLit;
    [SerializeField] private bool allowExtinguish;

    [Header("Requirement")]
    [SerializeField] private ItemDefinition requiredItem;
    [SerializeField] private bool consumeRequiredItem;

    [Header("Optional audio")]
    [SerializeField] private AudioSource fireLoopSource;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip igniteClip;
    [SerializeField] private AudioClip extinguishClip;

    private ParticleSystem[] effectSystems;
    private Light[] directLights;

    public bool IsLit { get; private set; }

    private void Awake()
    {
        // Находит готовые Particle System огня и дыма внутри prefab.
        effectSystems = GetComponentsInChildren<ParticleSystem>(true);

        // Это только обычные дочерние Light. Свет из Particle System Lights
        // отдельно искать не нужно: он исчезает при остановке частиц.
        directLights = GetComponentsInChildren<Light>(true);

        SetLit(startsLit, false);
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (!IsLit)
            return "Зажечь факел";

        return allowExtinguish
            ? "Потушить факел"
            : "Факел горит";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (IsLit)
        {
            if (allowExtinguish)
                SetLit(false, true);

            return;
        }

        PlayerInventory inventory = interactor.Inventory;

        if (requiredItem != null)
        {
            if (inventory == null || !inventory.HasItem(requiredItem, 1))
            {
                Debug.Log($"Нужен предмет: {requiredItem.DisplayName}", this);
                return;
            }

            if (consumeRequiredItem)
                inventory.RemoveItem(requiredItem, 1);
        }

        SetLit(true, true);
    }

    private void SetLit(bool lit, bool playSound)
    {
        IsLit = lit;

        foreach (ParticleSystem effectSystem in effectSystems)
        {
            if (effectSystem == null)
                continue;

            if (lit)
                effectSystem.Play(false);
            else
                effectSystem.Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
        }

        foreach (Light directLight in directLights)
        {
            if (directLight != null)
                directLight.enabled = lit;
        }

        if (fireLoopSource != null)
        {
            if (lit)
                fireLoopSource.Play();
            else
                fireLoopSource.Stop();
        }

        if (!playSound || oneShotSource == null)
            return;

        AudioClip clip = lit ? igniteClip : extinguishClip;

        if (clip != null)
            oneShotSource.PlayOneShot(clip);
    }

    public void RestoreLitState(bool lit)
    {
        SetLit(lit, false);
    }
}
