using System;

public class ItemInstance
{
    public ItemDefinition Definition { get; protected set; }

    public int Count { get; private set; }

    public string ID => Definition.ID;
    public string DisplayName => Definition.DisplayName;

    public ItemType ItemType => Definition.ItemType;
    public int MaxStack => Definition.MaxStack;

    public bool CanStack => MaxStack > 1;

    public ItemInstance(ItemDefinition definition, int count)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        Definition = definition;
        Count = count;
    }

    public static ItemInstance CreateInstance(ItemDefinition definition, int count)
    {
        if (definition is GunItem gunItem)
        {
            return new WeaponInstance(gunItem, count);
        }
        return new ItemInstance(definition, count);
    }

    public ItemInstance CreateCopy(int count)
    {
        return CreateInstance(Definition, count);
    }

    public int Add(int amount)
    {
        if (amount <= 0)
            return 0;

        int available = MaxStack - Count;

        if (available <= 0)
            return 0;

        int added = Math.Min(amount, available);

        Count += added;

        return added;
    }

    public int Remove(int amount)
    {
        if (amount <= 0)
            return 0;

        int removed = Math.Min(amount, Count);

        Count -= removed;

        return removed;
    }
}