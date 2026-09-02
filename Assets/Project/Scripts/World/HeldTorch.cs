using UnityEngine;

public class HeldTorch : MonoBehaviour
{
    [Header("Fuel")]
    [Min(1.0f)]
    [SerializeField] private float fuelSeconds = 180.0f;

    [Header("Optional audio")]
    [SerializeField] private AudioSource fireLoopSource;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip extinguishClip;

    private ParticleSystem[] effectSystems;
    private Light[] directLights;

    public float FuelRemaining { get; private set; }
    public bool IsLit { get; private set; }

    private void Awake()
    {
        effectSystems = GetComponentsInChildren<ParticleSystem>(true);
        directLights = GetComponentsInChildren<Light>(true);

        FuelRemaining = fuelSeconds;
        SetVisualState(true);
    }

    private void OnEnable()
    {
        SetVisualState(IsLit);

        if (IsLit && fireLoopSource != null && !fireLoopSource.isPlaying)
            fireLoopSource.Play();
    }

    private void Update()
    {
        if (!IsLit)
            return;

        FuelRemaining -= Time.deltaTime;

        if (FuelRemaining <= 0.0f)
        {
            FuelRemaining = 0.0f;
            Extinguish();
            return;
        }

    }

    public void Extinguish()
    {
        if (!IsLit)
            return;

        SetVisualState(false);

        if (oneShotSource != null && extinguishClip != null)
            oneShotSource.PlayOneShot(extinguishClip);
    }

    private void SetVisualState(bool lit)
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
    }
}
