using System.Collections.Generic;

public class KeyItemCollection
{
    private readonly HashSet<ItemDefinition> _keys = new();

    public IReadOnlyCollection<ItemDefinition> Keys => _keys;

    public bool Add(ItemDefinition key)
    {
        return _keys.Add(key);
    }

    public bool Has(ItemDefinition key)
    {
        return _keys.Contains(key);
    }

    public bool Remove(ItemDefinition key)
    {
        return _keys.Remove(key);
    }
}