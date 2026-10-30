using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 同时执行多个子步骤，全部完成后才进入下一步。
// 例如：一边播放开门动画（PlayAnimationStep），一边打开门（DoorStep）。
[Serializable]
public class ParallelStep : SequenceStep
{
    [SerializeReference] private List<SequenceStep> steps = new List<SequenceStep>();

    [NonSerialized] private SequenceStep[] running;
    [NonSerialized] private Coroutine[] coroutines;
    [NonSerialized] private bool[] finished;

    internal List<SequenceStep> Steps => steps;

    public override IEnumerator Run(SequenceContext context)
    {
        // 复制一份：运行中在 Inspector 修改列表不影响本次执行
        SequenceStep[] children = steps.ToArray();
        bool[] done = new bool[children.Length];
        running = children;
        finished = done;
        coroutines = new Coroutine[children.Length];

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null) done[i] = true;
            else coroutines[i] = context.Host.StartCoroutine(RunChild(children[i], context, done, i));
        }

        while (Array.IndexOf(done, false) >= 0) yield return null;

        running = null;
        coroutines = null;
        finished = null;
    }

    public override void Cancel(SequenceContext context)
    {
        if (running == null) return;

        for (int i = 0; i < running.Length; i++)
        {
            if (finished[i]) continue;
            if (coroutines[i] != null && context.Host != null) context.Host.StopCoroutine(coroutines[i]);
            running[i].Cancel(context);
        }

        running = null;
        coroutines = null;
        finished = null;
    }

    private static IEnumerator RunChild(SequenceStep step, SequenceContext context, bool[] done, int index)
    {
        yield return step.Run(context);
        done[index] = true;
    }
}
