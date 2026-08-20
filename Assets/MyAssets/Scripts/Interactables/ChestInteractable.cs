using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float _holdTime = 2.0f; // Requires 2 seconds hold to open
    private bool _isOpened;

    public string InteractionPrompt => _isOpened ? "Chest is empty" : $"Hold [E] to open ({_holdTime}s)";
    public bool RequiresHold => !_isOpened;
    public float HoldDuration => _holdTime;

    public void OnHoldStarted(GameObject interactor)
    {
        Debug.Log("Started unlocking chest...");
    }

    public void OnHoldProgress(float progress)
    {
        // progress flows smoothly from 0.0 to 1.0
        Debug.Log($"Unlocking Chest... {Mathf.RoundToInt(progress * 100)}%");
    }

    public void OnHoldCanceled(GameObject interactor)
    {
        Debug.Log("Chest unlocking interrupted!");
    }

    public void Interact(GameObject interactor)
    {
        _isOpened = true;
        Debug.Log("Chest opened! Loot dropped.");
    }
}