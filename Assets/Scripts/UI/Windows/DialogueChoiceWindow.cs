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

    [Tooltip("选项按钮预制体（需带 Button，按钮文字取子节点上的 TMP_Text），按选项数量自动生成")]
    [SerializeField] private GameObject optionButtonPrefab;

    [Tooltip("生成的按钮放在此节点下（建议挂 Layout Group 自动排版）；留空则放在窗口根节点下")]
    [SerializeField] private RectTransform optionContainer;

    // 已生成的按钮会在多次打开间复用，只增不减，多余的隐藏
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<TMP_Text> optionLabels = new List<TMP_Text>();
    private Request request;

    protected override void OnCreate()
    {
        if (optionContainer == null)
            optionContainer = (RectTransform)transform;
        if (optionButtonPrefab == null)
            Debug.LogError("DialogueChoiceWindow: optionButtonPrefab is not assigned.", this);
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

        int count = Mathf.Min(request.Choices.Count, optionButtons.Count, 9);
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
        while (optionButtons.Count < count)
        {
            if (!CreateOptionButton())
                break;
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool used = i < count;
            optionButtons[i].gameObject.SetActive(used);
            if (used && optionLabels[i] != null)
                optionLabels[i].text = request.Dialogue.GetChoiceText(request.Choices[i]);
        }
    }

    private bool CreateOptionButton()
    {
        if (optionButtonPrefab == null)
            return false;

        GameObject instance = Instantiate(optionButtonPrefab, optionContainer, false);
        Button button = instance.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("DialogueChoiceWindow: optionButtonPrefab has no Button component.", optionButtonPrefab);
            Destroy(instance);
            return false;
        }

        int index = optionButtons.Count;
        instance.name = $"{optionButtonPrefab.name}_{index + 1}";
        button.onClick.AddListener(() => Select(index));
        optionButtons.Add(button);
        optionLabels.Add(instance.GetComponentInChildren<TMP_Text>(true));
        return true;
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
