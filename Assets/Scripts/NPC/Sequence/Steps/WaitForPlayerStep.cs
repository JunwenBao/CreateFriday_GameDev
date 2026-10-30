using System;
using System.Collections;
using UnityEngine;

// 等玩家走到 NPC 附近再继续，等待期间可让 NPC 一直看向玩家
[Serializable]
public class WaitForPlayerStep : SequenceStep
{
    [Tooltip("玩家与 NPC 的水平距离小于该值时继续（米）")]
    [SerializeField, Min(0f)] private float distance = 3f;
    [SerializeField] private bool facePlayer = true;

    public override IEnumerator Run(SequenceContext context)
    {
        while (true)
        {
            Vector3 offset = context.PlayerTransform.position - context.Npc.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= distance * distance) yield break;

            if (facePlayer) context.Movement.TurnTowards(offset);
            yield return null;
        }
    }
}
