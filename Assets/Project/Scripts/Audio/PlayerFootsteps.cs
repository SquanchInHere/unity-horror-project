using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PlayerInputReader input;

    [Header("Surface clips")]
    [SerializeField] private AudioClip[] stoneClips;
    [SerializeField] private AudioClip[] waterClips;

    [Header("Step distance")]
    [SerializeField] private float walkingStepDistance = 2.0f;
    [SerializeField] private float sprintStepDistance = 1.45f;
    [SerializeField] private float crouchingStepDistance = 2.4f;
    [SerializeField] private float minimumMovementSpeed = 0.15f;

    private CharacterController controller;
    private MineFirstPersonController movement;
    private float travelledDistance;
    private int previousClipIndex = -1;
    private int waterZonesCount;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<MineFirstPersonController>();

        if (input == null)
            input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (!controller.isGrounded)
            return;

        AudioClip[] activeClips = waterZonesCount > 0
            ? waterClips
            : stoneClips;

        if (activeClips == null || activeClips.Length == 0)
            return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0.0f;

        float speed = horizontalVelocity.magnitude;

        if (speed < minimumMovementSpeed)
        {
            travelledDistance = 0.0f;
            return;
        }

        travelledDistance += speed * Time.deltaTime;

        float requiredDistance;

        if (movement != null && movement.IsCrouching)
            requiredDistance = crouchingStepDistance;
        else if (input != null && input.SprintHeld)
            requiredDistance = sprintStepDistance;
        else
            requiredDistance = walkingStepDistance;

        if (travelledDistance < requiredDistance)
            return;

        travelledDistance = 0.0f;
        PlayRandomStep(activeClips);
    }

    public void EnterWaterZone()
    {
        waterZonesCount++;
    }

    public void ExitWaterZone()
    {
        waterZonesCount = Mathf.Max(0, waterZonesCount - 1);
    }

    private void PlayRandomStep(AudioClip[] activeClips)
    {
        int index = Random.Range(0, activeClips.Length);

        if (activeClips.Length > 1 && index == previousClipIndex)
            index = (index + 1) % activeClips.Length;

        previousClipIndex = index;
        audioSource.pitch = Random.Range(0.94f, 1.06f);
        audioSource.PlayOneShot(activeClips[index]);
    }
}
