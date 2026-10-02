using System.Collections.Generic;
using UnityEngine;

// 挂在敌人头部的 sphere 上，用于标记辅助瞄准（磁吸）目标
public class AimAssistTarget : MonoBehaviour
{
    public static readonly List<AimAssistTarget> Targets = new List<AimAssistTarget>();

    private void OnEnable()
    {
        Targets.Add(this);
    }

    private void OnDisable()
    {
        Targets.Remove(this);
    }
}