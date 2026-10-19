using System;
using UnityEngine;

public enum ItemCategory
{
    Misc,
    Consumable,
    Weapon,
    Ammo,
    Quest
}

// 物品配置：一个物品一个资源，右键 Create/Items/Item 创建
[CreateAssetMenu(fileName = "Item_", menuName = "Items/Item", order = 0)]
public class ItemData : ScriptableObject
{
    [Tooltip("存档用的唯一 ID，留空自动生成")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemCategory category = ItemCategory.Misc;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemCategory Category => category;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
            itemID = Guid.NewGuid().ToString("N");
    }
#endif
}
