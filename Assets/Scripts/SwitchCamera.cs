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
        // قراءة زر التصويب فقط
        bool isAiming = Input.GetButton("Fire2");

        if (isAiming)
        {
            // تفعيل منظور التصويب
            ThirdPersonCam.SetActive(false);
            ThirdPersonCanvas.SetActive(false);

            AimCam.SetActive(true);
            AimCanvas.SetActive(true);
        }
        else
        {
            // العودة للمنظور العادي
            ThirdPersonCam.SetActive(true);
            ThirdPersonCanvas.SetActive(true);

            AimCam.SetActive(false);
            AimCanvas.SetActive(false);
        }
    }
}