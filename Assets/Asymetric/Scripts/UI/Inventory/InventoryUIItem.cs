using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Image _background;
    [SerializeField] private Color _defaultColor = Color.black;
    [SerializeField] private Color _selectedColor = Color.crimson;

    public ItemInstance Item { get; private set; }

    public void SetItem(ItemInstance item)
    {
        Item = item;

        UpdateName();
        UpdateCount();
        SetSelected(false);
    }

    public void UpdateName()
    {
        if (Item == null)
            return;

        _nameText.text = Item.DisplayName;
    }
    public void UpdateCount()
    {
        if (Item == null || _countText == null)
            return;

        if (Item is WeaponInstance gunInstance && gunInstance.Definition != null && gunInstance.Definition.WeaponData != null)
        {
            _countText.text = $"{gunInstance.CurrentAmmo} / {gunInstance.Definition.WeaponData.MaxClipAmmo}";
        }
        else if (Item.ItemType == ItemType.WEAPON)
        {
            _countText.text = "";
        }
        else if (Item.ItemType == ItemType.DOCUMENT || Item.ItemType == ItemType.KEY)
        {
            _countText.text = "";
        }
        else
        {
            _countText.text = Item.Count > 1 ? Item.Count.ToString() : "";
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected) _background.color = _selectedColor;
        else _background.color = _defaultColor;
    }

    public void SignalInteracted()
    {
        InventoryManager.Instance.ItemInteracted(this);
    }
}