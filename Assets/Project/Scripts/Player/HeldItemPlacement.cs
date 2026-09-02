using UnityEngine;

[DisallowMultipleComponent]
public class HeldItemPlacement : MonoBehaviour
{
    [Header("Transform relative to HeldItemRoot")]
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;

    public void ApplyTo(Transform heldSlot)
    {
        if (heldSlot == null)
            return;

        heldSlot.localPosition = localPosition;
        heldSlot.localRotation = Quaternion.Euler(localEulerAngles);
        heldSlot.localScale = localScale;
    }

    private void OnValidate()
    {
        if (localScale == Vector3.zero)
        {
            localScale = Vector3.one;
            return;
        }

        localScale.x = Mathf.Max(0.0001f, localScale.x);
        localScale.y = Mathf.Max(0.0001f, localScale.y);
        localScale.z = Mathf.Max(0.0001f, localScale.z);
    }
}
