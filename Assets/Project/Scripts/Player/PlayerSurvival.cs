using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerSurvival : MonoBehaviour
{
    // This component owns survival values and calculates their gameplay effects.
    // PlayerHealth remains responsible for storing health and handling death.
    [Header("Hunger")]
    [Min(1.0f)]
    [SerializeField] private float maximumHunger = 100.0f;

    [Min(0.0f)]
    [SerializeField] private float startingHunger = 100.0f;

    [Min(1.0f)]
    [SerializeField] private float fullHungerDepletionSeconds = 900.0f;

    [Range(0.01f, 1.0f)]
    [SerializeField] private float lowHungerThreshold = 0.2f;

    [Range(0.1f, 1.0f)]
    [SerializeField] private float starvingMovementMultiplier = 0.45f;

    [Header("Stamina")]
    [Min(1.0f)]
    [SerializeField] private float maximumStamina = 100.0f;

    [Min(0.0f)]
    [SerializeField] private float startingStamina = 100.0f;

    [Min(0.01f)]
    [SerializeField] private float sprintStaminaDrainPerSecond = 20.0f;

    [Min(0.01f)]
    [SerializeField] private float staminaRecoveryPerSecond = 16.0f;

    [Min(0.0f)]
    [SerializeField] private float staminaRecoveryDelay = 1.0f;

    [Min(0.0f)]
    [SerializeField] private float staminaRequiredAfterExhaustion = 20.0f;

    [Header("Poison")]
    [Min(1.0f)]
    [SerializeField] private float poisonDurationSeconds = 120.0f;

    [Min(1.0f)]
    [SerializeField] private float poisonedHungerDrainMultiplier = 2.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float poisonedStaminaRecoveryMultiplier = 0.35f;

    [Range(0.1f, 1.0f)]
    [SerializeField] private float poisonedMovementMultiplier = 0.65f;

    [Header("Health damage")]
    [Min(0.0f)]
    [SerializeField] private float starvationDamagePerSecond = 1.0f;

    [Min(0.0f)]
    [SerializeField]
    private float poisonDamagePerSecondAtFullStrength = 0.5f;

    private PlayerHealth health;
    private bool isSprinting;
    private bool sprintLocked;
    private float timeSinceSprint;

    public float CurrentHunger { get; private set; }
    public float CurrentStamina { get; private set; }
    public float PoisonTimeRemaining { get; private set; }

    public float MaximumHunger => maximumHunger;
    public float MaximumStamina => maximumStamina;

    // Normalized values are always between 0 and 1. They are convenient for
    // UI bars, Volume effects, and interpolation between weak and strong states.
    public float HungerNormalized => maximumHunger <= 0.0f
        ? 0.0f
        : Mathf.Clamp01(CurrentHunger / maximumHunger);

    public float StaminaNormalized => maximumStamina <= 0.0f
        ? 0.0f
        : Mathf.Clamp01(CurrentStamina / maximumStamina);

    public float PoisonNormalized => poisonDurationSeconds <= 0.0f
        ? 0.0f
        : Mathf.Clamp01(PoisonTimeRemaining / poisonDurationSeconds);

    public bool IsPoisoned => PoisonTimeRemaining > 0.0f;

    // After complete exhaustion, sprinting stays locked until enough stamina
    // has recovered. This prevents rapid switching between sprint and walk.
    public bool CanSprint => !sprintLocked && CurrentStamina > 0.0f;

    public float MovementSpeedMultiplier
    {
        get
        {
            float hungerMultiplier = 1.0f;

            // Hunger only slows the player below the configured threshold.
            // InverseLerp converts the low-hunger range into a 0..1 value.
            if (HungerNormalized < lowHungerThreshold)
            {
                float hungerProgress = Mathf.InverseLerp(
                    0.0f,
                    lowHungerThreshold,
                    HungerNormalized
                );

                hungerMultiplier = Mathf.Lerp(
                    starvingMovementMultiplier,
                    1.0f,
                    hungerProgress
                );
            }

            // PoisonNormalized starts at 1 and gradually falls to 0, so the
            // movement penalty becomes weaker during the poison duration.
            float poisonMultiplier = Mathf.Lerp(
                1.0f,
                poisonedMovementMultiplier,
                PoisonNormalized
            );

            return hungerMultiplier * poisonMultiplier;
        }
    }

    // UI and other systems subscribe to these events instead of polling values.
    public event Action<float, float> HungerChanged;
    public event Action<float, float> StaminaChanged;
    public event Action<float, bool> PoisonChanged;
    public event Action<ConsumableItemDefinition> ConsumableUsed;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        CurrentHunger = Mathf.Clamp(startingHunger, 0.0f, maximumHunger);
        CurrentStamina = Mathf.Clamp(startingStamina, 0.0f, maximumStamina);
        PoisonTimeRemaining = 0.0f;
        sprintLocked = CurrentStamina <= 0.0f;
    }

    private void Start()
    {
        HungerChanged?.Invoke(CurrentHunger, MaximumHunger);
        StaminaChanged?.Invoke(CurrentStamina, MaximumStamina);
        PoisonChanged?.Invoke(PoisonNormalized, IsPoisoned);
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            isSprinting = false;
            return;
        }

        float deltaTime = Time.deltaTime;

        // Each subsystem receives the same frame delta to keep all timers
        // independent from the current frame rate.
        UpdatePoison(deltaTime);
        UpdateHunger(deltaTime);
        UpdateStamina(deltaTime);
        UpdateHealthDamage(deltaTime);
    }

    public void SetSprinting(bool sprinting)
    {
        // The movement controller reports whether the player is actually
        // moving at sprint speed. Holding Sprint while standing does not drain.
        isSprinting = sprinting && CanSprint;
    }

    /// <summary>
    /// Applies the data stored in a consumable asset to the player.
    /// The inventory item is removed by HotbarController only after this succeeds.
    /// </summary>
    public bool TryConsume(ConsumableItemDefinition consumable)
    {
        if (consumable == null ||
            (health != null && health.IsDead))
        {
            return false;
        }

        ChangeHunger(consumable.HungerRestored);
        ChangeStamina(consumable.StaminaRestored);

        if (health != null && consumable.HealthRestored > 0.0f)
            health.Heal(consumable.HealthRestored);

        if (consumable.AppliesPoison)
            ApplyPoison();

        if (consumable.CuresPoison)
            CurePoison();

        ConsumableUsed?.Invoke(consumable);
        return true;
    }

    public void ApplyPoison()
    {
        // Consuming another poisonous item restarts the poison at full strength.
        PoisonTimeRemaining = poisonDurationSeconds;
        PoisonChanged?.Invoke(PoisonNormalized, IsPoisoned);
    }

    public void CurePoison()
    {
        if (!IsPoisoned)
            return;

        PoisonTimeRemaining = 0.0f;
        PoisonChanged?.Invoke(0.0f, false);
    }

    public void ChangeHunger(float amount)
    {
        SetHunger(CurrentHunger + amount);
    }

    public void ChangeStamina(float amount)
    {
        SetStamina(CurrentStamina + amount);
    }

    private void UpdateHunger(float deltaTime)
    {
        if (CurrentHunger <= 0.0f)
            return;

        // Dividing the maximum value by the configured lifetime produces the
        // exact per-second drain required to reach zero in that time.
        float baseDrainPerSecond =
            maximumHunger / fullHungerDepletionSeconds;

        float poisonDrainMultiplier = Mathf.Lerp(
            1.0f,
            poisonedHungerDrainMultiplier,
            PoisonNormalized
        );

        SetHunger(
            CurrentHunger -
            baseDrainPerSecond * poisonDrainMultiplier * deltaTime
        );
    }

    private void UpdateStamina(float deltaTime)
    {
        if (isSprinting && CanSprint)
        {
            timeSinceSprint = 0.0f;
            SetStamina(
                CurrentStamina -
                sprintStaminaDrainPerSecond * deltaTime
            );

            if (CurrentStamina <= 0.0f)
            {
                sprintLocked = true;
                isSprinting = false;
            }

            return;
        }

        isSprinting = false;
        timeSinceSprint += deltaTime;

        if (timeSinceSprint < staminaRecoveryDelay ||
            CurrentStamina >= MaximumStamina)
        {
            return;
        }

        // Recovery gradually improves as PoisonNormalized approaches zero.
        float poisonRecoveryMultiplier = Mathf.Lerp(
            1.0f,
            poisonedStaminaRecoveryMultiplier,
            PoisonNormalized
        );

        SetStamina(
            CurrentStamina +
            staminaRecoveryPerSecond * poisonRecoveryMultiplier * deltaTime
        );

        if (sprintLocked &&
            CurrentStamina >= staminaRequiredAfterExhaustion)
        {
            sprintLocked = false;
        }
    }

    private void UpdatePoison(float deltaTime)
    {
        if (!IsPoisoned)
            return;

        // Mathf.Max prevents the timer from becoming negative.
        PoisonTimeRemaining = Mathf.Max(
            0.0f,
            PoisonTimeRemaining - deltaTime
        );

        PoisonChanged?.Invoke(PoisonNormalized, IsPoisoned);
    }

    private void UpdateHealthDamage(float deltaTime)
    {
        if (health == null || health.IsDead)
            return;

        // Add all active damage rates first, then call TakeDamage once.
        // This keeps death and HealthChanged handling inside PlayerHealth.
        float damagePerSecond = 0.0f;

        if (CurrentHunger <= 0.0f)
            damagePerSecond += starvationDamagePerSecond;

        if (IsPoisoned)
        {
            // Poison damage fades linearly together with all other poison effects.
            damagePerSecond +=
                poisonDamagePerSecondAtFullStrength * PoisonNormalized;
        }

        if (damagePerSecond > 0.0f)
            health.TakeDamage(damagePerSecond * deltaTime);
    }

    private void SetHunger(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0f, MaximumHunger);

        // Avoid sending an event when clamping produced the existing value.
        if (Mathf.Approximately(CurrentHunger, clampedValue))
            return;

        CurrentHunger = clampedValue;
        HungerChanged?.Invoke(CurrentHunger, MaximumHunger);
    }

    private void SetStamina(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0f, MaximumStamina);

        if (Mathf.Approximately(CurrentStamina, clampedValue))
            return;

        CurrentStamina = clampedValue;
        StaminaChanged?.Invoke(CurrentStamina, MaximumStamina);
    }

    private void OnValidate()
    {
        maximumHunger = Mathf.Max(1.0f, maximumHunger);
        startingHunger = Mathf.Clamp(startingHunger, 0.0f, maximumHunger);
        fullHungerDepletionSeconds = Mathf.Max(
            1.0f,
            fullHungerDepletionSeconds
        );

        maximumStamina = Mathf.Max(1.0f, maximumStamina);
        startingStamina = Mathf.Clamp(
            startingStamina,
            0.0f,
            maximumStamina
        );
        staminaRequiredAfterExhaustion = Mathf.Clamp(
            staminaRequiredAfterExhaustion,
            0.0f,
            maximumStamina
        );

        poisonDurationSeconds = Mathf.Max(1.0f, poisonDurationSeconds);
        starvationDamagePerSecond = Mathf.Max(
            0.0f,
            starvationDamagePerSecond
        );
        poisonDamagePerSecondAtFullStrength = Mathf.Max(
            0.0f,
            poisonDamagePerSecondAtFullStrength
        );
    }
}
