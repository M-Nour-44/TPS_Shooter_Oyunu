using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Player Movement")]
    public float playerSpeed = 3f;
    public float playerSprint = 6f;

    [Header("Sitting")]
    public KeyCode sitKey = KeyCode.C;
    public float sitSpeed = 1.5f;
    private bool isSitting = false;

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
    private Vector3 velocity;
    public float turnCalmTime = 0.1f;
    private float turnCalmVelocity;
    public Transform surfaceCheck;
    private bool onSurface;
    public float surfaceDistance = 0.6f;
    public LayerMask surfaceMask;

    [Header("Aim / Shooting Rotation")]
    public float aimTurnSpeed = 15f;

    [Header("Enemy Collision Fix")]
    public float enemyTopNormalLimit = 0.25f;
    public float enemyPushForce = 5f;
    public float enemyDownForce = -8f;

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

        HandleSitting();
        PlayerMove();
        Jump();
    }

    void HandleSitting()
    {
        bool isAiming = Input.GetButton("Fire2");
        bool isShooting = Input.GetButton("Fire1");

        float horizontal_axis = Input.GetAxisRaw("Horizontal");
        float vertical_axis = Input.GetAxisRaw("Vertical");

        bool hasMoveInput = Mathf.Abs(horizontal_axis) > 0.1f || Mathf.Abs(vertical_axis) > 0.1f;

        if (isSitting && Input.GetButton("Sprint") && hasMoveInput && !isAiming && !isShooting)
        {
            isSitting = false;
        }

        if (Input.GetKeyDown(sitKey) && onSurface)
        {
            isSitting = !isSitting;
        }

        if (animator != null)
        {
            animator.SetBool("IsSitting", isSitting);
        }
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
            (Input.GetKey(KeyCode.W) ||
             Input.GetKey(KeyCode.A) ||
             Input.GetKey(KeyCode.D) ||
             Input.GetKey(KeyCode.S) ||
             Input.GetKey(KeyCode.UpArrow) ||
             Input.GetKey(KeyCode.LeftArrow) ||
             Input.GetKey(KeyCode.RightArrow) ||
             Input.GetKey(KeyCode.DownArrow)) &&
            onSurface &&
            !isAiming &&
            !isShooting &&
            !isSitting;

        float currentSpeed = isSitting ? sitSpeed : (isSprinting ? playerSprint : playerSpeed);

        float animSpeed = 0f;

        if (direction.magnitude >= 0.1f)
        {
            animSpeed = isSprinting ? 1f : 0.5f;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("IsAiming", shouldFaceCamera);

            float aimPitch = 0f;

            if (PlayerCamera != null)
            {
                float cameraPitch = PlayerCamera.eulerAngles.x;

                if (cameraPitch > 180f)
                {
                    cameraPitch -= 360f;
                }

                aimPitch = Mathf.Clamp(-cameraPitch / 90f, -1f, 1f);
            }

            animator.SetFloat("AimPitch", aimPitch, 0.2f, Time.deltaTime);
        }

        if (direction.magnitude >= 0.1f)
        {
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
            if (shouldFaceCamera)
            {
                RotatePlayerWithCamera();
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
        if (Input.GetButtonDown("Jump") && onSurface && !isSitting)
        {
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }

            velocity.y = Mathf.Sqrt(jumpRange * -2f * gravity);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead)
        {
            return;
        }

        if (hit.collider == null)
        {
            return;
        }

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

        if (enemy == null && !hit.collider.CompareTag("Enemy"))
        {
            return;
        }

        if (hit.normal.y > enemyTopNormalLimit)
        {
            Vector3 pushDirection = transform.position - hit.collider.bounds.center;
            pushDirection.y = 0f;

            if (pushDirection.sqrMagnitude < 0.01f)
            {
                pushDirection = -transform.forward;
            }

            pushDirection.Normalize();

            if (cC != null && cC.enabled)
            {
                cC.Move(pushDirection * enemyPushForce * Time.deltaTime);
            }

            velocity.y = enemyDownForce;
        }
    }

    public bool IsOnGround()
    {
        return onSurface;
    }

    public bool IsSitting()
    {
        return isSitting;
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
        else
        {
            SetAnimatorTriggerIfExists("Hit");
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

            SetAnimatorFloatIfExists("Speed", 0f);
            SetAnimatorFloatIfExists("AimPitch", 0f);

            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("IsSitting", false);
            SetAnimatorBoolIfExists("Idle", false);
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("Running", false);
            SetAnimatorBoolIfExists("AimWalk", false);
            SetAnimatorBoolIfExists("IdleAim", false);
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", false);
            SetAnimatorBoolIfExists("Reloading", false);

            ResetAnimatorTriggerIfExists("Jump");
            ResetAnimatorTriggerIfExists("Hit");

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

    private void SetAnimatorFloatIfExists(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(parameterName, value);
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