public class PlayerInventory
{
    public SuppliesInventory Supplies { get; }
    public KeyItemCollection KeyItems { get; }
    public DocumentCollection Documents { get; }

    public PlayerInventory(int suppliesCapacity, bool allowMultipleStacks)
    {
        Supplies = new SuppliesInventory(suppliesCapacity, allowMultipleStacks);
        KeyItems = new KeyItemCollection();
        Documents = new DocumentCollection();
    }
}