public interface IInventoryItem
{
    ItemType ItemType { get; set; }

    string ID { get; set; }
    string Name { get; set; }
    
    bool CanStack { get; set; }
    int ItemLimit { get; set; }
    int ItemCount { get; set; }

    void UseItem();
    bool CanUseItem { get; set; }

    void RemoveItem();
    bool CanRemoveItem { get; set; }

    void MoveItem();

    int InventoryPosition { get; set; }

    string AddressableInventoryReference { get; set; }

    string AddressableGameObjectReference { get; set; }
}