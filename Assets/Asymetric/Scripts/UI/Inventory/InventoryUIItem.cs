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
        if (Item == null)
            return;

        _countText.text = Item.Count.ToString();
    }

    public void SetSelected(bool selected)
    {
        if (selected) _background.color = _selectedColor;
        else _background.color = _defaultColor;
    }
}