using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool GameplayEnabled { get; private set; } = true;

    private PlayerInput playerInput;
    private InputAction crouchAction;
    private InputAction hotbar1Action;
    private InputAction hotbar2Action;
    private InputAction hotbar3Action;
    private InputAction sprintAction;

    private bool jumpPressed;
    private bool interactPressed;
    private bool inventoryPressed;
    private int requestedHotbarIndex = -1;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        crouchAction = playerInput.actions.FindAction("Crouch", false);
        hotbar1Action = playerInput.actions.FindAction("Hotbar1", false);
        hotbar2Action = playerInput.actions.FindAction("Hotbar2", false);
        hotbar3Action = playerInput.actions.FindAction("Hotbar3", false);
        sprintAction = playerInput.actions.FindAction("Sprint", false);
    }

    private void Update()
    {
        CrouchHeld = GameplayEnabled && crouchAction != null && crouchAction.IsPressed();
        SprintHeld = GameplayEnabled && sprintAction != null && sprintAction.IsPressed();

        if (!GameplayEnabled)
            return;

        Keyboard keyboard = Keyboard.current;

        if ((hotbar1Action != null && hotbar1Action.WasPressedThisFrame()) ||
            (keyboard != null &&
             (keyboard.digit1Key.wasPressedThisFrame ||
              keyboard.numpad1Key.wasPressedThisFrame)))
        {
            requestedHotbarIndex = 0;
        }
        else if ((hotbar2Action != null && hotbar2Action.WasPressedThisFrame()) ||
                 (keyboard != null &&
                  (keyboard.digit2Key.wasPressedThisFrame ||
                   keyboard.numpad2Key.wasPressedThisFrame)))
        {
            requestedHotbarIndex = 1;
        }
        else if ((hotbar3Action != null && hotbar3Action.WasPressedThisFrame()) ||
                 (keyboard != null &&
                  (keyboard.digit3Key.wasPressedThisFrame ||
                   keyboard.numpad3Key.wasPressedThisFrame)))
        {
            requestedHotbarIndex = 2;
        }
    }

    public void OnMove(InputValue value)
    {
        Move = GameplayEnabled ? value.Get<Vector2>() : Vector2.zero;
    }

    public void OnLook(InputValue value)
    {
        Look = GameplayEnabled ? value.Get<Vector2>() : Vector2.zero;
    }

    public void OnJump(InputValue value)
    {
        if (GameplayEnabled && value.isPressed)
            jumpPressed = true;
    }

    public void OnSprint(InputValue value)
    {
        SprintHeld = GameplayEnabled && value.isPressed;
    }

    public void OnCrouch(InputValue value)
    {
        CrouchHeld = GameplayEnabled && value.isPressed;
    }

    public void OnInteract(InputValue value)
    {
        if (GameplayEnabled && value.isPressed)
            interactPressed = true;
    }

    public void OnInventory(InputValue value)
    {
        if (value.isPressed)
            inventoryPressed = true;
    }

    public void OnHotbar1(InputValue value)
    {
        if (GameplayEnabled && value.isPressed)
            requestedHotbarIndex = 0;
    }

    public void OnHotbar2(InputValue value)
    {
        if (GameplayEnabled && value.isPressed)
            requestedHotbarIndex = 1;
    }

    public void OnHotbar3(InputValue value)
    {
        if (GameplayEnabled && value.isPressed)
            requestedHotbarIndex = 2;
    }

    public bool ConsumeJump()
    {
        bool result = jumpPressed;
        jumpPressed = false;
        return result;
    }

    public bool ConsumeInteract()
    {
        bool result = interactPressed;
        interactPressed = false;
        return result;
    }

    public bool ConsumeInventory()
    {
        bool result = inventoryPressed;
        inventoryPressed = false;
        return result;
    }

    public int ConsumeHotbarIndex()
    {
        int result = requestedHotbarIndex;
        requestedHotbarIndex = -1;
        return result;
    }

    public void SetGameplayEnabled(bool enabled)
    {
        GameplayEnabled = enabled;

        if (!enabled)
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            SprintHeld = false;
            CrouchHeld = false;
            jumpPressed = false;
            interactPressed = false;
            requestedHotbarIndex = -1;
        }
    }
}
