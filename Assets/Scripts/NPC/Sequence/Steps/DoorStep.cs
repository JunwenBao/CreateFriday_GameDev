using System;
using System.Collections;
using UnityEngine;

// 打开或关闭一扇 SwingDoor。放进 ParallelStep 与开门动画同时执行，
// 并用 Animation Event 或 Delay 对齐“手碰到门”的时机。
[Serializable]
public class DoorStep : SequenceStep
{
    public enum DoorAction { Open, Close }

    [SerializeField] private SwingDoor door;
    [SerializeField] private DoorAction action = DoorAction.Open;
    [Tooltip("先等 NPC 动画发出这个 Animation Event（Function 填 OnAnimationEvent，String 填此名）；留空则不等")]
    [SerializeField] private string animationEvent;
    [Tooltip("等待 Animation Event 的最长时间（秒），超时后照常开关门")]
    [SerializeField, Min(0f)] private float eventTimeout = 5f;
    [Tooltip("（收到 Animation Event 后）再等待的秒数")]
    [SerializeField, Min(0f)] private float delay;
    [Tooltip("等门完全打开/关上再完成此步骤")]
    [SerializeField] private bool waitUntilFinished;

    [NonSerialized] private NPC listeningNpc;
    [NonSerialized] private bool eventReceived;

    public override IEnumerator Run(SequenceContext context)
    {
        if (door == null)
        {
            Debug.LogWarning($"{context.Npc.name}: DoorStep has no door, skipped.", context.Npc);
            yield break;
        }

        if (!string.IsNullOrEmpty(animationEvent))
        {
            StartListening(context.Npc);
            float timer = 0f;
            while (!eventReceived && timer < eventTimeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            StopListening();

            if (!eventReceived)
                Debug.LogWarning($"{context.Npc.name}: Animation Event '{animationEvent}' not received within {eventTimeout}s, operating door anyway.", context.Npc);
        }

        if (delay > 0f) yield return new WaitForSeconds(delay);

        door.SetOpen(action == DoorAction.Open);

        if (waitUntilFinished)
        {
            while (door.IsMoving) yield return null;
        }
    }

    public override void Cancel(SequenceContext context) => StopListening();

    private void StartListening(NPC npc)
    {
        StopListening();
        eventReceived = false;
        listeningNpc = npc;
        listeningNpc.AnimationEventFired += OnAnimationEvent;
    }

    private void StopListening()
    {
        if (listeningNpc != null) listeningNpc.AnimationEventFired -= OnAnimationEvent;
        listeningNpc = null;
    }

    private void OnAnimationEvent(string eventName)
    {
        if (eventName == animationEvent) eventReceived = true;
    }
}
