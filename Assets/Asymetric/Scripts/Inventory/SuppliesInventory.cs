using System;
using System.Collections.Generic;

public class SuppliesInventory
{
    private readonly int _capacity;
    private readonly bool _allowMultipleStacks;

    private readonly List<ItemInstance> _items = new();

    public event Action<ItemInstance> ItemAdded;
    public event Action<ItemInstance> ItemChanged;
    public event Action<ItemInstance> ItemRemoved;

    public IReadOnlyList<ItemInstance> Items => _items;

    public int Capacity => _capacity;

    public bool IsFull => _items.Count >= _capacity;

    public SuppliesInventory(int capacity, bool allowMultipleStacks)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _allowMultipleStacks = allowMultipleStacks;
    }

    public int TryAdd(ItemInstance item)
    {
        if (item == null || item.Count <= 0)
            return 0;

        int originalCount = item.Count;

        AddToExistingStacks(item);

        if (item.Count > 0 &&
            !IsFull &&
            (_allowMultipleStacks || !HasItem(item)))
        {
            AddAsNewItem(item);
        }

        return originalCount - item.Count;
    }

    private void AddToExistingStacks(ItemInstance item)
    {
        while (item.Count > 0)
        {
            ItemInstance stack = FindAvailableStack(item);

            if (stack == null)
                return;

            int transferred = stack.Add(item.Count);

            if (transferred <= 0)
                return;

            item.Remove(transferred);

            ItemChanged?.Invoke(stack);

            // If multiple stacks aren't allowed, we've filled
            // the one permitted stack and must stop here.
            if (!_allowMultipleStacks)
                return;
        }
    }

    private void AddAsNewItem(ItemInstance item)
    {
        ItemInstance inventoryItem = item.CreateCopy(item.Count);

        _items.Add(inventoryItem);

        // The inventory now owns the copied instance.
        // The pickup retains its own instance.
        item.Remove(item.Count);

        ItemAdded?.Invoke(inventoryItem);
    }

    public bool Remove(ItemInstance item)
    {
        if (item == null)
            return false;

        if (!_items.Remove(item))
            return false;

        ItemRemoved?.Invoke(item);

        return true;
    }

    public int Remove(ItemInstance item, int amount)
    {
        if (item == null || amount <= 0)
            return 0;

        if (!_items.Contains(item))
            return 0;

        int removed = item.Remove(amount);

        if (item.Count <= 0)
        {
            _items.Remove(item);
            ItemRemoved?.Invoke(item);
        }
        else if (removed > 0)
        {
            ItemChanged?.Invoke(item);
        }

        return removed;
    }

    private ItemInstance FindAvailableStack(ItemInstance item)
    {
        return _items.Find(existing =>
            existing.ID == item.ID &&
            existing.CanStack &&
            existing.Count < existing.MaxStack);
    }

    private bool HasItem(ItemInstance item)
    {
        return _items.Exists(existing => existing.ID == item.ID);
    }
}