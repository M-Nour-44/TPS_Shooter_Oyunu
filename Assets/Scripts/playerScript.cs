using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Player Movement")]
    public float playerSpeed = 3f;
    public float playerSprint = 6f;

    [Header("Player Script Cameras")]
    public Transform PlayerCamera;

    [Header("Player Animator and Gravity")]
    public CharacterController cC;
    public float gravity = -9.81f;
    public Animator animator;

    [Header("Player Jumping and Velocity")]
    public float jumpRange = 1f;
    Vector3 velocity;

    public float turnCalmTime = 0.1f;
    float turnCalmVelocity;

    public Transform surfaceCheck;
    bool onSurface;
    public float surfaceDistance = 0.4f;
    public LayerMask surfaceMask;

    [Header("Aim / Shooting Rotation")]
    public float aimTurnSpeed = 15f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        onSurface = Physics.CheckSphere(surfaceCheck.position, surfaceDistance, surfaceMask);

        if (onSurface && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        cC.Move(velocity * Time.deltaTime);

        PlayerMove();

        Jump();
    }

    void PlayerMove()
    {
        float horizontal_axis = Input.GetAxisRaw("Horizontal");
        float vertical_axis = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal_axis, 0f, vertical_axis).normalized;

        bool isAiming = Input.GetButton("Fire2");
        bool isShooting = Input.GetButton("Fire1");

        // اللاعب يلف مع الكاميرا وقت Aim أو Shooting
        bool shouldFaceCamera = isAiming || isShooting;

        bool isSprinting =
            Input.GetButton("Sprint") &&
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) &&
            onSurface &&
            !isAiming &&
            !isShooting;

        float currentSpeed = isSprinting ? playerSprint : playerSpeed;

        if (direction.magnitude >= 0.1f)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", !isSprinting);
            animator.SetBool("Running", isSprinting);

            animator.SetBool("IdleAim", isAiming);
            animator.SetBool("AimWalk", isAiming);

            Vector3 moveDirection;

            if (shouldFaceCamera)
            {
                RotatePlayerWithCamera();

                Vector3 camForward = PlayerCamera.forward;
                Vector3 camRight = PlayerCamera.right;

                camForward.y = 0f;
                camRight.y = 0f;

                camForward.Normalize();
                camRight.Normalize();

                moveDirection = camForward * vertical_axis + camRight * horizontal_axis;
            }
            else
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + PlayerCamera.eulerAngles.y;

                float angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref turnCalmVelocity,
                    turnCalmTime
                );

                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }

            cC.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Running", false);
            animator.SetBool("AimWalk", false);

            if (shouldFaceCamera)
            {
                RotatePlayerWithCamera();

                animator.SetBool("Idle", false);
                animator.SetBool("IdleAim", isAiming);
            }
            else
            {
                animator.SetBool("Idle", true);
                animator.SetBool("IdleAim", false);
            }
        }
    }

    void RotatePlayerWithCamera()
    {
        float cameraYaw = PlayerCamera.eulerAngles.y;

        Quaternion targetRotation = Quaternion.Euler(0f, cameraYaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            aimTurnSpeed * Time.deltaTime
        );
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && onSurface)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Running", false);
            animator.SetBool("AimWalk", false);

            animator.SetTrigger("Jump");

            velocity.y = Mathf.Sqrt(jumpRange * -2f * gravity);
        }
        else
        {
            animator.ResetTrigger("Jump");
        }
    }
}