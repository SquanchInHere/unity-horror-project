using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [SerializeField] private string defaultPrompt = "Interact";

    public virtual bool CanInteract(PlayerInteractor interactor)
    {
        return enabled && gameObject.activeInHierarchy;
    }

    public virtual string GetPrompt(PlayerInteractor interactor)
    {
        return defaultPrompt;
    }

    public abstract void Interact(PlayerInteractor interactor);
}
