using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class PoisonVolumeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSurvival survival;
    [SerializeField] private Volume globalVolume;

    [Header("Poison targets")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float chromaticAberrationIntensity = 0.65f;

    [Range(-1.0f, 1.0f)]
    [SerializeField] private float lensDistortionIntensity = -0.35f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float vignetteIntensity = 0.45f;

    [Min(0.01f)]
    [SerializeField] private float transitionSpeed = 2.0f;

    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Vignette vignette;

    private float baseChromaticAberration;
    private float baseLensDistortion;
    private float baseVignette;
    private bool baseChromaticAberrationActive;
    private bool baseLensDistortionActive;
    private bool baseVignetteActive;
    private bool baseChromaticAberrationOverride;
    private bool baseLensDistortionOverride;
    private bool baseVignetteOverride;
    private float currentPoisonWeight;
    private bool initialized;

    private void Awake()
    {
        if (survival == null)
            survival = FindFirstObjectByType<PlayerSurvival>();

        if (globalVolume == null)
            globalVolume = FindGlobalVolume();

        initialized = InitializeVolumeOverrides();
    }

    private void Update()
    {
        if (!initialized || survival == null)
            return;

        currentPoisonWeight = Mathf.MoveTowards(
            currentPoisonWeight,
            survival.PoisonNormalized,
            transitionSpeed * Time.deltaTime
        );

        ApplyPoisonWeight(currentPoisonWeight);
    }

    private void OnDisable()
    {
        RestoreBaseValues();
    }

    private bool InitializeVolumeOverrides()
    {
        if (survival == null || globalVolume == null)
        {
            Debug.LogError(
                "PoisonVolumeController: PlayerSurvival or Global Volume is not assigned.",
                this
            );
            return false;
        }

        VolumeProfile runtimeProfile = globalVolume.profile;

        if (runtimeProfile == null)
        {
            Debug.LogError(
                "PoisonVolumeController: Global Volume has no Volume Profile.",
                globalVolume
            );
            return false;
        }

        bool hasChromaticAberration =
            runtimeProfile.TryGet(out chromaticAberration);
        bool hasLensDistortion =
            runtimeProfile.TryGet(out lensDistortion);
        bool hasVignette = runtimeProfile.TryGet(out vignette);

        if (!hasChromaticAberration ||
            !hasLensDistortion ||
            !hasVignette)
        {
            Debug.LogError(
                "PoisonVolumeController: Add Chromatic Aberration, Lens Distortion, and Vignette overrides to the assigned Volume Profile.",
                globalVolume
            );
            return false;
        }

        baseChromaticAberration = chromaticAberration.intensity.value;
        baseLensDistortion = lensDistortion.intensity.value;
        baseVignette = vignette.intensity.value;
        baseChromaticAberrationActive = chromaticAberration.active;
        baseLensDistortionActive = lensDistortion.active;
        baseVignetteActive = vignette.active;
        baseChromaticAberrationOverride =
            chromaticAberration.intensity.overrideState;
        baseLensDistortionOverride =
            lensDistortion.intensity.overrideState;
        baseVignetteOverride = vignette.intensity.overrideState;

        currentPoisonWeight = survival.PoisonNormalized;
        ApplyPoisonWeight(currentPoisonWeight);
        return true;
    }

    private void ApplyPoisonWeight(float weight)
    {
        float clampedWeight = Mathf.Clamp01(weight);
        bool poisonEffectActive = clampedWeight > 0.0001f;

        chromaticAberration.active =
            poisonEffectActive || baseChromaticAberrationActive;
        chromaticAberration.intensity.overrideState =
            poisonEffectActive || baseChromaticAberrationOverride;

        lensDistortion.active =
            poisonEffectActive || baseLensDistortionActive;
        lensDistortion.intensity.overrideState =
            poisonEffectActive || baseLensDistortionOverride;

        vignette.active = poisonEffectActive || baseVignetteActive;
        vignette.intensity.overrideState =
            poisonEffectActive || baseVignetteOverride;

        chromaticAberration.intensity.value = Mathf.Lerp(
            baseChromaticAberration,
            chromaticAberrationIntensity,
            clampedWeight
        );

        lensDistortion.intensity.value = Mathf.Lerp(
            baseLensDistortion,
            lensDistortionIntensity,
            clampedWeight
        );

        vignette.intensity.value = Mathf.Lerp(
            baseVignette,
            vignetteIntensity,
            clampedWeight
        );
    }

    private void RestoreBaseValues()
    {
        if (!initialized)
            return;

        chromaticAberration.intensity.value = baseChromaticAberration;
        lensDistortion.intensity.value = baseLensDistortion;
        vignette.intensity.value = baseVignette;
        chromaticAberration.active = baseChromaticAberrationActive;
        lensDistortion.active = baseLensDistortionActive;
        vignette.active = baseVignetteActive;
        chromaticAberration.intensity.overrideState =
            baseChromaticAberrationOverride;
        lensDistortion.intensity.overrideState =
            baseLensDistortionOverride;
        vignette.intensity.overrideState = baseVignetteOverride;
    }

    private static Volume FindGlobalVolume()
    {
        Volume[] volumes = FindObjectsByType<Volume>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Volume volume in volumes)
        {
            if (volume != null && volume.isGlobal)
                return volume;
        }

        return null;
    }
}
