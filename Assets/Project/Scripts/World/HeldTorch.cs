using UnityEngine;

public class HeldTorch : MonoBehaviour
{
    [Header("Fuel")]
    [Min(1.0f)]
    [SerializeField] private float maximumFuelSeconds = 180.0f;

    [Min(0.1f)]
    [SerializeField] private float fadeStartSeconds = 20.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float minimumLightMultiplier = 0.08f;

    [Header("Optional audio")]
    [SerializeField] private AudioSource fireLoopSource;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip extinguishClip;

    private ParticleSystem[] effectSystems;
    private Light[] directLights;
    private float[] initialLightIntensities;
    private float[] initialEmissionMultipliers;

    public float FuelRemaining { get; private set; }
    public float MaximumFuelSeconds => maximumFuelSeconds;
    public bool IsLit { get; private set; }
    public float FuelNormalized => maximumFuelSeconds <= 0.0f
        ? 0.0f
        : Mathf.Clamp01(FuelRemaining / maximumFuelSeconds);

    private void Awake()
    {
        effectSystems = GetComponentsInChildren<ParticleSystem>(true);
        directLights = GetComponentsInChildren<Light>(true);

        initialLightIntensities = new float[directLights.Length];
        initialEmissionMultipliers = new float[effectSystems.Length];

        for (int i = 0; i < directLights.Length; i++)
        {
            if (directLights[i] != null)
                initialLightIntensities[i] = directLights[i].intensity;
        }

        for (int i = 0; i < effectSystems.Length; i++)
        {
            if (effectSystems[i] == null)
                continue;

            ParticleSystem.EmissionModule emission = effectSystems[i].emission;
            initialEmissionMultipliers[i] = emission.rateOverTimeMultiplier;
        }

        FuelRemaining = maximumFuelSeconds;
        SetLit(true, false);
    }

    private void OnEnable()
    {
        ApplyVisualState();
    }

    private void Update()
    {
        if (!IsLit)
            return;

        ConsumeFuel(Time.deltaTime);
    }

    public bool TryConsumeFuel(float seconds)
    {
        if (!IsLit || seconds <= 0.0f || FuelRemaining <= 0.0f)
            return false;

        ConsumeFuel(seconds);
        return true;
    }

    public void ConsumeFuel(float seconds)
    {
        if (!IsLit || seconds <= 0.0f)
            return;

        FuelRemaining = Mathf.Max(0.0f, FuelRemaining - seconds);
        UpdateDimming();

        if (FuelRemaining <= 0.0f)
            Extinguish();
    }

    public void Ignite()
    {
        if (FuelRemaining <= 0.0f)
            return;

        SetLit(true, false);
    }

    public void Extinguish()
    {
        if (!IsLit)
            return;

        SetLit(false, true);
    }

    public void RestoreState(float fuelRemaining, bool lit)
    {
        FuelRemaining = Mathf.Clamp(
            fuelRemaining,
            0.0f,
            maximumFuelSeconds
        );

        SetLit(lit && FuelRemaining > 0.0f, false);
    }

    private void UpdateDimming()
    {
        float multiplier = 1.0f;

        if (FuelRemaining < fadeStartSeconds)
        {
            float fade = Mathf.Clamp01(FuelRemaining / fadeStartSeconds);
            multiplier = Mathf.Lerp(
                minimumLightMultiplier,
                1.0f,
                fade
            );
        }

        for (int i = 0; i < directLights.Length; i++)
        {
            if (directLights[i] != null)
            {
                directLights[i].intensity =
                    initialLightIntensities[i] * multiplier;
            }
        }

        for (int i = 0; i < effectSystems.Length; i++)
        {
            if (effectSystems[i] == null)
                continue;

            ParticleSystem.EmissionModule emission = effectSystems[i].emission;
            emission.rateOverTimeMultiplier =
                initialEmissionMultipliers[i] * multiplier;
        }

        if (fireLoopSource != null)
            fireLoopSource.volume = Mathf.Lerp(0.1f, 1.0f, multiplier);
    }

    private void SetLit(bool lit, bool playExtinguishSound)
    {
        IsLit = lit;
        ApplyVisualState();

        if (playExtinguishSound &&
            oneShotSource != null &&
            extinguishClip != null)
        {
            oneShotSource.PlayOneShot(extinguishClip);
        }
    }

    private void ApplyVisualState()
    {
        for (int i = 0; i < effectSystems.Length; i++)
        {
            ParticleSystem effectSystem = effectSystems[i];

            if (effectSystem == null)
                continue;

            if (IsLit)
                effectSystem.Play(false);
            else
                effectSystem.Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
        }

        for (int i = 0; i < directLights.Length; i++)
        {
            if (directLights[i] != null)
                directLights[i].enabled = IsLit;
        }

        if (fireLoopSource != null)
        {
            if (IsLit && !fireLoopSource.isPlaying)
                fireLoopSource.Play();
            else if (!IsLit)
                fireLoopSource.Stop();
        }

        UpdateDimming();
    }
}
