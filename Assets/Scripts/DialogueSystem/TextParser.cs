using UnityEngine;
using UnityEngine.Events;
using TMPro;

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

    public bool IsConversationActive { get; private set; }

    private void Awake()
    {
        animatedText = GetComponent<TMP_Animated>();
    }

    private void Update()
    {
        if (!IsConversationActive)
            return;

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

    public void BeginDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.conversationBlock == null || dialogue.conversationBlock.Count == 0)
        {
            Debug.LogWarning("Dialogue data is empty.", this);
            return;
        }

        currentDialogue = dialogue;
        lineIndex = -1;
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

        animatedText.ReadText(line.text);
    }

    private void EndConversation()
    {
        DialogueData finished = currentDialogue;
        IsConversationActive = false;
        currentDialogue = null;
        onConversationEnd?.Invoke();

        // 在对话 UI/相机复原之后再广播，监听者可以安全地开启新的对话或演出
        GameEvents.RaiseDialogueFinished(finished);
    }
}
