using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 单个格子的显示，挂在格子 Prefab 上
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject selectedFrame;

    public void Init(int index, Action<int> onClick)
    {
        button.onClick.AddListener(() => onClick(index));
    }

    public void Refresh(InventorySlot slot)
    {
        bool hasItem = !slot.IsEmpty;
        icon.enabled = hasItem;
        icon.sprite = hasItem ? slot.item.Icon : null;
        countText.text = hasItem && slot.count > 1 ? slot.count.ToString() : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
