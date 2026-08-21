using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public Inventory PlayerInventory { get; private set; }

    [SerializeField]
    private int _capacity = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerInventory = new Inventory(_capacity);
    }
}