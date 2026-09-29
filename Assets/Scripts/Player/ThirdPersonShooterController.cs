using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.Animations.Rigging;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private Rig aimRig;
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity = 1f;
    [SerializeField] private float aimSensitivity = 1f;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawBulletPosition;
    [SerializeField] private Transform vfxHitRed;

    [SerializeField] private GameObject crossHairUI;                // 十字准心UI

    [Header("Weapon Control")]
    [SerializeField] private float fireRate = 5f;                   // 武器射速
    [SerializeField] private GameObject weaponObjectHand;           // 武器对象
    [SerializeField] private GameObject weaponObjectHolster;        // 收枪位置的武器对象
    [SerializeField] private ParticleSystem muzzleSmokeParticle;    // 枪口烟雾特效

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private Animator animator;
    private float aimRigWeight;
    private float nextFireTime;
    private bool wasMoving;
    private bool wasAiming;
    private bool isWeaponDrawn;

    private Transform hitTransform = null;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
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
        }

        // 只有拔枪后，按住鼠标右键才会进入瞄准
        bool isAiming = isWeaponDrawn && starterAssetsInputs.aim;

        if (isAiming)
        {
            crossHairUI.SetActive(true);

            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetRotateOnMove(false);

            aimRigWeight = 1f;

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

            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
            thirdPersonController.SetRotateOnMove(true);

            aimRigWeight = 0f;

            if (!isMoving && (wasMoving || wasAiming))
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

    public void OnAnimHolsterWeapon()
    {
        weaponObjectHand.SetActive(false);
        weaponObjectHolster.SetActive(true);
    }

    public void OnAnimDrawWeapon()
    {
        weaponObjectHand.SetActive(true);
        weaponObjectHolster.SetActive(false);
    }

    private void Shoot()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + Mathf.Max(fireRate, 0.01f);
            animator.SetTrigger("Fire");
            muzzleSmokeParticle.Play();

            // TODO：实现这个射击结果判定
            if (hitTransform != null && hitTransform.GetComponent<BulletTarget>() != null)
            {
                Instantiate(vfxHitRed, hitTransform.position, Quaternion.identity);
                Debug.Log("Hit a target!");
            }
        }
    }
}