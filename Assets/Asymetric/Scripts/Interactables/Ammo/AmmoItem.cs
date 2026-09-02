using UnityEngine;

[CreateAssetMenu(fileName = "New Ammo Definition", menuName = "Items/Ammo Definition")]
public class AmmoItem : ItemDefinition
{
    [Header("Ammo Settings")]
    public AmmoType ammoType;

    private void OnEnable()
    {
        // Sets the base ItemType automatically when the asset is loaded/created
        ItemType = ItemType.AMMO; 
    }
}