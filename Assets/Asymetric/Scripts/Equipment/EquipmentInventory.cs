using System;
using System.Collections.Generic;

public class EquipmentInventory
{
    private readonly int _capacity;
    private readonly List<ItemInstance> _items = new();

    public IReadOnlyList<ItemInstance> Items => _items;
    public int Capacity => _capacity;
    public bool IsFull => _items.Count >= _capacity;

    // Events
    public event Action<ItemInstance, int> EquipmentEquipped;   // (Item, SlotIndex)
    public event Action<ItemInstance, int> EquipmentUnequipped; // (Item, SlotIndex)

    public EquipmentInventory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public bool TryEquip(ItemInstance item)
    {
        if (item == null || IsFull || _items.Contains(item))
            return false;

        _items.Add(item);
        int slotIndex = _items.Count - 1;

        EquipmentEquipped?.Invoke(item, slotIndex);
        return true;
    }

    public bool TryUnequip(ItemInstance item)
    {
        if (item == null || !_items.Contains(item))
            return false;

        int removedIndex = _items.IndexOf(item);
        _items.RemoveAt(removedIndex);

        EquipmentUnequipped?.Invoke(item, removedIndex);
        return true;
    }

    public ItemInstance GetInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _items.Count)
            return _items[slotIndex];

        return null;
    }
}