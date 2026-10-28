using System;
using System.Collections;
using UnityEngine;

// 寻路走到目标点位；可选到达后转向点位的朝向（蓝色 Z 轴），例如面朝门以便敲门
[Serializable]
public class MoveToStep : SequenceStep
{
    [Tooltip("目标点位，建议用场景中的空物体")]
    [SerializeField] private Transform target;
    [Tooltip("到达后转向目标点位的蓝色 Z 轴方向；不勾选则保持走过来时的朝向")]
    [SerializeField] private bool faceTargetForward;

    public Transform Target => target;
    public bool FaceTargetForward => faceTargetForward;

    public override IEnumerator Run(SequenceContext context)
    {
        if (target == null)
        {
            Debug.LogWarning($"{context.Npc.name}: MoveToStep has no target, skipped.", context.Npc);
            yield break;
        }

        NPCMovement movement = context.Movement;
        if (!movement.MoveTo(target.position)) yield break;

        // 等一帧让 NavMeshAgent 更新路径，否则 remainingDistance 可能仍是旧值
        yield return null;
        while (!movement.HasArrived) yield return null;
        movement.Stop();

        if (faceTargetForward)
        {
            while (!movement.TurnTowards(target.forward)) yield return null;
        }
    }

    public override void Cancel(SequenceContext context) => context.Movement.Stop();
}
