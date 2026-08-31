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

    private bool jumpPressed;
    private bool interactPressed;
    private bool inventoryPressed;
    private int requestedHotbarIndex = -1;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        crouchAction = playerInput.actions.FindAction("Crouch", false);

        if (crouchAction == null)
        {
            Debug.LogError(
                "В InputSystem_Actions не найдено действие Crouch в активной карте Player.",
                this
            );
        }
    }

    private void Update()
    {
        CrouchHeld = GameplayEnabled &&
                      crouchAction != null &&
                      crouchAction.IsPressed();
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
