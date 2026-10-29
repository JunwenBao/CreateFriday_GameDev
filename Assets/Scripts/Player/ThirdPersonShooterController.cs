using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.Animations.Rigging;

public class ThirdPersonShooterController : MonoBehaviour
{
    [Header("相机控制")]
    [SerializeField] private Rig aimRig;
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity = 1f;
    [SerializeField] private float aimSensitivity = 1f;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawBulletPosition;
    [SerializeField] private Transform vfxHitRed;

    [Header("准星UI")]
    [SerializeField] private GameObject crossHairUI;                // 十字准心UI

    [Header("武器控制")]
    [SerializeField] private float fireRate = 5f;                   // 武器射速
    [SerializeField] private GameObject weaponObjectHand;           // 武器对象
    [SerializeField] private GameObject weaponObjectHolster;        // 收枪位置的武器对象
    [SerializeField] private ParticleSystem muzzleSmokeParticle;    // 枪口烟雾特效

    [Header("辅助瞄准")]
    [SerializeField] private bool aimAssistEnabled = true;          // 是否启用辅助瞄准
    [SerializeField] private float aimAssistScreenRadius = 15f;     // 准星进入目标此屏幕像素半径内触发吸附
    [SerializeField] private float aimAssistSnapSpeed = 15f;        // 准星吸附/回中的平滑速度

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private Animator animator;
    private float aimRigWeight;
    private float nextFireTime;
    private bool wasMoving;
    private bool wasAiming;
    private bool isWeaponDrawn;
    private float weaponSwitchPendingTimer;                         // 按下 Q 后、动画机尚未进入拔/收枪状态前的移动锁定缓冲

    private const float WeaponSwitchPendingDuration = 0.2f;

    private Transform hitTransform = null;
    private RectTransform crossHairRectTransform;                    // 十字准心的 RectTransform，用于在屏幕上移动准心

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();

