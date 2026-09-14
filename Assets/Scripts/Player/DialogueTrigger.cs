using UnityEngine;
using StarterAssets;
using Cinemachine;

public class DialogueTrigger : MonoBehaviour
{
    private NPC currentNPC;
    private ThirdPersonController thirdPersonController;
    private bool dialogueIsActive;
    [SerializeField] private CinemachineTargetGroup targetGroup;

    [SerializeField] private GameObject DialogueCamera;

    [Header("UI")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TextParser textParser;

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

    private void StartDialogue()
    {
        if (currentNPC == null || targetGroup == null || targetGroup.m_Targets.Length < 2)
        {
            Debug.LogWarning("Dialogue requires an NPC and a Cinemachine Target Group with at least two targets.", this);
            return;
        }

        dialogueIsActive = true;
        thirdPersonController.enabled = false;

        targetGroup.m_Targets[0].target = transform;
        targetGroup.m_Targets[1].target = currentNPC.transform;

        currentNPC.TurnToPlayer(transform.position);
        currentNPC.npcIsTalking = true;

        DialogueCamera.SetActive(true);
        dialogueCanvas.SetActive(true);

        textParser.BeginDialogue(currentNPC.dialogue);
    }

    private void EndDialogue()
    {
        dialogueIsActive = false;
        currentNPC.npcIsTalking = false;
        thirdPersonController.enabled = true;

        DialogueCamera.SetActive(false);
        dialogueCanvas.SetActive(false);
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