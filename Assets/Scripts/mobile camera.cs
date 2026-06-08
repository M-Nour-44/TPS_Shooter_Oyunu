using UnityEngine;
using Cinemachine;
using UnityEngine.EventSystems;

public class CinemachineTouchInput : MonoBehaviour
{
    [Header("Touch Settings")]
    [Tooltip("ارفع هذه القيمة لتسريع حركة الكاميرا")]
    public float touchSensitivity = 0.5f;
    
    [Header("Troubleshooting")]
    [Tooltip("ضع علامة صح هنا للتجربة. إذا تحركت الكاميرا، فهذا يعني أن هناك واجهة شفافة تغطي الشاشة!")]
    public bool ignoreUIBlock = false;

    private int rightFingerId = -1;
    private float lookX = 0f;
    private float lookY = 0f;

    void Start()
    {
        CinemachineCore.GetInputAxis = GetCustomInputAxis;
    }

    void Update()
    {
        lookX = 0f;
        lookY = 0f;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (rightFingerId == -1)
                        {
                            if (t.position.x > Screen.width / 2f)
                            {
                                // فحص ما إذا كان الإصبع فوق زر واجهة المستخدم
                                bool isOverUI = EventSystem.current.IsPointerOverGameObject(t.fingerId);
                                
                                // تصحيح مشكلة معروفة في Unity Remote لقراءة اللمس
                                #if UNITY_EDITOR
                                isOverUI = EventSystem.current.IsPointerOverGameObject();
                                #endif

                                // إذا فعلنا خيار التجاهل، أو لم يكن الإصبع فوق الواجهة
                                if (ignoreUIBlock || !isOverUI)
                                {
                                    rightFingerId = t.fingerId;
                                }
                            }
                        }
                        break;
                    case TouchPhase.Moved:
                        if (t.fingerId == rightFingerId)
                        {
                            lookX = t.deltaPosition.x * touchSensitivity;
                            lookY = t.deltaPosition.y * touchSensitivity;
                        }
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == rightFingerId)
                        {
                            rightFingerId = -1;
                        }
                        break;
                }
            }
        }
    }

    private float GetCustomInputAxis(string axisName)
    {
        if (Input.touchCount > 0)
        {
            if (axisName == "Mouse X") return lookX;
            if (axisName == "Mouse Y") return lookY;
        }
        else 
        {
            return Input.GetAxis(axisName);
        }
        
        return 0f;
    }
}