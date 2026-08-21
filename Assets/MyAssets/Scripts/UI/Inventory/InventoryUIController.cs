using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Transform _inventoryContainer;

    [SerializeField]
    private GameObject _inventoryItemPrefab;

    private readonly List<InventoryUIItem> _items = new();

    private Inventory _inventory;

    private InventoryUIItem _selectedItem;

    public InventoryUIItem SelectedItem => _selectedItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _inventory = InventoryManager.Instance.PlayerInventory;

        _inventory.ItemAdded += AddItem;
        _inventory.ItemChanged += UpdateItem;
        _inventory.ItemRemoved += RemoveItem;

        // Useful if the UI is created after the inventory already
        // contains items.
        foreach (ItemInstance item in _inventory.Items)
        {
            AddItem(item);
        }
    }

    private void OnDestroy()
    {
        if (_inventory == null)
            return;

        _inventory.ItemAdded -= AddItem;
        _inventory.ItemChanged -= UpdateItem;
        _inventory.ItemRemoved -= RemoveItem;
    }

    public void AddItem(ItemInstance item)
    {
        if (item == null)
            return;

        // Safety check against accidentally creating duplicates.
        if (FindUIItem(item) != null)
        {
            UpdateItem(item);
            return;
        }

        GameObject itemObject = Instantiate(
            _inventoryItemPrefab,
            _inventoryContainer
        );

        InventoryUIItem uiItem =
            itemObject.GetComponent<InventoryUIItem>();

        if (uiItem == null)
        {
            Debug.LogError(
                "Inventory item prefab is missing an InventoryUIItem component."
            );

            Destroy(itemObject);
            return;
        }

        uiItem.SetItem(item);

        _items.Add(uiItem);

        // Automatically select the first item.
        if (_selectedItem == null)
        {
            SelectItem(uiItem);
        }
    }

    public void UpdateItem(ItemInstance item)
    {
        if (item == null)
            return;

        InventoryUIItem uiItem = FindUIItem(item);

        if (uiItem == null)
        {
            // Safety fallback.
            AddItem(item);
            return;
        }

        uiItem.UpdateCount();
    }

    public void RemoveItem(ItemInstance item)
    {
        if (item == null)
            return;

        InventoryUIItem uiItem = FindUIItem(item);

        if (uiItem == null)
            return;

        bool wasSelected = uiItem == _selectedItem;

        _items.Remove(uiItem);

        Destroy(uiItem.gameObject);

        if (wasSelected)
        {
            _selectedItem = null;

            // Automatically select the first remaining item.
            if (_items.Count > 0)
            {
                SelectItem(_items[0]);
            }
        }
    }

    public void SelectItem(InventoryUIItem item)
    {
        if (item == null)
            return;

        if (!_items.Contains(item))
            return;

        if (_selectedItem == item)
            return;

        if (_selectedItem != null)
        {
            _selectedItem.SetSelected(false);
        }

        _selectedItem = item;

        _selectedItem.SetSelected(true);
    }

    private InventoryUIItem FindUIItem(ItemInstance item)
    {
        return _items.Find(uiItem => uiItem.Item == item);
    }
}