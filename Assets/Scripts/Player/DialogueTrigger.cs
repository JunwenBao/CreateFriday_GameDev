using UnityEngine;
using StarterAssets;
using Cinemachine;

public class DialogueTrigger : MonoBehaviour
{
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
        if (dialogueIsActive && talkingNPC != null)
        {
            talkingNPC.TurnToPlayer(transform.position);
        }
    }

    // 与 NPC 对话：切换到对话相机，聚焦玩家与 NPC（由 NPC.Interact 调用）
    public void StartNPCDialogue(NPC npc)
    {
        if (dialogueIsActive) return;

        if (npc == null || targetGroup == null || targetGroup.m_Targets.Length < 2)
        {
            Debug.LogWarning("Dialogue requires an NPC and a Cinemachine Target Group with at least two targets.", this);
            return;
        }

        dialogueIsActive = true;
        talkingNPC = npc;
        thirdPersonController.enabled = false;

        targetGroup.m_Targets[0].target = transform;
        targetGroup.m_Targets[1].target = npc.transform;

        npc.TurnToPlayer(transform.position);
        npc.npcIsTalking = true;

        DialogueCamera.SetActive(true);
        dialogueCanvas.SetActive(true);

        textParser.BeginDialogue(npc.dialogue);
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
        dialogueIsActive = false;
        if (talkingNPC != null) talkingNPC.npcIsTalking = false;
        talkingNPC = null;
        thirdPersonController.enabled = true;

        DialogueCamera.SetActive(false);
        dialogueCanvas.SetActive(false);
    }
}