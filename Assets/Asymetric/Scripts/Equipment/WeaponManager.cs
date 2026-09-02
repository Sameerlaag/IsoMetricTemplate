using System;
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

    private void Start()
    {
        if (_currentWeaponData != null)
        {
            EquipWeapon(_currentWeaponData);
        }
    }

    public void EquipWeapon(WeaponData data)
    {
        _currentWeaponData = data;
        _currentAmmo = data.MaxClipAmmo;
        _currentSpread = data.BaseSpread;
        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
    }

    public void UpdateSpread(float currentMovementSpeed)
    {
        if (_currentWeaponData == null) return;

        // Calculate dynamic spread based on movement speed
        float targetSpread = _currentWeaponData.BaseSpread + (currentMovementSpeed * _currentWeaponData.MovementSpreadMultiplier);
        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * 10f);

        OnSpreadUpdated?.Invoke(_currentSpread);
    }

    public void TryFire(Vector3 aimPoint)
    {
        if (_isReloading || Time.time < _nextFireTime) return;

        if (_currentAmmo <= 0)
        {
            TryReload();
            return;
        }

        _nextFireTime = Time.time + _currentWeaponData.FireRate;
        _currentAmmo--;
        _currentSpread += _currentWeaponData.RecoilPerShot; // Add bloom

        // Perform raycast/projectile instantiation here using aimPoint
        Debug.Log($"Fired {_currentWeaponData.WeaponName}! Ammo remaining: {_currentAmmo}");

        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
    }

    public void TryReload()
    {
        if (_isReloading || _currentAmmo == _currentWeaponData.MaxClipAmmo) return;
        StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(_currentWeaponData.ReloadTime);

        _currentAmmo = _currentWeaponData.MaxClipAmmo;
        _isReloading = false;
        OnAmmoUpdated?.Invoke(_currentAmmo, _currentWeaponData.MaxClipAmmo);
        Debug.Log("Reload Complete!");
    }
}