        // 获取十字准心的 RectTransform，用于在屏幕上移动准心
        if (crossHairUI != null)
        {
            crossHairRectTransform = crossHairUI.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        Vector3 mouseWorldPosition = Vector3.zero;

        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask))
        {
            debugTransform.position = hit.point;
            mouseWorldPosition = hit.point;
            hitTransform = hit.transform;
        }

        bool isMoving = starterAssetsInputs.move.sqrMagnitude > 0.01f;

        // 按下 Q 切换拔枪/收枪状态
        if (starterAssetsInputs.drawWeapon)
        {
            isWeaponDrawn = !isWeaponDrawn;
            starterAssetsInputs.drawWeapon = false;

            animator.SetBool("IsArmed", isWeaponDrawn);
            weaponSwitchPendingTimer = WeaponSwitchPendingDuration;
        }

        // 拔枪/收枪动画播放期间禁止移动
        bool isSwitchingWeapon = IsPlayingWeaponSwitchAnimation();
        if (isSwitchingWeapon)
        {
            weaponSwitchPendingTimer = 0f;
        }
        else if (weaponSwitchPendingTimer > 0f)
        {
            weaponSwitchPendingTimer -= Time.deltaTime;
        }
        thirdPersonController.SetCanMove(!isSwitchingWeapon && weaponSwitchPendingTimer <= 0f);

        // 只有拔枪后，按住鼠标右键才会进入瞄准
        bool isAiming = isWeaponDrawn && starterAssetsInputs.aim;

        if (isAiming)
        {
            crossHairUI.SetActive(true);

            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetRotateOnMove(false);

            aimRigWeight = 1f;

            // 辅助瞄准：准星靠近头部 sphere 时自动吸附到其中心
            Vector3 crossHairTargetPos = screenCenterPoint;
            if (aimAssistEnabled &&
                TryGetAimAssistTarget(screenCenterPoint, out AimAssistTarget assistTarget, out Vector3 assistScreenPos))
            {
                crossHairTargetPos = assistScreenPos;
                mouseWorldPosition = assistTarget.transform.position;
                hitTransform = assistTarget.transform;
            }

            if (crossHairRectTransform != null)
            {
                crossHairRectTransform.position = Vector3.Lerp(
                    crossHairRectTransform.position, crossHairTargetPos, Time.deltaTime * aimAssistSnapSpeed);
            }

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

            animator.SetBool("IsAiming", true);
        }
        else
        {
            crossHairUI.SetActive(false);
            starterAssetsInputs.shoot = false;

            // 退出瞄准时把准星复位到屏幕中心，下次进入瞄准不会从旧的吸附位置漂移
            if (crossHairRectTransform != null)
            {
                crossHairRectTransform.position = screenCenterPoint;
            }

            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
            thirdPersonController.SetRotateOnMove(true);

            aimRigWeight = 0f;

            if (!isMoving && !thirdPersonController.IsCrouch && (wasMoving || wasAiming))
            {
                animator.Play("Idle Walk Run Blend", 0, 0f);
            }

            animator.SetBool("IsAiming", false);
        }

        // 上半身层（layer 1）权重：只要动画没回到 Unarmed 空手状态就保持，收枪动画才能完整播放
        AnimatorStateInfo upperBodyState = animator.GetCurrentAnimatorStateInfo(1);
        bool isUnarmed = upperBodyState.IsName("Unarmed") && !animator.IsInTransition(1);
        float upperBodyLayerWeight = (isWeaponDrawn || !isUnarmed) ? 1f : 0f;
        animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), upperBodyLayerWeight, Time.deltaTime * 10f));

        if (isAiming && starterAssetsInputs.shoot)
        {
            Shoot();

            starterAssetsInputs.shoot = false;
        }

        wasMoving = isMoving;
        wasAiming = isAiming;

        aimRig.weight = Mathf.Lerp(aimRig.weight, aimRigWeight, Time.deltaTime * 20f);
    }

    #region 射击控制
    /// <summary>
    /// 上半身层是否正在播放（或正过渡到）拔枪/收枪动画；正在从这两个状态过渡出去时视为已结束
    /// </summary>
    private bool IsPlayingWeaponSwitchAnimation()
    {
        AnimatorStateInfo state = animator.IsInTransition(1)
            ? animator.GetNextAnimatorStateInfo(1)
            : animator.GetCurrentAnimatorStateInfo(1);

        return state.IsName("Player_Gun_Draw") || state.IsName("Player_Gun_Holster");
    }
    /// <summary>
    /// 在所有 AimAssistTarget 中，找出屏幕上离准星最近且在吸附半径内的目标
    /// </summary>
    /// <param name="screenCenter"></param>
    /// <param name="bestTarget"></param>
    /// <param name="targetScreenPos"></param>
    /// <returns></returns>
    private bool TryGetAimAssistTarget(Vector2 screenCenter, out AimAssistTarget bestTarget, out Vector3 targetScreenPos)
    {
        bestTarget = null;
        targetScreenPos = Vector3.zero;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        float bestDistance = aimAssistScreenRadius;
        foreach (AimAssistTarget target in AimAssistTarget.Targets)
        {
            if (target == null)
            {
                continue;
            }

            Vector3 screenPos = cam.WorldToScreenPoint(target.transform.position);
            if (screenPos.z <= 0f)
            {
                continue; // 目标在相机背后
            }

            float distance = Vector2.Distance(screenCenter, screenPos);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestTarget = target;
                targetScreenPos = screenPos;
            }
        }

        return bestTarget != null;
    }

    /// <summary>
    /// 控制玩家射击
    /// </summary>
    private void Shoot()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + Mathf.Max(fireRate, 0.01f);  // 射速控制
            animator.SetTrigger("Fire");    // 动画控制
            muzzleSmokeParticle.Play();     // VFX

            // TODO：实现这个射击结果判定
            if (hitTransform != null && hitTransform.GetComponentInParent<BulletTarget>() != null)
            {
                Instantiate(vfxHitRed, hitTransform.position, Quaternion.identity);
                Debug.Log("Hit a target!");
            }
        }
    }
    #endregion

    #region 动画事件
    /// <summary>
    /// 当动画播放到收枪时，切换枪的GameObject到腰部
    /// </summary>
    public void OnAnimHolsterWeapon()
    {
        weaponObjectHand.SetActive(false);
        weaponObjectHolster.SetActive(true);
    }

    /// <summary>
    /// 当动画播放到拔枪时，切换枪的GameObject到手部
    /// </summary>
    public void OnAnimDrawWeapon()
    {
        weaponObjectHand.SetActive(true);
        weaponObjectHolster.SetActive(false);
    }
    #endregion
}