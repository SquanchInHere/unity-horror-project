using System;
using System.Collections;
using UnityEngine;

public class OpenObject : InteractableBase
{
    [Header("Moving Object")]
    [SerializeField] private Transform movingPart;

    [Header("Motion")]
    [SerializeField] private MotionType motionType = MotionType.Rotate;
    [SerializeField] private MotionAxis motionAxis = MotionAxis.Y;

    [SerializeField] private float openingDegree = 90f;

    [SerializeField] private float animationDuration = 0.8f;

    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Behavior")]
    [SerializeField] private bool canClose = true;
    [SerializeField] private bool canOpen = true;

    [Header("Prompt")]
    [SerializeField] private string openPrompt = "Open";
    [SerializeField] private string closePrompt = "Close";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private Vector3 closedLocalPosition;
    private Vector3 openedLocalPosition;

    private Quaternion closedLocalRotation;
    private Quaternion openedLocalRotation;

    private Coroutine animationCoroutine;

    private bool isOpen;
    private bool isAnimating;

    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;

    public event Action Opened;
    public event Action Closed;

    private void Awake()
    {
        if (movingPart == null)
        {
            enabled = false;
            return;
        }

        RememberPositions();
    }

    private void RememberPositions()
    {
        Vector3 axis = GetAxis();

        closedLocalPosition = movingPart.localPosition;
        closedLocalRotation = movingPart.localRotation;

        openedLocalPosition =
            closedLocalPosition + axis * openingDegree;

        openedLocalRotation =
            closedLocalRotation *
            Quaternion.AngleAxis(openingDegree, axis);
    }

    private Vector3 GetAxis()
    {
        return motionAxis switch
        {
            MotionAxis.X => Vector3.right,
            MotionAxis.Y => Vector3.up,
            MotionAxis.Z => Vector3.forward,
            _ => Vector3.up
        };
    }

    public override string GetPrompt(PlayerInteractor interactor)
    {
        if (isAnimating)
            return "Wait";

        if (!isOpen)
            return openPrompt;

        return canClose ? closePrompt : "";
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        return base.CanInteract(interactor) &&
               !isAnimating &&
               (isOpen ? canClose : canOpen);
    }

    public override void Interact(PlayerInteractor interactor)
    {
        TrySetOpen(!isOpen);
    }

    public bool Open()
    {
        return TrySetOpen(true);
    }

    public bool Close()
    {
        return TrySetOpen(false);
    }

    private bool TrySetOpen(bool shouldOpen)
    {
        if (isAnimating || shouldOpen == isOpen)
            return false;

        if (shouldOpen && !canOpen)
            return false;

        if (!shouldOpen && !canClose)
            return false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(
            AnimateObject(shouldOpen)
        );

        return true;
    }

    private IEnumerator AnimateObject(bool shouldOpen)
    {
        isAnimating = true;

        Vector3 startPosition = movingPart.localPosition;
        Quaternion startRotation = movingPart.localRotation;

        Vector3 targetPosition = shouldOpen
            ? openedLocalPosition
            : closedLocalPosition;

        Quaternion targetRotation = shouldOpen
            ? openedLocalRotation
            : closedLocalRotation;

        PlaySound(shouldOpen ? openSound : closeSound);

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            float curvedProgress =
                animationCurve.Evaluate(progress);

            if (motionType == MotionType.Rotate)
            {
                movingPart.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curvedProgress
                );
            }
            else
            {
                movingPart.localPosition = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedProgress
                );
            }

            yield return null;
        }

        if (motionType == MotionType.Rotate)
            movingPart.localRotation = targetRotation;
        else
            movingPart.localPosition = targetPosition;

        isOpen = shouldOpen;
        isAnimating = false;
        animationCoroutine = null;

        if (isOpen)
            Opened?.Invoke();
        else
            Closed?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
