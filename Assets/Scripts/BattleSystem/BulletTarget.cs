using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可被子弹击中的目标。被击中时播放受击效果：模型闪色、冲击力、受击动画、事件回调
/// </summary>
public class BulletTarget : MonoBehaviour
{
    [Header("命中特效")]
    [Tooltip("该目标专用的命中特效，为空时根据 useShooterHitVfx 决定是否使用射击者配置的默认命中特效")]
    [SerializeField] private Transform hitVfxOverride;
    [Tooltip("未设置专用命中特效时，是否使用射击者的默认命中特效（如血花）。瓶子等物体可关闭")]
    [SerializeField] private bool useShooterHitVfx = true;

    [Header("受击闪色")]
    [SerializeField] private bool flashOnHit = true;
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("需要闪色的渲染器，为空时自动收集自身及子物体上的 MeshRenderer / SkinnedMeshRenderer")]
    [SerializeField] private Renderer[] flashRenderers;

    [Header("冲击力")]
    [Tooltip("目标带有非 Kinematic 刚体时，沿子弹方向施加的冲击力")]
    [SerializeField] private float hitForce = 5f;

    [Header("受击动画")]
    [Tooltip("受击时设置的 Animator Trigger 名称，为空则不触发")]
    [SerializeField] private string hitAnimTrigger = "";

    [Header("事件")]
    [Tooltip("被击中时触发，参数为命中点")]
    [SerializeField] private UnityEvent<Vector3> onHit = new UnityEvent<Vector3>();

    /// <summary>被击中时触发（命中点、命中法线、子弹方向）</summary>
    public event Action<Vector3, Vector3, Vector3> Hit;

    /// <summary>
    /// 返回命中该目标时应生成的特效，可能为空
    /// </summary>
    /// <param name="shooterDefaultVfx">射击者配置的默认命中特效</param>
    public Transform ResolveHitVfx(Transform shooterDefaultVfx)
    {
        if (hitVfxOverride != null)
        {
            return hitVfxOverride;
        }
        return useShooterHitVfx ? shooterDefaultVfx : null;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Rigidbody targetRigidbody;
    private Animator targetAnimator;
    private int hitAnimTriggerHash;
    private bool hasHitAnimTrigger;
    private MaterialPropertyBlock[] originalBlocks;
    private MaterialPropertyBlock flashBlock;
    private Coroutine flashRoutine;

    private void Awake()
    {
        targetRigidbody = GetComponentInParent<Rigidbody>();
        targetAnimator = GetComponentInParent<Animator>();

        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = Array.FindAll(GetComponentsInChildren<Renderer>(),
                r => r is MeshRenderer || r is SkinnedMeshRenderer);
        }
        originalBlocks = new MaterialPropertyBlock[flashRenderers.Length];
        flashBlock = new MaterialPropertyBlock();

        if (!string.IsNullOrEmpty(hitAnimTrigger) && targetAnimator != null)
        {
            hitAnimTriggerHash = Animator.StringToHash(hitAnimTrigger);
            foreach (AnimatorControllerParameter param in targetAnimator.parameters)
            {
                if (param.nameHash == hitAnimTriggerHash && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasHitAnimTrigger = true;
                    break;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreFlash();
        }
    }

    /// <summary>
    /// 子弹命中该目标时调用
    /// </summary>
    /// <param name="hitPoint">命中点</param>
    /// <param name="hitNormal">命中表面法线</param>
    /// <param name="shotDirection">子弹飞行方向</param>
    public void TakeHit(Vector3 hitPoint, Vector3 hitNormal, Vector3 shotDirection)
    {
        if (flashOnHit && flashRenderers.Length > 0 && isActiveAndEnabled)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                RestoreFlash();
            }
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (targetRigidbody != null && !targetRigidbody.isKinematic && hitForce > 0f)
        {
            targetRigidbody.AddForceAtPosition(shotDirection.normalized * hitForce, hitPoint, ForceMode.Impulse);
        }

        if (hasHitAnimTrigger)
        {
            targetAnimator.SetTrigger(hitAnimTriggerHash);
        }

        Hit?.Invoke(hitPoint, hitNormal, shotDirection);
        onHit.Invoke(hitPoint);
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer r = flashRenderers[i];
            if (r == null)
            {
                continue;
            }

            // 记录原有的 PropertyBlock，闪色结束后还原
            originalBlocks[i] ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(originalBlocks[i]);

            r.GetPropertyBlock(flashBlock);
            flashBlock.SetColor(BaseColorId, flashColor);
            flashBlock.SetColor(ColorId, flashColor);
            r.SetPropertyBlock(flashBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        RestoreFlash();
    }

    private void RestoreFlash()
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null && originalBlocks[i] != null)
            {
                flashRenderers[i].SetPropertyBlock(originalBlocks[i]);
            }
        }
        flashRoutine = null;
    }
}
