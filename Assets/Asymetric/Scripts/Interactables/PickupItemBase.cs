using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ItemDefinition _definition;

    [SerializeField]
    private int _count;

    private ItemInstance _item;

    private void Awake()
    {
        _item = new ItemInstance(_definition, _count);
    }

    public string InteractionPrompt { get; }

    public void Interact(GameObject interactor)
    {
        int pickedUp = InventoryManager.Instance
            .PlayerInventory.Supplies
            .TryAdd(_item);
        
        Debug.Log($"player picked up {pickedUp} of this item");

        if (pickedUp <= 0)
        {
            // Failed feedback
            return;
        }

        if (_item.Count <= 0)
            Destroy(gameObject);
        else
            UpdateVisualCount();
        
        Debug.Log($"{_item.Count} remain");

    }


    private void UpdateVisualCount()
    {
        // Update world pickup UI
    }
}