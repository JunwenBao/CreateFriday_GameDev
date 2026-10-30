using System;
using System.Collections;
using UnityEngine;

// 原地等待一段时间
[Serializable]
public class WaitStep : SequenceStep
{
    [SerializeField, Min(0f)] private float seconds = 1f;

    public override IEnumerator Run(SequenceContext context)
    {
        yield return new WaitForSeconds(seconds);
    }
}
