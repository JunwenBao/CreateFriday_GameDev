using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Game.UI;

// 对话流程控制：逐句推进台词、弹出选项、结束对话。
// 画面由 DialogueWindow 显示（通过 UIManager 打开 / 关闭），本组件不直接引用任何 UI 物体。
public class TextParser : MonoBehaviour
{
    // 一段对话彻底播放完毕（最后一句被再次按E）时触发
    public UnityEvent onConversationEnd;

    public event System.Action<DialogueLine> LineStarted;

    private DialogueWindow window;
    private DialogueData currentDialogue;
    private int lineIndex;
    private bool awaitingChoice;

    // 正在播放的选项回复（ReplyThenReturn）；为 null 表示在播放主线台词
    private List<DialogueReply> activeReplies;
    private int replyIndex = -1;

    public bool IsConversationActive { get; private set; }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // 玩家被销毁（如切换场景）时对话窗口仍由常驻的 UIManager 持有，需要一并关闭
    private void OnDestroy()
    {
        if (IsConversationActive)
            CloseWindows();
    }

    // 对话进行中切换语言：用新语言重新显示当前这一句
    private void OnLocaleChanged(Locale _)
    {
        if (IsConversationActive && HasCurrentLine)
            DisplayCurrentLine();
    }

    private void Update()
    {
        if (!IsConversationActive || window == null)
            return;

        // 窗口被外部关闭（例如 CloseAll）时结束对话，避免玩家一直被锁住
        if (!window.IsOpen)
            EndConversation(raiseFinished: false);
    }

    private bool HasCurrentLine => currentDialogue != null && lineIndex >= 0 && lineIndex < currentDialogue.conversationBlock.Count;

    private DialogueLine CurrentLine => currentDialogue.conversationBlock[lineIndex];

    private bool IsShowingReply => activeReplies != null && replyIndex >= 0 && replyIndex < activeReplies.Count;

    public void BeginDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.conversationBlock == null || dialogue.conversationBlock.Count == 0)
        {
            Debug.LogWarning("Dialogue data is empty.", this);
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager is missing, cannot show the dialogue window.", this);
            // 让调用方（DialogueTrigger）恢复玩家与相机，但不广播对话完成
            onConversationEnd?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        lineIndex = -1;
        awaitingChoice = false;
        ClearReplies();
        IsConversationActive = true;

        // 窗口可能异步加载：加载完成前推进的台词会在 OnWindowOpened 中补显示
        UIManager.Instance.Open<DialogueWindow>(new DialogueWindow.Request { OnContinue = OnContinueRequested }, OnWindowOpened);
        if (!UIManager.Instance.IsOpen<DialogueWindow>())
        {
            // 加载失败（例如未在 UIWindowRegistry 中注册），UIManager 已输出错误
            EndConversation(raiseFinished: false);
            return;
        }
        AdvanceLine();
    }

    private void OnWindowOpened(DialogueWindow opened)
    {
        if (!IsConversationActive)
            return;

        window = opened;
        if (HasCurrentLine)
            DisplayCurrentLine();
    }

    private void OnContinueRequested()
    {
        if (!IsConversationActive || awaitingChoice || !HasCurrentLine)
            return;

        if (IsShowingReply)
        {
            AdvanceReply();
            return;
        }

        // 带选项的台词：整句显示后再按一次 E 才弹出选项（第一次按 E 只会跳过打字，由 DialogueWindow 处理）
        if (CurrentLine.HasChoices)
        {
            ShowChoices();
            return;
        }

        AdvanceLine();
    }

    private void StartReplies(List<DialogueReply> replies)
    {
        activeReplies = replies;
        replyIndex = -1;
        AdvanceReply();
    }

    private void AdvanceReply()
    {
        replyIndex++;
        if (!IsShowingReply)
        {
            // 回复播放完（或没有配置回复）：重新显示带选项的台词，玩家再按 E 弹出选项
            ClearReplies();
            DisplayCurrentLine();
            return;
        }

        LineStarted?.Invoke(activeReplies[replyIndex].ToLine());
        DisplayCurrentLine();
    }

    private void ClearReplies()
    {
        activeReplies = null;
        replyIndex = -1;
    }

    private void AdvanceLine()
    {
        lineIndex++;
        if (lineIndex >= currentDialogue.conversationBlock.Count)
        {
            EndConversation();
            return;
        }

        LineStarted?.Invoke(CurrentLine);
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (window == null)
            return;

        DialogueLine line = IsShowingReply ? activeReplies[replyIndex].ToLine() : CurrentLine;
        window.ShowLine(line.speaker.ToString(), currentDialogue.GetLineText(line));
    }

    private void ShowChoices()
    {
        awaitingChoice = true;
        // 选项面板期间隐藏对话框（不关闭，否则会被 Update 视为对话被外部终止）
        window?.SetHidden(true);
        DialogueData dialogue = currentDialogue;
        int index = lineIndex;

        UIManager.Instance.Open<DialogueChoiceWindow>(new DialogueChoiceWindow.Request
        {
            Dialogue = dialogue,
            Choices = CurrentLine.choices,
            OnSelected = choiceIndex =>
            {
                // 回调到达前对话已被结束或替换时忽略
                if (!awaitingChoice || currentDialogue != dialogue || lineIndex != index)
                    return;
                OnChoiceSelected(dialogue.conversationBlock[index].choices[choiceIndex]);
            }
        });
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        awaitingChoice = false;
        window?.SetHidden(false);

        switch (choice.outcome)
        {
            case DialogueChoiceOutcome.Continue:
                AdvanceLine();
                break;
            case DialogueChoiceOutcome.EndDialogue:
                EndConversation();
                break;
            case DialogueChoiceOutcome.GameOver:
                EndConversation(raiseFinished: false);
                GameEvents.RaiseGameOver();
                break;
            case DialogueChoiceOutcome.ReplyThenReturn:
                StartReplies(choice.replies);
                break;
        }
    }

    private void EndConversation(bool raiseFinished = true)
    {
        DialogueData finished = currentDialogue;
        CloseWindows();
        IsConversationActive = false;
        awaitingChoice = false;
        ClearReplies();
        currentDialogue = null;
        window = null;
        onConversationEnd?.Invoke();

        // 在对话 UI/相机复原之后再广播，监听者可以安全地开启新的对话或演出
        if (raiseFinished)
            GameEvents.RaiseDialogueFinished(finished);
    }

    private void CloseWindows()
    {
        UIManager manager = UIManager.Instance;
        if (manager == null)
            return;

        if (awaitingChoice)
            manager.Close<DialogueChoiceWindow>();
        manager.Close<DialogueWindow>();
    }
}
