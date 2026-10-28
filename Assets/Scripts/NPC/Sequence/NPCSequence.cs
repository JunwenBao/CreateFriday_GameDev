using System.Collections;
using System.Collections.Generic;
using Game.Story;
using UnityEngine;
using UnityEngine.Events;

// NPC 演出序列：按顺序执行一串步骤（走到点位、播放动作、等待玩家、开始对话……），
// 每一步完成后才进入下一步。由 Start Trigger 指定的玩法事件自动启动，或由其他脚本调用 Play()。
// 演出期间会关闭 NPCPatrol，并可锁定玩家与该 NPC 的互动。
public class NPCSequence : MonoBehaviour
{
    [Tooltip("执行演出的 NPC，留空则使用本物体上的 NPC")]
    [SerializeField] private NPC npc;
    [Tooltip("留空则自动查找场景中的玩家（DialogueTrigger）")]
    [SerializeField] private DialogueTrigger player;

    [Header("Start")]
    [Tooltip("自动启动条件，例如某段对话结束、玩家进入区域。选 None 时只能由代码调用 Play()")]
    [SerializeReference] private StoryTrigger startTrigger;
    [SerializeField] private bool playOnce = true;

    [Header("Control")]
    [Tooltip("演出期间玩家不能与该 NPC 互动")]
    [SerializeField] private bool lockInteraction = true;
    [Tooltip("演出结束后恢复 NPCPatrol（仅当演出前它是开启的）")]
    [SerializeField] private bool resumePatrolOnFinish;

    [Header("Steps")]
    [SerializeReference] private List<SequenceStep> steps = new List<SequenceStep>();

    [Header("Events")]
    public UnityEvent onStarted;
    public UnityEvent onFinished;

    private SequenceContext context;
    private Coroutine routine;
    private SequenceStep currentStep;
    private NPCPatrol patrol;
    private bool patrolWasEnabled;
    private bool isPlaying;
    private bool hasPlayed;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (npc == null) npc = GetComponent<NPC>();
    }

    private void OnEnable()
    {
        startTrigger?.Bind(OnStartTriggerFired);
    }

    private void OnDisable()
    {
        startTrigger?.Unbind();
        Stop();
    }

    private void OnStartTriggerFired() => Play();

    // 开始演出；已在播放、已播过（Play Once）或缺少必要组件时返回 false
    public bool Play()
    {
        if (IsPlaying || (playOnce && hasPlayed)) return false;
        if (!isActiveAndEnabled || !TryCreateContext()) return false;

        hasPlayed = true;
        isPlaying = true;
        routine = StartCoroutine(Run());
        return true;
    }

    // 中途停止演出，NPC 停在当前位置
    public void Stop()
    {
        if (!isPlaying) return;

        if (routine != null) StopCoroutine(routine);
        routine = null;
        isPlaying = false;
        currentStep?.Cancel(context);
        currentStep = null;
        ReleaseControl();
    }

    private IEnumerator Run()
    {
        TakeControl();
        onStarted?.Invoke();

        // 用下标遍历：运行中在 Inspector 修改步骤不会让枚举器抛异常
        for (int i = 0; i < steps.Count; i++)
        {
            SequenceStep step = steps[i];
            if (step == null) continue;
            currentStep = step;
            yield return step.Run(context);
        }

        currentStep = null;
        routine = null;
        isPlaying = false;
        ReleaseControl();
        onFinished?.Invoke();
    }

    private bool TryCreateContext()
    {
        if (npc == null)
        {
            Debug.LogWarning($"{name}: NPCSequence has no NPC assigned.", this);
            return false;
        }

        NPCMovement movement = npc.GetComponent<NPCMovement>();
        if (movement == null)
        {
            Debug.LogWarning($"{npc.name} has no NPCMovement, cannot play sequence.", this);
            return false;
        }

        if (player == null) player = FindObjectOfType<DialogueTrigger>();
        if (player == null)
        {
            Debug.LogWarning($"{name}: no player DialogueTrigger found in the scene.", this);
            return false;
        }

        context = new SequenceContext(npc, movement, player, this);
        return true;
    }

    private void TakeControl()
    {
        // 关闭巡逻，避免它与演出同时下达移动指令（NPCPatrol.OnDisable 会停止当前移动）
        patrol = npc.GetComponent<NPCPatrol>();
        patrolWasEnabled = patrol != null && patrol.enabled;
        if (patrolWasEnabled) patrol.enabled = false;

        if (lockInteraction) npc.InteractionLocked = true;
    }

    private void ReleaseControl()
    {
        if (npc != null && lockInteraction) npc.InteractionLocked = false;
        if (resumePatrolOnFinish && patrolWasEnabled && patrol != null) patrol.enabled = true;
        patrolWasEnabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        DeduplicateSteps(steps, new HashSet<SequenceStep>());
    }

    // Inspector 列表的 "+" 会让新元素与上一个元素共用同一个对象，这里拆成独立副本（含 ParallelStep 的子步骤）
    private static void DeduplicateSteps(List<SequenceStep> list, HashSet<SequenceStep> seen)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            SequenceStep step = list[i];
            if (step == null) continue;
            if (!seen.Add(step))
            {
                step = (SequenceStep)JsonUtility.FromJson(JsonUtility.ToJson(step), step.GetType());
                list[i] = step;
                seen.Add(step);
            }
            if (step is ParallelStep parallel) DeduplicateSteps(parallel.Steps, seen);
        }
    }

    // 选中时画出移动路线：白线为路径，蓝线为到达后的朝向
    private void OnDrawGizmosSelected()
    {
        Transform from = npc != null ? npc.transform : transform;
        Vector3 last = from.position;

        foreach (SequenceStep step in steps)
        {
            if (step is not MoveToStep move || move.Target == null) continue;

            Vector3 p = move.Target.position;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(last, p);
            Gizmos.DrawWireSphere(p, 0.25f);
            if (move.FaceTargetForward)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(p, p + move.Target.forward * 0.8f);
            }
            last = p;
        }
    }
#endif
}
