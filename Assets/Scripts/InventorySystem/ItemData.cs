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

// 物品配置：一个物品一个资源，右键 Create/Inventory/Item 创建
[CreateAssetMenu(fileName = "Item_", menuName = "Inventory/Item", order = 0)]
public class ItemData : ScriptableObject
{
    [Tooltip("存档用的唯一 ID，留空自动生成")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemCategory category = ItemCategory.Misc;
    [SerializeField, Min(1)] private int maxStack = 1;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemCategory Category => category;
    public int MaxStack => maxStack;

    // 需要"使用"效果的物品继承 ItemData 并重写下面两个成员
    public virtual bool IsUsable => false;

    // 返回 true 表示使用后消耗一个
    public virtual bool Use(Inventory owner) => false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
            itemID = Guid.NewGuid().ToString("N");
    }
#endif
}
