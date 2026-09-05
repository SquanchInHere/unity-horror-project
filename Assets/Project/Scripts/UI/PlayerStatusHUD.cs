using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerStatusHUD : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerSurvival survival;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider staminaSlider;

    [Header("Status")]
    [SerializeField] private GameObject poisonIndicator;

    private void Awake()
    {
        if (health == null)
            health = FindFirstObjectByType<PlayerHealth>();

        if (survival == null)
            survival = FindFirstObjectByType<PlayerSurvival>();
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        health.HealthChanged += UpdateHealth;
        survival.HungerChanged += UpdateHunger;
        survival.StaminaChanged += UpdateStamina;
        survival.PoisonChanged += UpdatePoison;

        UpdateHealth(health.CurrentHealth, health.MaximumHealth);
        UpdateHunger(survival.CurrentHunger, survival.MaximumHunger);
        UpdateStamina(survival.CurrentStamina, survival.MaximumStamina);
        UpdatePoison(survival.PoisonNormalized, survival.IsPoisoned);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.HealthChanged -= UpdateHealth;

        if (survival != null)
        {
            survival.HungerChanged -= UpdateHunger;
            survival.StaminaChanged -= UpdateStamina;
            survival.PoisonChanged -= UpdatePoison;
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (health == null || survival == null)
        {
            Debug.LogError(
                "PlayerStatusHUD: PlayerHealth or PlayerSurvival is not assigned.",
                this
            );
            valid = false;
        }

        if (healthSlider == null ||
            hungerSlider == null ||
            staminaSlider == null)
        {
            Debug.LogError(
                "PlayerStatusHUD: One or more status Sliders are not assigned.",
                this
            );
            valid = false;
        }

        return valid;
    }

    private void UpdateHealth(float current, float maximum)
    {
        SetSliderValue(healthSlider, current, maximum);
    }

    private void UpdateHunger(float current, float maximum)
    {
        SetSliderValue(hungerSlider, current, maximum);
    }

    private void UpdateStamina(float current, float maximum)
    {
        SetSliderValue(staminaSlider, current, maximum);
    }

    private void UpdatePoison(float normalized, bool isPoisoned)
    {
        if (poisonIndicator != null)
            poisonIndicator.SetActive(isPoisoned);
    }

    private static void SetSliderValue(
        Slider slider,
        float current,
        float maximum)
    {
        if (slider == null)
            return;

        slider.minValue = 0.0f;
        slider.maxValue = Mathf.Max(1.0f, maximum);
        slider.value = Mathf.Clamp(current, 0.0f, slider.maxValue);
    }
}
