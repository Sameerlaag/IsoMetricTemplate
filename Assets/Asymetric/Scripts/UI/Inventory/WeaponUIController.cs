
using UnityEngine;

public class WeaponUIController : MonoBehaviour
{
    private SuppliesInventory _weaponsInventory;

    private void Start()
    {
        _weaponsInventory = InventoryManager.Instance.PlayerInventory.Supplies;
    }
}
