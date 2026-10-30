using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

// 由走/跑/蹲走动画片段上的 Animation Event（Function 填 PlayFootstep）在脚落地的帧触发。
// 事件可用菜单 Assets > Audio > Generate Footstep Events 自动生成，再在 FBX 的 Animation 页签里微调
public class FootstepAudio : MonoBehaviour
{
    public const string AnimationEventFunction = nameof(PlayFootstep);

    [Header("Wwise")]
    public AK.Wwise.Event footstepEvent;

    [Header("Footstep Settings")]
    [Tooltip("片段的实际权重（混合树权重 × 动画层权重）高于该值才播放，避免混合/切层时多个片段重复触发")]
    [Range(0f, 1f)] public float minClipWeight = 0.5f;

    [Tooltip("两次脚步声之间的最小间隔（秒），防止动画过渡时重复触发")]
    public float minStepInterval = 0.1f;

    private Animator animator;
    private CharacterController characterController;
    private ThirdPersonController thirdPersonController;
    private readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>();
    private float lastStepTime = float.NegativeInfinity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    public void PlayFootstep(AnimationEvent animationEvent)
    {
        if (!isActiveAndEnabled || !IsGrounded()) return;
        if (GetEffectiveWeight(animationEvent.animatorClipInfo) <= minClipWeight) return;
        if (Time.time - lastStepTime < minStepInterval) return;

        lastStepTime = Time.time;
        footstepEvent?.Post(gameObject);
    }

    // 事件自带的权重只反映混合树内的权重，还要乘上片段所在动画层的有效权重。
    // 当前 Animator 的上层都是无遮罩的 Override 层，下层会被每个上层按 (1 - 上层权重) 压低
    private float GetEffectiveWeight(AnimatorClipInfo info)
    {
        if (animator == null || info.clip == null) return info.weight;

        float layerWeight = -1f;
        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            if (!LayerContainsClip(layer, info.clip)) continue;

            float weight = layer == 0 ? 1f : animator.GetLayerWeight(layer);
            for (int upper = layer + 1; upper < animator.layerCount; upper++)
            {
                weight *= 1f - animator.GetLayerWeight(upper);
            }
            layerWeight = Mathf.Max(layerWeight, weight);
        }

        return layerWeight < 0f ? info.weight : info.weight * layerWeight;
    }

    private bool LayerContainsClip(int layer, AnimationClip clip)
    {
        animator.GetCurrentAnimatorClipInfo(layer, clipInfos);
        foreach (var clipInfo in clipInfos)
        {
            if (clipInfo.clip == clip) return true;
        }

        animator.GetNextAnimatorClipInfo(layer, clipInfos);
        foreach (var clipInfo in clipInfos)
        {
            if (clipInfo.clip == clip) return true;
        }

        return false;
    }

    private bool IsGrounded()
    {
        if (thirdPersonController != null) return thirdPersonController.Grounded;
        if (characterController != null) return characterController.isGrounded;
        return true;
    }
}
