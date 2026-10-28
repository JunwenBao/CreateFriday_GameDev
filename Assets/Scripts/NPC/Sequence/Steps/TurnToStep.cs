using System;
using System.Collections;
using UnityEngine;

// 原地转向目标物体；目标留空时转向玩家
[Serializable]
public class TurnToStep : SequenceStep
{
    [Tooltip("留空则转向玩家")]
    [SerializeField] private Transform target;

    public override IEnumerator Run(SequenceContext context)
    {
        Transform look = target != null ? target : context.PlayerTransform;
        while (!context.Movement.TurnTowards(look.position - context.Npc.transform.position))
            yield return null;
    }
}
