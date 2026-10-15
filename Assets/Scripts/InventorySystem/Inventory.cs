using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int count;

    public bool IsEmpty => item == null || count <= 0;

    public void Clear()
    {
        item = null;
        count = 0;
    }
}

// 挂在玩家上：纯数据与逻辑，不依赖 UI。数据变化时触发 OnChanged
public class Inventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int capacity = 20;
    [Tooltip("可在 Inspector 里预填初始物品")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnChanged;

    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        while (slots.Count < capacity)
            slots.Add(new InventorySlot());
    }

    // 返回放不下的剩余数量，0 表示全部放入
    public int Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return amount;

        int remaining = amount;

        // 先叠到已有同类格子，再放空格子
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.item == item && slot.count < item.MaxStack)
            {
                int add = Mathf.Min(item.MaxStack - slot.count, remaining);
                slot.count += add;
                remaining -= add;
            }
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty)
            {
                int add = Mathf.Min(item.MaxStack, remaining);
                slot.item = item;
                slot.count = add;
                remaining -= add;
            }
        }

        if (remaining != amount) OnChanged?.Invoke();
        return remaining;
    }

    // 数量不足时不做任何修改并返回 false
    public bool Remove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount) return false;

        for (int i = slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.item != item) continue;

            int take = Mathf.Min(slot.count, amount);
            slot.count -= take;
            amount -= take;
            if (slot.count <= 0) slot.Clear();
        }

        OnChanged?.Invoke();
        return true;
    }

    public void RemoveAt(int index, int amount = 1)
    {
        if (!IsValidIndex(index) || slots[index].IsEmpty) return;

        InventorySlot slot = slots[index];
        slot.count -= amount;
        if (slot.count <= 0) slot.Clear();

        OnChanged?.Invoke();
    }

    public void UseAt(int index)
    {
        if (!IsValidIndex(index) || slots[index].IsEmpty) return;

        ItemData item = slots[index].item;
        if (item.Use(this))
            Remove(item, 1);
    }

    public int GetCount(ItemData item)
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item == item) total += slots[i].count;
        }
        return total;
    }

    public bool Has(ItemData item, int amount = 1) => GetCount(item) >= amount;

    private bool IsValidIndex(int index) => index >= 0 && index < slots.Count;
}
