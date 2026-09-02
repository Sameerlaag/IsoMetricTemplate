public class PlayerInventory
{
    public SuppliesInventory Supplies { get; }
    public WeaponsInventory Weapons { get; }
    public EquipmentInventory Equipment { get; }
    public KeyItemCollection KeyItems { get; }
    public DocumentCollection Documents { get; }

    public PlayerInventory(
        int suppliesCapacity,
        int weaponCapacity,
        int equipmentCapacity,
        bool allowMultipleStacks)
    {
        Supplies = new SuppliesInventory(suppliesCapacity, allowMultipleStacks);
        Weapons = new WeaponsInventory(weaponCapacity);
        Equipment = new EquipmentInventory(equipmentCapacity);
        KeyItems = new KeyItemCollection();
        Documents = new DocumentCollection();
    }
}