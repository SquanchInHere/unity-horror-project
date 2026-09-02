using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Min(1.0f)]
    [SerializeField] private float maximumHealth = 100.0f;

    public float CurrentHealth { get; private set; }
    public float MaximumHealth => maximumHealth;
    public bool IsDead { get; private set; }

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maximumHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0.0f)
            return;

        CurrentHealth = Mathf.Max(0.0f, CurrentHealth - damage);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);

        if (CurrentHealth <= 0.0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0.0f)
            return;

        CurrentHealth = Mathf.Min(MaximumHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
    }

    public void RestoreHealth(float savedHealth)
    {
        IsDead = false;
        CurrentHealth = Mathf.Clamp(savedHealth, 1.0f, maximumHealth);

        PlayerInputReader input = GetComponent<PlayerInputReader>();

        if (input != null)
            input.SetGameplayEnabled(true);

        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
    }

    private void Die()
    {
        IsDead = true;

        PlayerInputReader input = GetComponent<PlayerInputReader>();

        if (input != null)
            input.SetGameplayEnabled(false);

        Died?.Invoke();
    }
}
