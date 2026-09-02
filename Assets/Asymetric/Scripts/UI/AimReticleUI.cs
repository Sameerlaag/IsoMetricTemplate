using UnityEngine;
using UnityEngine.UI;

public class AimReticleUI : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private WeaponManager _weaponManager;
    [SerializeField] private Image _reticleImage;

    [SerializeField] private InputReader _inputReader;

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

    private void Update()
    {
        if (_playerController != null && _playerController.CurrentState == PlayerController.MovementState.Aiming)
        {
            if (_reticleImage != null)
            {
                if (!_reticleImage.enabled) _reticleImage.enabled = true;

                if (_inputReader != null)
                {
                    _reticleImage.transform.position = _inputReader.MousePosition;
                }
            }
        }
        else
        {
            if (_reticleImage != null && _reticleImage.enabled)
            {
                _reticleImage.enabled = false;
            }
        }
    }

    private void HandleWeaponEquipped(WeaponData newWeapon)
    {
        if (_reticleImage != null && newWeapon != null)
        {
            _reticleImage.sprite = newWeapon.EnabledAimSprite;
        }
    }

    private void UpdateReticleSpread(float spreadScale)
    {
        if (_reticleImage == null) return;
        _reticleImage.transform.localScale = new Vector3(spreadScale, spreadScale, 1f);
    }
}