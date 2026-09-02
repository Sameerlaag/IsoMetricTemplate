using System;
using System.Collections.Generic;

public class WeaponsInventory
{
    private readonly int _capacity;
    private readonly List<ItemInstance> _weapons = new();
    private int _activeSlotIndex = -1;

    public IReadOnlyList<ItemInstance> Weapons => _weapons;
    public int Capacity => _capacity;
    public bool IsFull => _weapons.Count >= _capacity;
    public int ActiveSlotIndex => _activeSlotIndex;

    public ItemInstance ActiveWeapon => 
        (_activeSlotIndex >= 0 && _activeSlotIndex < _weapons.Count) ? _weapons[_activeSlotIndex] : null;

    // Events
    public event Action<ItemInstance, int> WeaponEquipped;    // (Weapon, SlotIndex)
    public event Action<ItemInstance, int> WeaponUnequipped;  // (Weapon, SlotIndex)
    public event Action<ItemInstance, int> ActiveWeaponChanged; // (Weapon, SlotIndex)

    public WeaponsInventory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public bool TryEquip(ItemInstance weapon)
    {
        if (weapon == null || IsFull || _weapons.Contains(weapon))
            return false;

        _weapons.Add(weapon);
        int slotIndex = _weapons.Count - 1;

        WeaponEquipped?.Invoke(weapon, slotIndex);

        // Auto-select first added weapon
        if (_weapons.Count == 1)
        {
            SelectSlot(0);
        }

        return true;
    }

    public bool TryUnequip(ItemInstance weapon)
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