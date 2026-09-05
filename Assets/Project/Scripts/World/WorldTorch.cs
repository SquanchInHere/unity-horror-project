using UnityEngine;

public class WorldTorch : InteractableBase
{
    [Header("State")]
    [SerializeField] private bool startsLit;
    [SerializeField] private bool allowExtinguish;

    [Header("Ignition")]
    [Min(0.0f)]
    [SerializeField] private float ignitionFuelCost = 5.0f;

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
        effectSystems = GetComponentsInChildren<ParticleSystem>(true);
        directLights = GetComponentsInChildren<Light>(true);
        SetLit(startsLit, false);
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        if (IsLit)
            return allowExtinguish;

        HeldTorch heldTorch = interactor.Hotbar != null
            ? interactor.Hotbar.GetSelectedHeldComponent<HeldTorch>()
            : null;

        return heldTorch != null && heldTorch.IsLit;
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        return IsLit ? "Extinguish the torch" : "Light the torch";
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (IsLit)
        {
            if (allowExtinguish)
                SetLit(false, true);

            return;
        }

        HeldTorch heldTorch = interactor.Hotbar != null
            ? interactor.Hotbar.GetSelectedHeldComponent<HeldTorch>()
            : null;

        if (heldTorch == null || !heldTorch.IsLit)
            return;

        if (!heldTorch.TryConsumeFuel(ignitionFuelCost))
            return;

        SetLit(true, true);
    }

    public void RestoreLitState(bool lit)
    {
        SetLit(lit, false);
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
}