using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIItem : MonoBehaviour
{
    [SerializeField] private Color _defaultColor;
    [SerializeField] private Color _selectedColor;
    [SerializeField] private Color _emptyColor;
    [SerializeField] private Image _containerImage;
    [SerializeField] private Image _weaponImage;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _ammoCount;
    
    private bool _isActive;

    public void Initialize(string weaponName, Sprite weaponSprite, int ammoCount, int inventoryAmmo, bool active)
    {
        _name.text = weaponName;
        _weaponImage.sprite = weaponSprite;
        UpdateCount(ammoCount, inventoryAmmo);
        Activate(active);
    }
    
    public void Activate(bool isActive)
    {
        _containerImage.color = isActive ? _selectedColor : _defaultColor;
        _isActive = isActive;
    }

    public void UpdateCount(int ammoCount, int inventoryAmmo)
    {
        if (ammoCount <= 0) _containerImage.color = _emptyColor;
        _ammoCount.text = $"{ammoCount} - {inventoryAmmo}";
    }
}