using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    private readonly int _capacity;
    private readonly List<ItemInstance> _items = new();

    public IReadOnlyList<ItemInstance> Items => _items;

    public bool IsFull => _items.Count >= _capacity;

    public Inventory(int capacity)
    {
        _capacity = capacity;
    }

    public int TryAdd(ItemInstance item)
    {
        if (item == null || item.Count <= 0)
        {
            Debug.Log($"Item count is 0 or null");
            return 0;
        }

        int originalCount = item.Count;

        AddToExistingStack(item);

        Debug.Log($"Still holding {item.Count} vs {originalCount} items");

        if (item.Count > 0 && !IsFull)
        {
            Debug.Log($"Adding items");
            _items.Add(item);
        }

        return originalCount - item.Count;
    }

    private void AddToExistingStack(ItemInstance item)
    {
        var stack = FindAvailableStack(item);

        if (stack == null)
        {
            Debug.Log($"Stack is null");
            return;
        }

        int transferred = stack.Add(item.Count);
        item.Remove(transferred);
        Debug.Log($"Updated stack with {transferred} items");

    }

    private ItemInstance FindAvailableStack(ItemInstance item)
    {
        return _items.Find(existing =>
            existing.ID == item.ID &&
            existing.CanStack &&
            existing.Count < existing.MaxStack);
    }
}