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
    public HotbarController Hotbar { get; private set; }

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        Inventory = GetComponent<PlayerInventory>();
        Hotbar = GetComponent<HotbarController>();

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


        if (currentInteractable == null ||
            !currentInteractable.CanInteract(this) ||
            !input.ConsumeInteract())
            return;

        currentInteractable.Interact(this);

        FindInteractable();
    }

    public void RefreshPrompt()
    {
        FindInteractable();
    }


    private void FindInteractable()
    {
        currentInteractable = null;

        if(playerCamera == null)
        {
            HidePrompt();
            return;
        }

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

        if (currentInteractable == null ||
            !currentInteractable.CanInteract(this))
        {
            currentInteractable = null;
            HidePrompt();
            return;
        }

        string prompt = currentInteractable.GetPrompt(this);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            currentInteractable = null;
            HidePrompt();
            return;
        }

        if (promptText != null)
        {
            promptText.text = $"[E] {prompt}";
            promptText.gameObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptText == null)
            return;

        promptText.text = string.Empty;
        promptText.gameObject.SetActive(false);
    }
}
