using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [SerializeField] private float damage = 25.0f;
    [SerializeField] private bool damageOnlyOnce = true;

    private bool wasTriggered;

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (damageOnlyOnce && wasTriggered)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health == null)
            return;

        wasTriggered = true;
        health.TakeDamage(damage);
    }
}
