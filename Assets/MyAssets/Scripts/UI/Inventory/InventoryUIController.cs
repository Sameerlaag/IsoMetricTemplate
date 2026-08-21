using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private Transform _inventoryContainer;
    [SerializeField] private GameObject _inventoryItemPrefab;

    private List<GameObject> _items = new List<GameObject>();

    private static InventoryUIController instance;
    
    public static InventoryUIController Instance => instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void AddItem(GameObject item)
    {
        
    }
}