using System;
using System.Collections;
using UnityEngine;

// 在 NPC 头顶显示一段提示文本（始终朝向镜头）；文本和 Key 都留空时隐藏头顶文本。
// 可同时在 NPC 的对话动画层播放一个动作（如挥手、指路）。
// 指定 Speaker 可让其他 NPC 说话：多个本步骤依次排列并勾选 Wait Until Hidden，即可形成 NPC 之间的对话。
// NPC 上没有 NPCOverheadText 时会自动添加一个（使用默认设置）。
[Serializable]
public class ShowOverheadTextStep : SequenceStep
{
    [Tooltip("说这句话的 NPC，留空则为本演出的 NPC")]
    [SerializeField] private NPC speaker;
    [Tooltip("文本所在的 String Table Collection 名称")]
    [SerializeField] private string tableName = DialogueData.DefaultTableName;
    [Tooltip("本地化 Key；留空则直接显示下面的原文")]
    [SerializeField] private string key;
    [SerializeField, TextArea] private string sourceText;
    [Tooltip("显示时长（秒），0 表示一直显示，直到之后的步骤或代码将其替换/隐藏")]
    [SerializeField, Min(0f)] private float duration = 3f;
    [Tooltip("显示文本时同时播放的动作：Animator 中的 Trigger 或 Bool 参数名，由 NPC 的对话动画层播放；留空则不播放")]
    [SerializeField] private string animParameter;

    [Tooltip("说话前先转身面向听者")]
    [SerializeField] private bool faceListener;
    [Tooltip("听者，留空时：说话者不是本演出的 NPC 则面向本演出的 NPC")]
    [SerializeField] private Transform listener;
    [Tooltip("转身速度（度/秒）")]
    [SerializeField, Min(1f)] private float turnSpeed = 360f;

    [Tooltip("等显示时长结束（以及动作播完）再进入下一步；否则显示后立即继续（时长为 0 时只等动作）")]
    [SerializeField] private bool waitUntilHidden;

    [NonSerialized] private NPCOverheadText activeOverhead;

    public override IEnumerator Run(SequenceContext context)
    {
        NPC npc = speaker != null ? speaker : context.Npc;

        if (faceListener)
        {
            Transform target = ResolveListener(context, npc);
            if (target != null) yield return TurnTowards(npc, target);
        }

        NPCOverheadText overhead = npc.GetComponent<NPCOverheadText>();
        if (overhead == null) overhead = npc.gameObject.AddComponent<NPCOverheadText>();
        activeOverhead = overhead;

        if (!string.IsNullOrEmpty(key)) overhead.ShowLocalized(tableName, key, duration);
        else overhead.Show(sourceText, duration);

        bool playingAnim = !string.IsNullOrEmpty(animParameter) && npc.PlayDialogueAnimation(animParameter);

        if (!waitUntilHidden) yield break;

        // 文本和动作都结束才继续，避免下一句的动作打断这一句还没播完的动作
        while ((duration > 0f && overhead.IsShowing) || (playingAnim && npc.IsPlayingDialogueAnimation))
            yield return null;
    }

    public override void Cancel(SequenceContext context)
    {
        if (activeOverhead != null) activeOverhead.Hide();
        activeOverhead = null;
    }

    private Transform ResolveListener(SequenceContext context, NPC npc)
    {
        if (listener != null) return listener;
        if (npc != context.Npc) return context.Npc.transform;

        Debug.LogWarning($"{npc.name}: ShowOverheadTextStep has Face Listener on but no Listener, skipped turning.", npc);
        return null;
    }

    private IEnumerator TurnTowards(NPC npc, Transform target)
    {
        Transform t = npc.transform;
        // 与玩家对话时由 DialogueTrigger 控制朝向，正在寻路时由 NavMeshAgent 控制朝向，
        // 两者都可能让朝向一直对不准，所以限制最长转身时间，避免演出卡住
        float maxTime = 180f / turnSpeed + 0.5f;

        for (float time = 0f; time < maxTime && !npc.npcIsTalking; time += Time.deltaTime)
        {
            Vector3 direction = target.position - t.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) yield break;

            Quaternion look = Quaternion.LookRotation(direction);
            t.rotation = Quaternion.RotateTowards(t.rotation, look, turnSpeed * Time.deltaTime);
            if (Quaternion.Angle(t.rotation, look) <= 1f) yield break;
            yield return null;
        }
    }
}
