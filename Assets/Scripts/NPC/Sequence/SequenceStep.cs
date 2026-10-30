using System;
using System.Collections;
using UnityEngine;

// 演出中的一个步骤。Run 是协程，返回即表示该步骤完成，NPCSequence 随后执行下一步。
// 新增步骤类型：继承本类并加 [Serializable]，Inspector 的步骤下拉框会自动出现。
// 注意：步骤以类名序列化，改名会让已配置的步骤丢失（可用 [UnityEngine.Scripting.APIUpdating.MovedFrom] 迁移）。
[Serializable]
public abstract class SequenceStep
{
    public abstract IEnumerator Run(SequenceContext context);

    // 演出被中途停止时调用，用于清理该步骤留下的状态（例如停止移动）
    public virtual void Cancel(SequenceContext context) { }
}

// 步骤执行时可用的对象
public class SequenceContext
{
    public NPC Npc { get; }
    public NPCMovement Movement { get; }
    public DialogueTrigger Player { get; }
    public Transform PlayerTransform => Player.transform;
    // 运行演出的组件，需要同时启动多个协程的步骤（如 ParallelStep）用它来 StartCoroutine
    public MonoBehaviour Host { get; }

    public SequenceContext(NPC npc, NPCMovement movement, DialogueTrigger player, MonoBehaviour host)
    {
        Npc = npc;
        Movement = movement;
        Player = player;
        Host = host;
    }
}
