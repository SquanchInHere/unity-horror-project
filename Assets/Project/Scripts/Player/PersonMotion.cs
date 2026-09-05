using UnityEngine;


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PersonMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraMotionRoot;
    [SerializeField] private Transform heldItemRoot;

    [Header("Camera bob")]
    [SerializeField] private float walkFrequency = 8.5f;
    [SerializeField] private float sprintFrequency = 11.5f;
    [SerializeField] private float horizontalAmplitude = 0.018f;
    [SerializeField] private float verticalAmplitude = 0.028f;
    [SerializeField] private float returnSpeed = 10.0f;

    [Header("Held item motion")]
    [SerializeField] private float lookSway = 1.5f;
    [SerializeField] private float movementSway = 2.0f;
    [SerializeField] private float itemBobMultiplier = 1.4f;
    [SerializeField] private float itemSmoothSpeed = 12.0f;

    private CharacterController controller;
    private PlayerInputReader input;
    private MineFirstPersonController movement;

    private Vector3 cameraStartPosition;
    private Vector3 itemStartPosition;
    private Quaternion itemStartRotation;
    private float bobTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<MineFirstPersonController>();

        if (cameraMotionRoot != null)
            cameraStartPosition = cameraMotionRoot.localPosition;

        if (heldItemRoot != null)
        {
            itemStartPosition = heldItemRoot.localPosition;
            itemStartRotation = heldItemRoot.localRotation;
        }
    }

    private void LateUpdate()
    {
        if (cameraMotionRoot == null || heldItemRoot == null)
            return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0.0f;

        bool moving = input.GameplayEnabled &&
                      controller.isGrounded &&
                      horizontalVelocity.sqrMagnitude > 0.04f;

        float xBob = 0.0f;
        float yBob = 0.0f;

        if (moving)
        {
            float frequency = input.SprintHeld
                ? sprintFrequency
                : walkFrequency;

            if (movement != null && movement.IsCrouching)
                frequency *= 0.75f;

            bobTime += Time.deltaTime * frequency;
            xBob = Mathf.Cos(bobTime * 0.5f) * horizontalAmplitude;
            yBob = Mathf.Abs(Mathf.Sin(bobTime)) * verticalAmplitude;
        }
        else
        {
            bobTime = 0.0f;
        }

        Vector3 cameraTarget = cameraStartPosition +
                               new Vector3(xBob, yBob, 0.0f);

        cameraMotionRoot.localPosition = Vector3.Lerp(
            cameraMotionRoot.localPosition,
            cameraTarget,
            1.0f - Mathf.Exp(-returnSpeed * Time.deltaTime)
        );

        Vector2 look = input.GameplayEnabled ? input.Look : Vector2.zero;
        Vector2 move = input.GameplayEnabled ? input.Move : Vector2.zero;

        Vector3 itemBob = new Vector3(
            xBob,
            -yBob,
            0.0f
        ) * itemBobMultiplier;

        Vector3 itemPositionTarget = itemStartPosition + itemBob;
        Quaternion itemRotationTarget = itemStartRotation * Quaternion.Euler(
            -look.y * lookSway * 0.05f + move.y * movementSway,
            look.x * lookSway * 0.05f,
            -move.x * movementSway
        );

        float itemLerp = 1.0f - Mathf.Exp(
            -itemSmoothSpeed * Time.deltaTime
        );

        heldItemRoot.localPosition = Vector3.Lerp(
            heldItemRoot.localPosition,
            itemPositionTarget,
            itemLerp
        );

        heldItemRoot.localRotation = Quaternion.Slerp(
            heldItemRoot.localRotation,
            itemRotationTarget,
            itemLerp
        );
    }
}
