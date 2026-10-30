using System;
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

    [Header("Dialogue")]
    [Tooltip("对话时转身面向玩家；坐着或靠在固定位置的 NPC 可关闭")]
    [SerializeField] private bool turnToPlayerWhenTalking = true;

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

    // 演出（NPCSequence）进行中时锁定，玩家不能与其对话
    public bool InteractionLocked { get; set; }
    public override bool CanInteract => !InteractionLocked;

    // PlayDialogueAnimation 播放的动作尚未结束（含渐出）
    public bool IsPlayingDialogueAnimation => dialogueAnimRoutine != null;

    // 动画片段上的 Animation Event 触发时广播事件名，供演出步骤对齐时机（如手碰到门把时开门）
    public event Action<string> AnimationEventFired;

    // Animation Event 的 Function 填 OnAnimationEvent，String 填事件名
    public void OnAnimationEvent(string eventName) => AnimationEventFired?.Invoke(eventName);

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
        if (!CanInteract) return;

        if (player.DialogueTrigger != null)
        {
            SetHighlight(false);
            player.DialogueTrigger.StartNPCDialogue(this);
        }
    }

    // 在对话动画层播放 paramName（Trigger 或 Bool），播放完后回到基础层；动画层或参数不存在时返回 false
    public bool PlayDialogueAnimation(string paramName)
    {
        if (animator == null) return false;
        if (dialogueLayerIndex >= animator.layerCount)
        {
            Debug.LogWarning($"Animator on {name} has no layer {dialogueLayerIndex} for dialogue animations.", this);
            return false;
        }

        AnimatorControllerParameterType? type = null;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) { type = p.type; break; }
        }
        if (type == null)
        {
            Debug.LogWarning($"Animator on {name} has no parameter '{paramName}'.", this);
            return false;
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
        return true;
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

    // 对话期间由 DialogueTrigger 每帧调用；turnToPlayerWhenTalking 关闭时保持原朝向
    public void TurnToPlayer(Vector3 playerPosition)
    {
        if (!turnToPlayerWhenTalking) return;

        Vector3 direction = (playerPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}