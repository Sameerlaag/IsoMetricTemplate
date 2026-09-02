
using UnityEngine;

public class EquipmentUIController : MonoBehaviour
{
    
    private SuppliesInventory _equipmentInventory;

    private void Start()
    {
        _equipmentInventory = InventoryManager.Instance.PlayerInventory.Supplies;

        // _equipmentInventory.ItemAdded += AddItem;
        // _equipmentInventory.ItemChanged += UpdateItem;
        // _equipmentInventory.ItemRemoved += RemoveItem;
        //
        // // Useful if the UI is created after the inventory already
        // // contains items.
        // foreach (ItemInstance item in _equipmentInventory.Items)
        // {
        //     AddItem(item);
        // }
    }
}
