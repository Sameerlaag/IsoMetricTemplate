using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    
    // Hold Mechanics Configuration
    bool RequiresHold => false;
    float HoldDuration => 0f; // Time in seconds

    // Triggered when interaction successfully completes (instant or hold finish)
    void Interact(GameObject interactor);

    // Optional callbacks for hold progress UI feedback
    void OnHoldStarted(GameObject interactor) { }
    void OnHoldProgress(float progress) { } // progress values: 0.0f to 1.0f
    void OnHoldCanceled(GameObject interactor) { }

    Vector3 InteractionPosition => (this as MonoBehaviour) != null ? ((MonoBehaviour)this).transform.position : Vector3.zero;
}