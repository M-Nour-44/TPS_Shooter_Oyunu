using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Player Movement")]
    public float playerSpeed = 3f;
    public float playerSprint = 6f;

    [Header("Player Health Things")]
    private float playerHealth = 120f;
    private float presentHealth;
    private bool isDead = false;
    public HealthBar healthBar;

    [Header("Player Script Cameras")]
    public Transform PlayerCamera;

    [Header("Player Animator and Gravity")]
    public CharacterController cC;
    public float gravity = -9.81f;
    public Animator animator;

    [Header("Death Animation")]
    public string deathBoolName = "Die";
    public string deathTriggerName = "";
    public bool destroyPlayerAfterDeath = false;
    public float destroyDelay = 6f;

    [Header("Player Sounds")]
    public AudioSource playerAudioSource;
    public AudioClip hitSound;
    public float hitSoundCooldown = 0.5f;
    private float nextHitSoundTime = 0f;

    [Header("Player Jumping and Velocity")]
    public float jumpRange = 1f;
    Vector3 velocity;
    public float turnCalmTime = 0.1f;
    float turnCalmVelocity;
    public Transform surfaceCheck;
    bool onSurface;
    public float surfaceDistance = 0.6f;
    public LayerMask surfaceMask;

    [Header("Aim / Shooting Rotation")]
    public float aimTurnSpeed = 15f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        presentHealth = playerHealth;

        if (healthBar != null)
        {
            healthBar.GiveFullHealth(playerHealth);
        }
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        onSurface = Physics.CheckSphere(surfaceCheck.position, surfaceDistance, surfaceMask);

        if (onSurface && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        if (cC != null)
        {
            cC.Move(velocity * Time.deltaTime);
        }

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
        bool shouldFaceCamera = isAiming || isShooting;

        bool isSprinting =
            Input.GetButton("Sprint") &&
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.DownArrow) ) &&
            onSurface &&
            !isAiming &&
            !isShooting;

        float currentSpeed = isSprinting ? playerSprint : playerSpeed;

        if (direction.magnitude >= 0.1f)
        {
            if (animator != null)
            {
                animator.SetBool("Idle", false);
                animator.SetBool("Walk", !isSprinting);
                animator.SetBool("Running", isSprinting);
                animator.SetBool("IdleAim", isAiming);
                animator.SetBool("AimWalk", isAiming);
            }

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
                float targetAngle =
                    Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg +
                    PlayerCamera.eulerAngles.y;

                float angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref turnCalmVelocity,
                    turnCalmTime
                );

                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }

            if (cC != null)
            {
                cC.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("Walk", false);
                animator.SetBool("Running", false);
                animator.SetBool("AimWalk", false);
            }

            if (shouldFaceCamera)
            {
                RotatePlayerWithCamera();

                if (animator != null)
                {
                    animator.SetBool("Idle", false);
                    animator.SetBool("IdleAim", isAiming);
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.SetBool("Idle", true);
                    animator.SetBool("IdleAim", false);
                }
            }
        }
    }

    void RotatePlayerWithCamera()
    {
        if (PlayerCamera == null)
        {
            return;
        }

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
            if (animator != null)
            {
                animator.SetBool("Walk", false);
                animator.SetBool("Running", false);
                animator.SetBool("AimWalk", false);
                animator.SetTrigger("Jump");
            }

            velocity.y = Mathf.Sqrt(jumpRange * -2f * gravity);
        }
        else
        {
            if (animator != null)
            {
                animator.ResetTrigger("Jump");
            }
        }
    }

    public void playerHitDamage(float takeDamage)
    {
        if (isDead)
        {
            return;
        }

        presentHealth -= takeDamage;

        if (presentHealth < 0)
        {
            presentHealth = 0;
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(presentHealth);
        }

        if (Time.time >= nextHitSoundTime)
        {
            if (playerAudioSource != null && hitSound != null)
            {
                playerAudioSource.PlayOneShot(hitSound);
                nextHitSoundTime = Time.time + hitSoundCooldown;
            }
        }

        if (presentHealth <= 0)
        {
            PlayerDie();
        }
    }

    private void PlayerDie()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (cC != null)
        {
            cC.enabled = false;
        }

        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            SetAnimatorBoolIfExists("Idle", false);
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("Running", false);
            SetAnimatorBoolIfExists("AimWalk", false);
            SetAnimatorBoolIfExists("IdleAim", false);
            ResetAnimatorTriggerIfExists("Jump");

            if (!string.IsNullOrEmpty(deathBoolName))
            {
                SetAnimatorBoolIfExists(deathBoolName, true);
            }

            if (!string.IsNullOrEmpty(deathTriggerName))
            {
                SetAnimatorTriggerIfExists(deathTriggerName);
            }
        }

        UIController uiController = FindObjectOfType<UIController>();

        if (uiController != null)
        {
            uiController.SendMessage("openDeathMenu", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0.0001f;
        }

        if (destroyPlayerAfterDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetAnimatorTriggerIfExists(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameterName);
        }
    }

    private void ResetAnimatorTriggerIfExists(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterName);
        }
    }
}