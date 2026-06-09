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

    void Update()
    {
        bool isAiming = Input.GetButton("Fire2");

        if (isAiming)
        {
            if (ThirdPersonCam != null) ThirdPersonCam.SetActive(false);
            if (ThirdPersonCanvas != null) ThirdPersonCanvas.SetActive(false);

            if (AimCam != null) AimCam.SetActive(true);
            if (AimCanvas != null) AimCanvas.SetActive(true);
        }
        else
        {
            if (ThirdPersonCam != null) ThirdPersonCam.SetActive(true);
            if (ThirdPersonCanvas != null) ThirdPersonCanvas.SetActive(true);

            if (AimCam != null) AimCam.SetActive(false);
            if (AimCanvas != null) AimCanvas.SetActive(false);
        }
    }
}