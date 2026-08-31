using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [field: SerializeField]
    public string ID { get; private set; }

    [field: SerializeField]
    public string DisplayName { get; private set; }

    [field: SerializeField]
    public ItemType ItemType { get; private set; }

    [field: SerializeField]
    public int MaxStack { get; private set; }

    [field: SerializeField]
    public string InventoryPrefabAddress { get; private set; }

    [field: SerializeField]
    public string WorldPrefabAddress { get; private set; }
}