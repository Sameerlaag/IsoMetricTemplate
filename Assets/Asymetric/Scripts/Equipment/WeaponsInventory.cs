using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsInventory
{
    private readonly int _capacity;
    private readonly List<WeaponInstance> _weapons = new();
    private int _activeSlotIndex = -1;

    public IReadOnlyList<WeaponInstance> Weapons => _weapons;
    public int Capacity => _capacity;
    public bool IsFull => _weapons.Count >= _capacity;
    public int ActiveSlotIndex => _activeSlotIndex;

    public WeaponInstance ActiveWeapon => 
        (_activeSlotIndex >= 0 && _activeSlotIndex < _weapons.Count) ? _weapons[_activeSlotIndex] : null;

    // Events
    public event Action<WeaponInstance, int> WeaponEquipped;    // (Weapon, SlotIndex)
    public event Action<WeaponInstance, int> WeaponUnequipped;  // (Weapon, SlotIndex)
    public event Action<WeaponInstance, int> ActiveWeaponChanged; // (Weapon, SlotIndex)

    public WeaponsInventory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public bool TryEquip(WeaponInstance weapon)
    {
        Debug.Log($"[TryEquip] Attempting to equip: {weapon}");

        if (weapon == null)
        {
            Debug.LogWarning("[TryEquip] Failed: weapon is null.");
            return false;
        }

        if (IsFull)
        {
            Debug.LogWarning($"[TryEquip] Failed: weapon inventory is full. Count: {_weapons.Count}");
            return false;
        }

        if (_weapons.Contains(weapon))
        {
            Debug.LogWarning("[TryEquip] Failed: weapon is already in the inventory.");
            return false;
        }

        Debug.Log("[TryEquip] All checks passed. Adding weapon...");

        _weapons.Add(weapon);

        int slotIndex = _weapons.Count - 1;

        Debug.Log($"[TryEquip] Weapon added successfully. Slot index: {slotIndex}, Total weapons: {_weapons.Count}");

        WeaponEquipped?.Invoke(weapon, slotIndex);

        Debug.Log("[TryEquip] WeaponEquipped event invoked.");

        // Auto-select first added weapon
        if (_weapons.Count == 1)
        {
            Debug.Log("[TryEquip] First weapon added. Selecting slot 0.");
            SelectSlot(0);
        }

        Debug.Log("[TryEquip] Equip successful.");

        return true;
    }


    public bool TryUnequip(WeaponInstance weapon)
    {
        if (weapon == null || !_weapons.Contains(weapon))
            return false;

        int removedIndex = _weapons.IndexOf(weapon);
        _weapons.RemoveAt(removedIndex);

        WeaponUnequipped?.Invoke(weapon, removedIndex);

        if (_weapons.Count == 0)
        {
            _activeSlotIndex = -1;
            ActiveWeaponChanged?.Invoke(null, -1);
        }
        else
        {
            int nextIndex = Math.Clamp(_activeSlotIndex, 0, _weapons.Count - 1);
            _activeSlotIndex = -1; // Reset to force change notification in SelectSlot
            SelectSlot(nextIndex);
        }

        return true;
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _weapons.Count)
            return;

        if (_activeSlotIndex == slotIndex)
            return;

        _activeSlotIndex = slotIndex;
        ActiveWeaponChanged?.Invoke(ActiveWeapon, _activeSlotIndex);
    }

    public void CycleNext()
    {
        if (_weapons.Count <= 1) return;
        int nextSlot = (_activeSlotIndex + 1) % _weapons.Count;
        SelectSlot(nextSlot);
    }
}