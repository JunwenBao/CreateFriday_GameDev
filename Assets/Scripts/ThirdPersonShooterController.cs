using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
//using System.Numerics;

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

    [SerializeField] private GameObject crossHairUI;

    [Header("Gun")]
    [SerializeField] private ParticleSystem muzzleSmokeParticle;

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private Animator animator;
    private float aimRigWeight;

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
        Transform hitTransform = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask))
        {
            debugTransform.position = hit.point;
            mouseWorldPosition = hit.point;
            hitTransform = hit.transform;
        }

        if (starterAssetsInputs.aim)
        {
            crossHairUI.SetActive(true);

            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetRotateOnMove(false);
            aimRigWeight = 1f;
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
        }
        else
        {
            crossHairUI.SetActive(false);

            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
            thirdPersonController.SetRotateOnMove(true);
            aimRigWeight = 0f;
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

        }

        if (starterAssetsInputs.shoot)
        {
            animator.SetTrigger("Fire");
            muzzleSmokeParticle.Play();

            if (hitTransform != null)
            {
                if (hitTransform.GetComponent<BulletTarget>() != null)
                {
                    Instantiate(vfxHitRed, hitTransform.position, Quaternion.identity); // Instantiate green hit effect
                    Debug.Log("Hit a target!");
                }
                else
                {
                    //Instantiate(vfxHitRed, transform.position, Quaternion.identity); // Instantiate red hit effect
                }
            }

            //Vector3 aimDir = (mouseWorldPosition - spawBulletPosition.position).normalized;
            //Instantiate(pfBulletProjectile, spawBulletPosition.position, Quaternion.LookRotation(aimDir, Vector3.up));
            starterAssetsInputs.shoot = false; // Reset shoot input after shootings
        }

        aimRig.weight = Mathf.Lerp(aimRig.weight, aimRigWeight, Time.deltaTime * 20f);
    }
}