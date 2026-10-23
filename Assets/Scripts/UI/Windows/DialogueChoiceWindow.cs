using System;
using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// 对话中的选项面板：由 TextParser 打开，玩家点击按钮或按数字键 1/2/... 做出选择
public class DialogueChoiceWindow : UIWindow
{
    public sealed class Request
    {
        public DialogueData Dialogue;
        public IReadOnlyList<DialogueChoice> Choices;
        public Action<int> OnSelected;
    }

    [Tooltip("选项按钮，按顺序对应 DialogueLine.choices；多余的按钮会被隐藏。按钮文字取子节点上的 TMP_Text")]
    [SerializeField] private Button[] optionButtons;

    private TMP_Text[] optionLabels;
    private Request request;

    protected override void OnCreate()
    {
        optionLabels = new TMP_Text[optionButtons.Length];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => Select(index));
            optionLabels[i] = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
        }
    }

    protected override void OnOpen(object data)
    {
        request = data as Request;
        RefreshOptions();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    protected override void OnRefresh(object data)
    {
        request = data as Request;
        RefreshOptions();
    }

    protected override void OnClose()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // 必须做出选择，返回键无效
    protected override void OnBackPressed() { }

    private void Update()
    {
        if (!IsOpen || request == null)
            return;

        int count = Mathf.Min(request.Choices.Count, optionButtons.Length, 9);
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                Select(i);
                return;
            }
        }
    }

    private void OnLocaleChanged(Locale _) => RefreshOptions();

    private void RefreshOptions()
    {
        int count = request?.Choices?.Count ?? 0;
        if (count > optionButtons.Length)
            Debug.LogWarning($"Dialogue has {count} choices but only {optionButtons.Length} option buttons.", this);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool used = i < count;
            optionButtons[i].gameObject.SetActive(used);
            if (used && optionLabels[i] != null)
                optionLabels[i].text = request.Dialogue.GetChoiceText(request.Choices[i]);
        }
    }

    private void Select(int index)
    {
        if (request == null || index >= request.Choices.Count)
            return;

        Action<int> onSelected = request.OnSelected;
        request = null;
        Close();
        onSelected?.Invoke(index);
    }
}
