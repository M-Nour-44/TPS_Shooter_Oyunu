using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour
{
    [Header("Camera to Assign")]
    public GameObject AimCam;
    public GameObject AimCanvas;
    public GameObject ThirdPersonCam;
    public GameObject ThirdPersonCanvas;

    [Header("Camera Animator")]
    public Animator animator;

    void Update()
    {
        bool isAiming = Input.GetButton("Fire2");

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        if (isAiming && isMoving)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("IdleAim", true);
            animator.SetBool("AimWalk", true);
            animator.SetBool("Walk", true);

            ThirdPersonCam.SetActive(false);
            ThirdPersonCanvas.SetActive(false);

            AimCam.SetActive(true);
            AimCanvas.SetActive(true);
        }
        else if (isAiming)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("IdleAim", true);
            animator.SetBool("AimWalk", false);
            animator.SetBool("Walk", false);

            ThirdPersonCam.SetActive(false);
            ThirdPersonCanvas.SetActive(false);

            AimCam.SetActive(true);
            AimCanvas.SetActive(true);
        }
        else
        {
            animator.SetBool("IdleAim", false);
            animator.SetBool("AimWalk", false);

            ThirdPersonCam.SetActive(true);
            ThirdPersonCanvas.SetActive(true);

            AimCam.SetActive(false);
            AimCanvas.SetActive(false);
        }
    }
}