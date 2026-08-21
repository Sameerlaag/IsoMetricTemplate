using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory")]
    [SerializeField]
    private int _capacity = 10;

    [Tooltip(
        "If enabled, the same item ID can occupy multiple inventory slots. " +
        "If disabled, an item ID can only have one stack."
    )]
    [SerializeField]
    private bool _allowMultipleStacks = true;

    public Inventory PlayerInventory { get; private set; }

    public bool AllowMultipleStacks => _allowMultipleStacks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerInventory = new Inventory(
            _capacity,
            _allowMultipleStacks
        );
    }
}