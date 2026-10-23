using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;
using Game.UI;

[RequireComponent(typeof(TMP_Animated))]
public class TextParser : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerNameText;

    // 一段对话彻底播放完毕（最后一句被再次按E）时触发
    public UnityEvent onConversationEnd;

    public event System.Action<DialogueLine> LineStarted;

    private TMP_Animated animatedText;
    private DialogueData currentDialogue;
    private int lineIndex;
    private bool awaitingChoice;

    public bool IsConversationActive { get; private set; }

    private void Awake()
    {
        animatedText = GetComponent<TMP_Animated>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // 对话进行中切换语言：用新语言重新显示当前这一句
    private void OnLocaleChanged(Locale _)
    {
        if (IsConversationActive && currentDialogue != null && lineIndex >= 0 && lineIndex < currentDialogue.conversationBlock.Count)
            animatedText.ReadText(currentDialogue.GetLineText(currentDialogue.conversationBlock[lineIndex]));
    }

    private void Update()
    {
        if (!IsConversationActive || awaitingChoice)
            return;

        // 带选项的台词显示完毕后自动弹出选项（按 E 跳过打字也会立即弹出）
        if (CurrentLine.HasChoices)
        {
            if (Input.GetKeyDown(KeyCode.E) && animatedText.IsReading)
                animatedText.SkipToEnd();
            if (!animatedText.IsReading)
                ShowChoices();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // 第一次按E：文字还在打字时，直接显示完整这一句
            if (animatedText.IsReading)
                animatedText.SkipToEnd();
            // 再次按E：这一句已经显示完，进入下一句
            else
                AdvanceLine();
        }
    }

    private DialogueLine CurrentLine => currentDialogue.conversationBlock[lineIndex];

    public void BeginDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.conversationBlock == null || dialogue.conversationBlock.Count == 0)
        {
            Debug.LogWarning("Dialogue data is empty.", this);
            return;
        }

        currentDialogue = dialogue;
        lineIndex = -1;
        awaitingChoice = false;
        IsConversationActive = true;
        AdvanceLine();
    }

    private void AdvanceLine()
    {
        lineIndex++;
        if (lineIndex >= currentDialogue.conversationBlock.Count)
        {
            EndConversation();
            return;
        }

        DialogueLine line = currentDialogue.conversationBlock[lineIndex];
        
        if (speakerNameText != null) speakerNameText.text = line.speaker.ToString();

        LineStarted?.Invoke(line);

        animatedText.ReadText(currentDialogue.GetLineText(line));
    }

    private void ShowChoices()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager is missing, cannot show dialogue choices. Continuing dialogue.", this);
            AdvanceLine();
            return;
        }

        awaitingChoice = true;
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
        }
    }

    private void EndConversation(bool raiseFinished = true)
    {
        DialogueData finished = currentDialogue;
        IsConversationActive = false;
        awaitingChoice = false;
        currentDialogue = null;
        onConversationEnd?.Invoke();

        // 在对话 UI/相机复原之后再广播，监听者可以安全地开启新的对话或演出
        if (raiseFinished)
            GameEvents.RaiseDialogueFinished(finished);
    }
}
