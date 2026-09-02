using TMPro;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text promptText;

    [Header("Raycast")]
    [SerializeField] private float interactionDistance = 3.0f;
    [SerializeField] private LayerMask interactionMask;

    private PlayerInputReader input;
    private InteractableBase currentInteractable;

    public PlayerInventory Inventory { get; private set; }

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        Inventory = GetComponent<PlayerInventory>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (!input.GameplayEnabled)
        {
            currentInteractable = null;
            HidePrompt();
            return;
        }

        FindInteractable();

        if (currentInteractable != null && input.ConsumeInteract())
            currentInteractable.Interact(this);
    }

    private void FindInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionMask,
                QueryTriggerInteraction.Collide))
        {
            currentInteractable = hit.collider.GetComponentInParent<InteractableBase>();
        }

        if (currentInteractable == null)
        {
            HidePrompt();
            return;
        }

        if (promptText != null)
        {
            promptText.text = $"[E] {currentInteractable.GetPrompt(this)}";
            promptText.gameObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }
}
