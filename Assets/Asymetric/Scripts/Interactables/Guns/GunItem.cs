using UnityEngine;

[CreateAssetMenu(fileName = "New Ammo Definition", menuName = "Items/Gun Definition")]
public class GunItem : ItemDefinition
{
    [Header("Gun Settings")] public WeaponData WeaponData;

    private void OnEnable()
    {
        // Sets the base ItemType automatically when the asset is loaded/created
        ItemType = ItemType.WEAPON;
    }
}