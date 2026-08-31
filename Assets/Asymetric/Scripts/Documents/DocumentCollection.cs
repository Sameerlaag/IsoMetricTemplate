using System.Collections.Generic;

public class DocumentCollection
{
    private readonly HashSet<ItemInstance> _documents = new();

    public IReadOnlyCollection<ItemInstance> Documents => _documents;

    public void Add(ItemInstance document)
    {
        _documents.Add(document);
    }

    public bool Contains(ItemInstance document)
    {
        return _documents.Contains(document);
    }
}