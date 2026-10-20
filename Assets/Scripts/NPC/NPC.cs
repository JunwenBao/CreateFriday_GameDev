using System.Collections;
using UnityEngine;
using Cinemachine;

public class NPC : InteractableBase
{
    public NPCData data;
    public DialogueData dialogue;
    public bool npcIsTalking;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera dialogueCamera;

    [Header("Dialogue Animation")]
    [SerializeField] private int dialogueLayerIndex = 1;
    [Tooltip("Safety limit in case the layer never leaves its default state.")]
    [SerializeField] private float dialogueAnimEnterTimeout = 1f;
    [SerializeField] private float dialogueLayerFadeDuration = 0.25f;

    private Animator animator;
    private Coroutine dialogueAnimRoutine;
    private string activeBoolParam;
    private int dialogueIdleHash;

    public bool HasDialogueCamera => dialogueCamera != null;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        SetDialogueCameraActive(false);
    }

    // 控制NPC对话相机的激活状态
    public void SetDialogueCameraActive(bool active)
    {
        if (dialogueCamera != null)
            dialogueCamera.gameObject.SetActive(active);
    }

    public override void Interact(PlayerInteraction player)
    {
        if (player.DialogueTrigger != null)
        {
            SetHighlight(false);
            player.DialogueTrigger.StartNPCDialogue(this);
        }
    }

    // 在对话动画层播放 paramName（Trigger 或 Bool），播放完后回到基础层
    public void PlayDialogueAnimation(string paramName)
    {
        if (animator == null || dialogueLayerIndex >= animator.layerCount) return;

        AnimatorControllerParameterType? type = null;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) { type = p.type; break; }
        }
        if (type == null)
        {
            Debug.LogWarning($"Animator on {name} has no parameter '{paramName}'.", this);
            return;
        }

        if (dialogueAnimRoutine != null)
        {
            // Interrupted: keep the current weight so the new fade continues smoothly.
            StopCoroutine(dialogueAnimRoutine);
            if (activeBoolParam != null)
            {
                animator.SetBool(activeBoolParam, false);
                activeBoolParam = null;
            }
        }
        else
        {
            dialogueIdleHash = animator.GetCurrentAnimatorStateInfo(dialogueLayerIndex).fullPathHash;
        }

        dialogueAnimRoutine = StartCoroutine(DialogueAnimRoutine(paramName, type.Value));
    }

    private void StepLayerWeight(int layer, float target)
    {
        float step = dialogueLayerFadeDuration > 0f ? Time.deltaTime / dialogueLayerFadeDuration : 1f;
        animator.SetLayerWeight(layer, Mathf.MoveTowards(animator.GetLayerWeight(layer), target, step));
    }

    private IEnumerator DialogueAnimRoutine(string paramName, AnimatorControllerParameterType type)
    {
        int layer = dialogueLayerIndex;
        int idleHash = dialogueIdleHash;

        if (type == AnimatorControllerParameterType.Bool)
        {
            animator.SetBool(paramName, true);
            activeBoolParam = paramName;
        }
        else
        {
            animator.SetTrigger(paramName);
        }

        // 渐入并等待进入目标动画状态
        float timer = 0f;
        yield return null;
        while (animator.GetCurrentAnimatorStateInfo(layer).fullPathHash == idleHash && timer < dialogueAnimEnterTimeout)
        {
            StepLayerWeight(layer, 1f);
            timer += Time.deltaTime;
            yield return null;
        }

        // 播放中继续渐入，剩余时间不足一个渐变时长时开始渐出
        while (true)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.fullPathHash == idleHash) break;
            if (!animator.IsInTransition(layer))
            {
                float remaining = (1f - info.normalizedTime) * info.length;
                if (remaining <= dialogueLayerFadeDuration) break;
            }
            StepLayerWeight(layer, 1f);
            yield return null;
        }

        if (activeBoolParam != null)
        {
            animator.SetBool(activeBoolParam, false);
            activeBoolParam = null;
        }
        if (type == AnimatorControllerParameterType.Trigger)
            animator.ResetTrigger(paramName);

        while (animator.GetLayerWeight(layer) > 0f)
        {
            StepLayerWeight(layer, 0f);
            yield return null;
        }
        dialogueAnimRoutine = null;
    }

    public void TurnToPlayer(Vector3 playerPosition)
    {
        Vector3 direction = (playerPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}