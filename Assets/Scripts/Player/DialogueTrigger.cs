using System.Collections;
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

    [Tooltip("Leave empty to use the CinemachineBrain on Camera.main.")]
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private bool hardCutDialogueCamera = true;

    private CinemachineBlendDefinition originalBlend;
    private bool blendOverridden;
    private Coroutine restoreBlendRoutine;

    [Header("Dialogue")]
    [Tooltip("对话流程控制器，对话框界面由它通过 UIManager 打开 DialogueWindow。留空则使用本物体上的 TextParser")]
    [SerializeField] private TextParser textParser;

    public bool IsDialogueActive => dialogueIsActive;
    // 结束对话的那一下 E 键不能被 PlayerInteraction 当作新的互动
    public bool DialogueEndedThisFrame => Time.frameCount == dialogueEndedFrame;

    private int dialogueEndedFrame = -1;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();

        if (cameraBrain == null && Camera.main != null)
            cameraBrain = Camera.main.GetComponent<CinemachineBrain>();

        if (textParser == null)
            textParser = GetComponent<TextParser>();

        if (textParser == null)
        {
            Debug.LogError("Text Parser is not assigned on DialogueTrigger.", this);
            return;
        }

        textParser.onConversationEnd.AddListener(EndDialogue);
        textParser.LineStarted += OnLineStarted;
    }

    private void OnDestroy()
    {
        if (textParser != null)
            textParser.LineStarted -= OnLineStarted;
    }

    private void OnLineStarted(DialogueLine line)
    {
        if (talkingNPC != null && !string.IsNullOrEmpty(line.animTrigger))
            talkingNPC.PlayDialogueAnimation(line.animTrigger);
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

        if (npc == null) return;

        bool useFallbackCamera = !npc.HasDialogueCamera;
        if (useFallbackCamera && (targetGroup == null || targetGroup.m_Targets.Length < 2))
        {
            Debug.LogWarning("NPC has no dialogue camera and the fallback Target Group is not set up.", this);
            return;
        }

        dialogueIsActive = true;
        talkingNPC = npc;
        thirdPersonController.enabled = false;

        npc.TurnToPlayer(transform.position);
        npc.npcIsTalking = true;

        BeginHardCut();

        if (useFallbackCamera)
        {
            targetGroup.m_Targets[0].target = transform;
            targetGroup.m_Targets[1].target = npc.transform;
            DialogueCamera.SetActive(true);
        }
        else
        {
            npc.SetDialogueCameraActive(true);
        }

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

        textParser.BeginDialogue(dialogue);
    }

    private void EndDialogue()
    {
        dialogueIsActive = false;
        dialogueEndedFrame = Time.frameCount;
        if (talkingNPC != null)
        {
            talkingNPC.npcIsTalking = false;
            talkingNPC.SetDialogueCameraActive(false);
        }
        talkingNPC = null;
        thirdPersonController.enabled = true;

        DialogueCamera.SetActive(false);

        if (blendOverridden)
            restoreBlendRoutine = StartCoroutine(RestoreBlendAfterCut());
    }

    private void BeginHardCut()
    {
        if (!hardCutDialogueCamera || cameraBrain == null) return;

        if (restoreBlendRoutine != null)
        {
            StopCoroutine(restoreBlendRoutine);
            restoreBlendRoutine = null;
        }

        if (!blendOverridden)
        {
            originalBlend = cameraBrain.m_DefaultBlend;
            blendOverridden = true;
        }
        cameraBrain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
    }

    // Brain picks up the camera change in LateUpdate, so keep Cut until the next frame.
    private IEnumerator RestoreBlendAfterCut()
    {
        yield return null;
        cameraBrain.m_DefaultBlend = originalBlend;
        blendOverridden = false;
        restoreBlendRoutine = null;
    }
}