using UnityEngine;
using StarterAssets;
using Cinemachine;
using Game.Story;

public class DialogueTrigger : MonoBehaviour
{
    private NPC currentNPC;
    // NPC of the conversation in progress; null for item dialogues.
    private NPC talkingNPC;
    private ThirdPersonController thirdPersonController;
    private bool dialogueIsActive;
    [SerializeField] private CinemachineTargetGroup targetGroup;

    [SerializeField] private GameObject DialogueCamera;

    [Header("UI")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TextParser textParser;

    public bool IsDialogueActive => dialogueIsActive;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();

        if (textParser == null)
        {
            Debug.LogError("Text Parser is not assigned on DialogueTrigger.", this);
            return;
        }

        textParser.onConversationEnd.AddListener(EndDialogue);
    }

    private void Update()
    {
        if (dialogueIsActive && currentNPC != null)
        {
            currentNPC.TurnToPlayer(transform.position);
        }

        // 对话未开始时E键用于开始对话；对话进行中E键交给TextParser处理快进/下一句
        if (Input.GetKeyDown(KeyCode.E) && currentNPC != null && !dialogueIsActive)
        {
            StartDialogue();
        }
    }

    // 与 NPC 对话：切换到对话相机，聚焦玩家与 NPC
    private void StartDialogue()
    {
        if (currentNPC == null || targetGroup == null || targetGroup.m_Targets.Length < 2)
        {
            Debug.LogWarning("Dialogue requires an NPC and a Cinemachine Target Group with at least two targets.", this);
            return;
        }

        dialogueIsActive = true;
        talkingNPC = currentNPC;
        thirdPersonController.enabled = false;

        targetGroup.m_Targets[0].target = transform;
        targetGroup.m_Targets[1].target = currentNPC.transform;

        currentNPC.TurnToPlayer(transform.position);
        currentNPC.npcIsTalking = true;

        DialogueCamera.SetActive(true);
        dialogueCanvas.SetActive(true);

        textParser.BeginDialogue(currentNPC.dialogue);
    }

    // 与物品对话：不切换相机，仅弹出对话框并开始对话
    public void StartItemDialogue(DialogueData dialogue)
    {
        if (dialogueIsActive) return;

        if (dialogue == null)
        {
            Debug.LogWarning("Item dialogue data is empty.", this);
            return;
        }

        dialogueIsActive = true;
        dialogueCanvas.SetActive(true);

        textParser.BeginDialogue(dialogue);
    }

    private void EndDialogue()
    {
        NPC finishedNPC = talkingNPC;
        talkingNPC = null;

        dialogueIsActive = false;
        if (finishedNPC != null) finishedNPC.npcIsTalking = false;
        thirdPersonController.enabled = true;

        DialogueCamera.SetActive(false);
        dialogueCanvas.SetActive(false);

        // 对话状态恢复后再推进剧情，节点动作可以安全地开启新的对话或演出
        if (finishedNPC != null && finishedNPC.storyNodeOnTalk != null)
        {
            if (StoryManager.Instance != null)
                StoryManager.Instance.TryUnlockNode(finishedNPC.storyNodeOnTalk);
            else
                Debug.LogWarning("No StoryManager in scene; cannot unlock story node.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            currentNPC = other.GetComponent<NPC>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            currentNPC = null;
        }
    }
}