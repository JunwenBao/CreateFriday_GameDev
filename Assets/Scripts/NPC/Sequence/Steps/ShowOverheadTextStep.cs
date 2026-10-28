using System;
using System.Collections;
using UnityEngine;

// 在 NPC 头顶显示一段提示文本（始终朝向镜头）；文本和 Key 都留空时隐藏头顶文本。
// 可同时在 NPC 的对话动画层播放一个动作（如挥手、指路）。
// NPC 上没有 NPCOverheadText 时会自动添加一个（使用默认设置）。
[Serializable]
public class ShowOverheadTextStep : SequenceStep
{
    [Tooltip("文本所在的 String Table Collection 名称")]
    [SerializeField] private string tableName = DialogueData.DefaultTableName;
    [Tooltip("本地化 Key；留空则直接显示下面的原文")]
    [SerializeField] private string key;
    [SerializeField, TextArea] private string sourceText;
    [Tooltip("显示时长（秒），0 表示一直显示，直到之后的步骤或代码将其替换/隐藏")]
    [SerializeField, Min(0f)] private float duration = 3f;
    [Tooltip("显示文本时同时播放的动作：Animator 中的 Trigger 或 Bool 参数名，由 NPC 的对话动画层播放；留空则不播放")]
    [SerializeField] private string animParameter;
    [Tooltip("等显示时长结束（以及动作播完）再进入下一步；否则显示后立即继续（时长为 0 时只等动作）")]
    [SerializeField] private bool waitUntilHidden;

    public override IEnumerator Run(SequenceContext context)
    {
        NPC npc = context.Npc;
        NPCOverheadText overhead = npc.GetComponent<NPCOverheadText>();
        if (overhead == null) overhead = npc.gameObject.AddComponent<NPCOverheadText>();

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
        NPCOverheadText overhead = context.Npc.GetComponent<NPCOverheadText>();
        if (overhead != null) overhead.Hide();
    }
}
