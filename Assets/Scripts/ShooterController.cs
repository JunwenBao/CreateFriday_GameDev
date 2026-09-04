using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterController : MonoBehaviour
{
    //private MovementInput input;
    private Animator anim;

    //[Header("Cinemachine")]
    //public CinemachineFreeLook thirdPersonCam;

    private void Start()
    {
        //input = GetComponent<MovementInput>();
        anim = GetComponent<Animator>();
    }
}