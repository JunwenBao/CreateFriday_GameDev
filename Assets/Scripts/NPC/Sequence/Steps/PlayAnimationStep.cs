using System;
using System.Collections;
using UnityEngine;

// 在 NPC 的对话动画层播放一个动作（如敲门、挥手），等动作播完再继续
[Serializable]
public class PlayAnimationStep : SequenceStep
{
    [Tooltip("Animator 中的 Trigger 或 Bool 参数名，由 NPC 的对话动画层（Dialogue Layer Index）播放")]
    [SerializeField] private string animParameter = "Knock";
    [Tooltip("Animator 中没有该参数或动画层时，原地停顿这么久作为占位（秒）")]
    [SerializeField] private float fallbackDuration = 1.5f;

    public override IEnumerator Run(SequenceContext context)
    {
        if (!context.Npc.PlayDialogueAnimation(animParameter))
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        while (context.Npc.IsPlayingDialogueAnimation) yield return null;
    }
}
