using UnityEngine;
using UnityEngine.UI;

public class AimReticleUI : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private WeaponManager _weaponManager;
    [SerializeField] private Image _reticleImage;

    private void OnEnable()
    {
        if (_weaponManager != null)
        {
            _weaponManager.OnSpreadUpdated += UpdateReticleSpread;
            _weaponManager.OnWeaponEquipped += HandleWeaponEquipped;
        }
    }

    private void OnDisable()
    {
        if (_weaponManager != null)
        {
            _weaponManager.OnSpreadUpdated -= UpdateReticleSpread;
            _weaponManager.OnWeaponEquipped -= HandleWeaponEquipped;
        }
    }

    private void HandleWeaponEquipped(WeaponData newWeapon)
    {
        if (_reticleImage != null && newWeapon != null)
        {
            _reticleImage.enabled = true;
            _reticleImage.sprite = newWeapon.EnabledAimSprite;
        }
    }

    private void UpdateReticleSpread(float spreadAngle)
    {
        float scaleFactor = 1f + (spreadAngle * 0.1f);
        _reticleImage.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }
}