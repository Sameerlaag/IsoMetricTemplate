using System.Collections.Generic;
using UnityEngine;

public class EquipmentUIController : MonoBehaviour
{
    private EquipmentInventory _equipmentInventory;

    private void Start()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null)
        {
            _equipmentInventory = InventoryManager.Instance.PlayerInventory.Equipment;
            _equipmentInventory.EquipmentEquipped += HandleEquipmentEquipped;
            _equipmentInventory.EquipmentUnequipped += HandleEquipmentUnequipped;

            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (_equipmentInventory != null)
        {
            _equipmentInventory.EquipmentEquipped -= HandleEquipmentEquipped;
            _equipmentInventory.EquipmentUnequipped -= HandleEquipmentUnequipped;
        }
    }

    private void HandleEquipmentEquipped(ItemInstance item, int slotIndex)
    {
        RefreshUI();
    }

    private void HandleEquipmentUnequipped(ItemInstance item, int slotIndex)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_equipmentInventory == null) return;

        IReadOnlyList<ItemInstance> items = _equipmentInventory.Items;
        // Update equipment slot views if slots exist
    }
}
