using System;

public class ItemInstance
{
    public ItemDefinition Definition { get; }

    public int Count { get; private set; }

    public string ID => Definition.ID;
    public ItemType ItemType => Definition.ItemType;
    public int MaxStack => Definition.MaxStack;

    public bool CanStack => MaxStack > 1;

    public ItemInstance(ItemDefinition definition, int count)
    {
        Definition = definition;
        Count = count;
    }

    public int Add(int amount)
    {
        if (!CanStack)
            return 0;

        int available = MaxStack - Count;
        int added = Math.Min(amount, available);

        Count += added;

        return added;
    }

    public int Remove(int amount)
    {
        int removed = Math.Min(amount, Count);

        Count -= removed;

        return removed;
    }
}