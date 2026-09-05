using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerSurvival))]
public class MineFirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.5f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravity = -22.0f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float maximumLookAngle = 85.0f;

    [Header("Crouch")]
    [SerializeField] private float crouchingHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 7.0f;
    [SerializeField] private LayerMask ceilingMask;

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    private CharacterController controller;
    private PlayerInputReader input;
    private PlayerSurvival survival;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 crouchingCenter;

    private float standingCameraY;
    private float crouchingCameraY;

    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        survival = GetComponent<PlayerSurvival>();

        if (cameraPivot == null)
        {
            Debug.LogError(
                "MineFirstPersonController: Camera Pivot is not assigned.",
                this
            );
            enabled = false;
            return;
        }

        standingHeight = controller.height;
        standingCenter = controller.center;

        crouchingHeight = Mathf.Clamp(
            crouchingHeight,
            controller.radius * 2.0f,
            standingHeight
        );

        crouchingCenter = standingCenter;
        crouchingCenter.y -= (standingHeight - crouchingHeight) * 0.5f;

        standingCameraY = cameraPivot.localPosition.y;
        crouchingCameraY = standingCameraY - (standingHeight - crouchingHeight);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!input.GameplayEnabled)
        {
            IsSprinting = false;
            survival.SetSprinting(false);
            return;
        }

        UpdateCrouch();
        UpdateMovement();
        UpdateLook();
    }

    private void UpdateCrouch()
    {
        bool wantsToCrouch = input.CrouchHeld;

        if (!wantsToCrouch && controller.height < standingHeight - 0.01f)
        {
            if (!CanStandUp())
                wantsToCrouch = true;
        }

        IsCrouching = wantsToCrouch;

        float targetHeight = IsCrouching ? crouchingHeight : standingHeight;
        Vector3 targetCenter = IsCrouching ? crouchingCenter : standingCenter;
        float targetCameraY = IsCrouching ? crouchingCameraY : standingCameraY;

        controller.height = Mathf.MoveTowards(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        controller.center = Vector3.MoveTowards(
            controller.center,
            targetCenter,
            crouchTransitionSpeed * Time.deltaTime
        );

        Vector3 cameraPosition = cameraPivot.localPosition;
        cameraPosition.y = Mathf.MoveTowards(
            cameraPosition.y,
            targetCameraY,
            crouchTransitionSpeed * Time.deltaTime
        );
        cameraPivot.localPosition = cameraPosition;
    }

    private bool CanStandUp()
    {
        float distanceToStandingHeight = standingHeight - controller.height;

        if (distanceToStandingHeight <= 0.01f)
            return true;

        float castRadius = Mathf.Max(0.01f, controller.radius - 0.03f);
        Vector3 currentCenter = transform.TransformPoint(controller.center);

        Vector3 currentHeadSphereCenter =
            currentCenter + transform.up * (controller.height * 0.5f - castRadius);

        bool ceilingFound = Physics.SphereCast(
            currentHeadSphereCenter,
            castRadius,
            transform.up,
            out _,
            distanceToStandingHeight,
            ceilingMask,
            QueryTriggerInteraction.Ignore
        );

        return !ceilingFound;
    }

    private void UpdateMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0.0f)
            verticalVelocity = -2.0f;

        Vector2 moveInput = input.Move;
        Vector3 horizontalDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        if (horizontalDirection.sqrMagnitude > 1.0f)
            horizontalDirection.Normalize();

        bool hasMovementInput = horizontalDirection.sqrMagnitude > 0.01f;
        IsSprinting = !IsCrouching &&
                      hasMovementInput &&
                      input.SprintHeld &&
                      survival.CanSprint;

        survival.SetSprinting(IsSprinting);

        float currentSpeed;

        if (IsCrouching)
            currentSpeed = crouchSpeed;
        else if (IsSprinting)
            currentSpeed = sprintSpeed;
        else
            currentSpeed = walkSpeed;

        currentSpeed *= survival.MovementSpeedMultiplier;

        if (input.ConsumeJump() && controller.isGrounded && !IsCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalDirection * currentSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateLook()
    {
        Vector2 lookInput = input.Look;

        transform.Rotate(
            Vector3.up,
            lookInput.x * mouseSensitivity,
            Space.Self
        );

        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maximumLookAngle,
            maximumLookAngle
        );

        cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0.0f, 0.0f);
    }
}
