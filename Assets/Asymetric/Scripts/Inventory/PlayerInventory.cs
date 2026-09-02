using System.Linq;

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

    public int TryAddItem(ItemInstance item)
    {
        if (item == null) return 0;

        switch (item.ItemType)
        {
            case ItemType.DOCUMENT:
                Documents.Add(item);
                return item.Count;
            case ItemType.KEY:
                KeyItems.Add(item.Definition);
                return item.Count;
            default:
                return Supplies.TryAdd(item);
        }
    }

    public bool TryEquipWeapon(ItemInstance item)
    {
        if (item is not WeaponInstance weapon) return false;
        if (Weapons.IsFull) return false;

        if (Supplies.Items.Contains(item))
        {
            bool isEquipped = Weapons.TryEquip(weapon);

            if (!isEquipped) return false;

            Supplies.Remove(item);

            return true;
        }

        return false;
    }

    public bool TryUnequipWeapon(ItemInstance item)
    {
        if (item is not WeaponInstance weapon) return false;
        if (Supplies.IsFull) return false;

        if (Weapons.TryUnequip(weapon))
        {
            Supplies.TryAdd(weapon);
            return true;
        }

        return false;
    }

    public bool TryEquipEquipment(ItemInstance equipment)
    {
        if (equipment == null || equipment.ItemType != ItemType.EQUIPABLE) return false;
        if (Equipment.IsFull) return false;

        if (Supplies.Items.Contains(equipment))
        {
            Supplies.Remove(equipment);
        }

        return Equipment.TryEquip(equipment);
    }

    public bool TryUnequipEquipment(ItemInstance equipment)
    {
        if (equipment == null || !Equipment.Items.Contains(equipment)) return false;
        if (Supplies.IsFull) return false;

        if (Equipment.TryUnequip(equipment))
        {
            Supplies.TryAdd(equipment);
            return true;
        }

        return false;
    }
}