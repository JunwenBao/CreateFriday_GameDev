using System;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 对话框窗口：只负责显示说话人与逐字台词，并把"继续"输入（E 键 / 点击对话框）交给 TextParser。
// 台词推进、选项与结束由 TextParser 控制，通过 UIManager 打开 / 关闭本窗口。
public class DialogueWindow : UIWindow<DialogueWindow.Request>, IPointerClickHandler
{
    public sealed class Request
    {
        // 当前台词已完整显示后，玩家请求进入下一句
        public Action OnContinue;
    }

    [SerializeField] private TMP_Animated bodyText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private KeyCode continueKey = KeyCode.E;

    private Request request;
    private int openedFrame = -1;

    public bool IsTyping => bodyText != null && bodyText.IsReading;

    public void ShowLine(string speakerName, string text)
    {
        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        // 打字协程只能在激活的物体上运行
        if (bodyText != null && bodyText.isActiveAndEnabled)
            bodyText.ReadText(text ?? string.Empty);
    }

    public void SkipTyping()
    {
        if (bodyText != null)
            bodyText.SkipToEnd();
    }

    protected override void OnOpen(Request data)
    {
        request = data;
        // 打开对话的那一下 E 键不能同时跳过第一句
        openedFrame = Time.frameCount;
    }

    protected override void OnRefresh(Request data) => request = data;

    protected override void OnClose() => request = null;

    private void Update()
    {
        if (Input.GetKeyDown(continueKey) && AcceptsInput())
            Continue();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && AcceptsInput())
            Continue();
    }

    // 第一次：跳过打字直接显示整句；整句已显示：请求下一句
    private void Continue()
    {
        if (IsTyping)
            SkipTyping();
        else
            request?.OnContinue?.Invoke();
    }

    // 上方有其他可交互窗口（选项、暂停菜单等）时不响应
    private bool AcceptsInput()
    {
        if (!IsOpen || IsCovered || IsHidden || request == null || Time.frameCount == openedFrame)
            return false;

        UIWindow focused = UIManager.Instance != null ? UIManager.Instance.FocusedWindow : null;
        return focused == null || focused == this || focused.SortingOrder < SortingOrder;
    }
}
