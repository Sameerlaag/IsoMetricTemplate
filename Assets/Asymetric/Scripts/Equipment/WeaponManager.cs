using System;
using System.Collections;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponData _currentWeaponData;
    [SerializeField] private Transform _weaponHoldPoint;

    // Weapon State
    private int _currentAmmo;
    private bool _isReloading;
    private float _nextFireTime;
    private float _currentSpread;

    // Events for UI/Reticle
    public event Action<float> OnSpreadUpdated;
    public event Action<WeaponData> OnWeaponEquipped;
    public event Action<int, int> OnAmmoUpdated; // Current / Max

    public WeaponData CurrentWeaponData => _currentWeaponData;
    public bool IsReloading => _isReloading;

    private WeaponsInventory _weaponsInventory;
    private WeaponInstance _currentWeaponInstance;

    private void Start()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null)
        {
            _weaponsInventory = InventoryManager.Instance.PlayerInventory.Weapons;
            _weaponsInventory.ActiveWeaponChanged += HandleActiveWeaponChanged;

            if (_weaponsInventory.ActiveWeapon != null)
            {
                HandleActiveWeaponChanged(_weaponsInventory.ActiveWeapon, _weaponsInventory.ActiveSlotIndex);
            }
            else if (_currentWeaponData != null)
            {
                EquipWeapon(_currentWeaponData);
            }
        }
        else if (_currentWeaponData != null)
        {
            EquipWeapon(_currentWeaponData);
        }
    }

    private void OnDestroy()
    {
        if (_weaponsInventory != null)
        {
            _weaponsInventory.ActiveWeaponChanged -= HandleActiveWeaponChanged;
        }
    }

    private void HandleActiveWeaponChanged(WeaponInstance weapon, int slotIndex)
    {
        _currentWeaponInstance = weapon;

        if (_currentWeaponInstance != null && _currentWeaponInstance.Definition != null && _currentWeaponInstance.Definition.WeaponData != null)
        {
            EquipWeapon(_currentWeaponInstance.Definition.WeaponData, _currentWeaponInstance.CurrentAmmo);
        }
        else
        {
            _currentWeaponData = null;
            _currentAmmo = 0;
            _currentWeaponInstance = null;
            OnWeaponEquipped?.Invoke(null);
            OnAmmoUpdated?.Invoke(0, 0);
        }
    }

    [SerializeField] private LayerMask _enemyLayer = ~0;

    private IWeaponAimMechanic _currentAimMechanic;

    public IWeaponAimMechanic CurrentAimMechanic => _currentAimMechanic;

    public void EquipWeapon(WeaponData data, int startingAmmo = -1)
    {
        if (data == null)
        {
            _currentWeaponData = null;
            _currentAmmo = 0;
            _currentAimMechanic = null;
            OnWeaponEquipped?.Invoke(null);
            OnAmmoUpdated?.Invoke(0, 0);
            return;
        }

        _currentWeaponData = data;
        _currentAmmo = startingAmmo >= 0 ? startingAmmo : data.MaxClipAmmo;
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.CurrentAmmo = _currentAmmo;
        }
        _currentSpread = data.BaseSpread;

        // Obtain or attach weapon aim mechanic (defaults to RevolverAimMechanic if none present)
        _currentAimMechanic = GetComponent<IWeaponAimMechanic>();
        if (_currentAimMechanic == null)
        {
            _currentAimMechanic = gameObject.AddComponent<RevolverAimMechanic>();
        }

        _currentAimMechanic.Initialize(_currentWeaponData);

        OnWeaponEquipped?.Invoke(_currentWeaponData);
        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
    }

    public void UpdateAimState(Vector3 aimPoint, float currentMovementSpeed, Vector2 mouseDelta, float cursorDistance, Camera camera)
    {
        if (_currentWeaponData == null) return;

        if (_currentAimMechanic != null)
        {
            _currentAimMechanic.UpdateAim(aimPoint, currentMovementSpeed, mouseDelta, cursorDistance, camera);
            OnSpreadUpdated?.Invoke(_currentAimMechanic.CurrentReticleScale);
        }
        else
        {
            UpdateSpread(currentMovementSpeed);
        }
    }

    public void UpdateSpread(float currentMovementSpeed)
    {
        if (_currentWeaponData == null) return;

        // Calculate dynamic spread based on movement speed
        float targetSpread = _currentWeaponData.BaseSpread +
                             (currentMovementSpeed * _currentWeaponData.MovementSpreadMultiplier);
        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * 10f);

        OnSpreadUpdated?.Invoke(_currentSpread);
    }

    public void TryFire(Vector3 aimPoint, Camera camera = null)
    {
        if (_isReloading || Time.time < _nextFireTime || _currentWeaponData == null) return;

        if (_currentAmmo <= 0)
        {
            TryReload();
            return;
        }

        _nextFireTime = Time.time + _currentWeaponData.FireRate;
        _currentAmmo--;
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.CurrentAmmo = _currentAmmo;
        }

        _currentSpread += _currentWeaponData.RecoilPerShot; // Add bloom

        if (_currentAimMechanic != null && camera != null)
        {
            bool hitSuccess = _currentAimMechanic.EvaluateShot(aimPoint, camera, _enemyLayer, out float hitProb, out RaycastHit hit);
            Debug.Log($"Fired {_currentWeaponData.WeaponName}! Hit: {hitSuccess} (Hit Prob: {hitProb * 100:F1}%). Ammo remaining: {_currentAmmo}");
        }
        else
        {
            Debug.Log($"Fired {_currentWeaponData.WeaponName}! Ammo remaining: {_currentAmmo}");
        }

        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
    }

    public void TryReload()
    {
        if (_isReloading || _currentWeaponData == null || _currentAmmo >= _currentWeaponData.MaxClipAmmo) return;

        int availableReserveAmmo = 0;
        if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null)
        {
            availableReserveAmmo = InventoryManager.Instance.PlayerInventory.GetTotalAmmoCount(_currentWeaponData.RequiredAmmoType);
        }

        if (availableReserveAmmo <= 0)
        {
            Debug.Log($"[Reload] No reserve ammo of type {_currentWeaponData.RequiredAmmoType} in inventory!");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        Debug.Log($"Reloading {_currentWeaponData.WeaponName}...");
        yield return new WaitForSeconds(_currentWeaponData.ReloadTime);

        int ammoNeeded = _currentWeaponData.MaxClipAmmo - _currentAmmo;
        int consumedAmmo = 0;

        if (InventoryManager.Instance != null && InventoryManager.Instance.PlayerInventory != null)
        {
            consumedAmmo = InventoryManager.Instance.PlayerInventory.ConsumeAmmo(_currentWeaponData.RequiredAmmoType, ammoNeeded);
        }

        _currentAmmo += consumedAmmo;
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.CurrentAmmo = _currentAmmo;
        }

        _isReloading = false;
        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
        Debug.Log($"Reload Complete! Clip ammo: {_currentAmmo}/{_currentWeaponData.MaxClipAmmo}");
    }
}