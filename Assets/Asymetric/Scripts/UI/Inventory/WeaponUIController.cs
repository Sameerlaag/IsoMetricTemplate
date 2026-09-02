using System.Collections.Generic;
using UnityEngine;

public class WeaponUIController : MonoBehaviour
{
    [SerializeField] private List<WeaponUIItem> _weaponSlots = new();

    private WeaponsInventory _weaponsInventory;

    [SerializeField] private WeaponManager _weaponManager;

    private void Start()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null)
        {
            _weaponsInventory = InventoryManager.Instance.PlayerInventory.Weapons;
            _weaponsInventory.WeaponEquipped += HandleWeaponEquipped;
            _weaponsInventory.WeaponUnequipped += HandleWeaponUnequipped;
            _weaponsInventory.ActiveWeaponChanged += HandleActiveWeaponChanged;

            if (_weaponManager != null)
            {
                _weaponManager.OnAmmoUpdated += HandleAmmoUpdated;
            }

            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (_weaponsInventory != null)
        {
            _weaponsInventory.WeaponEquipped -= HandleWeaponEquipped;
            _weaponsInventory.WeaponUnequipped -= HandleWeaponUnequipped;
            _weaponsInventory.ActiveWeaponChanged -= HandleActiveWeaponChanged;
        }

        if (_weaponManager != null)
        {
            _weaponManager.OnAmmoUpdated -= HandleAmmoUpdated;
        }
    }

    private void HandleAmmoUpdated(int current, int max)
    {
        RefreshUI();
    }

    private void HandleWeaponEquipped(WeaponInstance weapon, int slotIndex)
    {
        RefreshUI();
    }

    private void HandleWeaponUnequipped(WeaponInstance weapon, int slotIndex)
    {
        RefreshUI();
    }

    private void HandleActiveWeaponChanged(WeaponInstance weapon, int slotIndex)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_weaponsInventory == null) return;

        IReadOnlyList<WeaponInstance> weapons = _weaponsInventory.Weapons;

        for (int i = 0; i < _weaponSlots.Count; i++)
        {
            if (i < weapons.Count && weapons[i] != null)
            {
                WeaponInstance weaponItem = weapons[i];
                bool isActive = (i == _weaponsInventory.ActiveSlotIndex);
                int reserveAmmo = 0;
                if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null && weaponItem.Definition != null && weaponItem.Definition.WeaponData != null)
                {
                    reserveAmmo = InventoryManager.Instance.PlayerInventory.GetTotalAmmoCount(weaponItem.Definition.WeaponData.RequiredAmmoType);
                }

                _weaponSlots[i].Initialize(
                    weaponItem.DisplayName,
                    weaponItem.Definition.Icon,
                    weaponItem.CurrentAmmo,
                    reserveAmmo,
                    isActive
                );
            }
            else
            {
                _weaponSlots[i].Activate(false);
            }
        }
    }
}