using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Input Timings")]
    [SerializeField] private float _holdThreshold = 0.4f;

    private EquipmentInventory _equipmentInventory;

    private float _keyHoldTimer;
    private bool _isHoldingKey;

    // Events for UI or external listeners
    public event Action<ItemInstance, int> EquipmentUsed; // (EquipmentInstance, SlotIndex)

    /// <summary>
    /// Binds the pure C# EquipmentInventory reference to this manager.
    /// </summary>
    public void Initialize(EquipmentInventory equipmentInventory)
    {
        _equipmentInventory = equipmentInventory;
    }

    /// <summary>
    /// Called every frame from PlayerController.Update() with the raw press state.
    /// </summary>
    /// <param name="isPressed">Whether the equipment key is held down in the current frame.</param>
    public void ProcessEquipmentInput(bool isPressed)
    {
        if (_equipmentInventory == null) return;

        if (isPressed)
        {
            if (!_isHoldingKey)
            {
                _isHoldingKey = true;
                _keyHoldTimer = 0f;
            }

            _keyHoldTimer += Time.deltaTime;
        }
        else if (_isHoldingKey)
        {
            // Key released: evaluate hold duration
            _isHoldingKey = false;

            // Hold (>= threshold) triggers Slot 1 (Index 1)
            // Tap (< threshold) triggers Slot 0 (Index 0)
            int targetSlot = (_keyHoldTimer >= _holdThreshold) ? 1 : 0;
            UseEquipmentInSlot(targetSlot);
        }
    }

    public void UseEquipmentInSlot(int slotIndex)
    {
        if (_equipmentInventory == null) return;

        ItemInstance equipment = _equipmentInventory.GetInSlot(slotIndex);
        if (equipment == null)
        {
            Debug.Log($"[EquipmentManager] No equipment equipped in slot {slotIndex + 1}.");
            return;
        }

        // Notify event listeners (e.g. UI or Equipment Execution Logic)
        EquipmentUsed?.Invoke(equipment, slotIndex);

        Debug.Log($"[EquipmentManager] Activated {equipment.Definition.DisplayName} in slot {slotIndex + 1}.");
    }
}