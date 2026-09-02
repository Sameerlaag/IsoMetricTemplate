using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private WeaponManager _weaponManager;

    [Header("Inventory")] [SerializeField] private int _supplierCapacity = 10;
    private int _weaponCapacity = 2;
    private int _equipmentCapacity = 1;

    [Tooltip(
        "If enabled, the same item ID can occupy multiple inventory slots. " +
        "If disabled, an item ID can only have one stack."
    )]
    [SerializeField]
    private bool _allowMultipleStacks = true;

    public PlayerInventory PlayerInventory { get; private set; }

    public bool AllowMultipleStacks => _allowMultipleStacks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerInventory = new PlayerInventory(
            _supplierCapacity,
            _weaponCapacity,
            _equipmentCapacity,
            _allowMultipleStacks
        );
    }

    public void ItemInteracted(InventoryUIItem uiItem)
    {
        if (uiItem == null || uiItem.Item == null) return;

        ItemInstance item = uiItem.Item;

        if (item.ItemType == ItemType.WEAPON)
        {
            PlayerInventory.TryEquipWeapon(item);
        }
        else if (item.ItemType == ItemType.EQUIPABLE)
        {
            PlayerInventory.TryEquipEquipment(item);
        }
    }
}