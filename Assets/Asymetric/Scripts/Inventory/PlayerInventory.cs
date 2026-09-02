using System.Linq;
using UnityEngine;

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
        if (item == null) return false;
        if (Weapons.IsFull) return false;

        WeaponInstance weapon = item as WeaponInstance;
        if (weapon == null && item.Definition is GunItem gunItem)
        {
            weapon = new WeaponInstance(gunItem, item.Count);
        }

        if (weapon == null) return false;

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

    public int GetTotalAmmoCount(AmmoType ammoType)
    {
        int total = 0;
        foreach (var item in Supplies.Items)
        {
            if (item is AmmoInstance ammoInst && ammoInst.AmmoType == ammoType)
            {
                total += ammoInst.Count;
            }
            else if (item.Definition is AmmoItem ammoItem && ammoItem.ammoType == ammoType)
            {
                total += item.Count;
            }
        }
        return total;
    }

    public int ConsumeAmmo(AmmoType ammoType, int amountNeeded)
    {
        if (amountNeeded <= 0) return 0;

        int amountRemaining = amountNeeded;
        var matchingItems = Supplies.Items
            .Where(item => (item is AmmoInstance ammoInst && ammoInst.AmmoType == ammoType) ||
                           (item.Definition is AmmoItem ammoItem && ammoItem.ammoType == ammoType))
            .ToList();

        foreach (var item in matchingItems)
        {
            if (amountRemaining <= 0) break;

            int removed = Supplies.Remove(item, amountRemaining);
            amountRemaining -= removed;
        }

        return amountNeeded - amountRemaining;
    }
}