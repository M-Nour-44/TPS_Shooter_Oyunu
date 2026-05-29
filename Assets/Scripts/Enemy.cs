using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Health and Damage")]
    [SerializeField] private float enemyHealth = 120f;
    private float presentHealth;
    public float giveDamage = 5f;
    public HealthBar healthBar;

    [Header("Enemy Things")]
    public NavMeshAgent enemyAgent;
    public Transform LookPoint;
    public Camera ShootingRaycastArea;
    public Transform playerBody;
    public LayerMask PlayerLayer;

    [Header("Enemy Guarding Var")]
    public GameObject[] walkPoints;
    int currentEnemyPosition = 0;

    public float walkingPointRadius = 2f;
    public float enemySpeed;

    [Header("Enemy Shooting Var")]
    public float timebtwShoot = 0.7f;
    bool previouslyShoot;

    [Header("Enemy Animation and Spark effect")]
    public Animator anim;
    public ParticleSystem muzzleSpark;
    public EnemyWeapon enemyWeapon;

    [Header("Enemy Mood/Situation")]
    public float visionRadius = 20f;
    public float shootingRadius = 10f;

    public bool playerInVisionRadius;
    public bool playerInShootingRadius;

    [Header("Stationary Guard")]
    public bool stationaryGuard = false;
    public bool stationaryCanShoot = true;

    private bool isDead = false;
    private bool isAlerted = false;

    private void Awake()
    {
        if (enemyAgent == null)
        {
            enemyAgent = GetComponent<NavMeshAgent>();
        }

        if (enemyAgent != null)
        {
            enemyAgent.speed = enemySpeed;
        }

        presentHealth = enemyHealth;

        if (healthBar != null)
        {
            healthBar.GiveFullHealth(enemyHealth);
        }

        if (playerBody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerBody = player.transform;
            }
        }

        if (stationaryGuard)
        {
            StopStationaryAnimation();
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (stationaryGuard)
        {
            StationaryGuardUpdate();
            return;
        }

        if (!AgentReady())
        {
            return;
        }

        bool playerInsideVision =
            Physics.CheckSphere(transform.position, visionRadius, PlayerLayer);

        bool playerInsideShooting =
            Physics.CheckSphere(transform.position, shootingRadius, PlayerLayer);

        playerInVisionRadius = playerInsideVision || isAlerted;
        playerInShootingRadius = playerInsideShooting;

        if (!playerInVisionRadius && !playerInShootingRadius)
        {
            Guard();
        }
        else if (playerInVisionRadius && !playerInShootingRadius)
        {
            PursuePlayer();
        }
        else if (playerInVisionRadius && playerInShootingRadius)
        {
            ShootPlayer();
        }
    }

    private bool AgentReady()
    {
        return enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh;
    }

    private void StationaryGuardUpdate()
    {
        if (enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh)
        {
            enemyAgent.isStopped = true;
            enemyAgent.ResetPath();
        }

        bool playerInsideShooting =
            Physics.CheckSphere(transform.position, shootingRadius, PlayerLayer);

        playerInVisionRadius = false;
        playerInShootingRadius = playerInsideShooting;

        if (playerInShootingRadius && stationaryCanShoot)
        {
            StationaryShootPlayer();
        }
        else
        {
            StopStationaryAnimation();
        }
    }

    private void StationaryShootPlayer()
    {
        LookAtPlayerYOnly();

        if (!previouslyShoot)
        {
            if (muzzleSpark != null)
            {
                muzzleSpark.Play();
            }

            if (enemyWeapon != null)
            {
                enemyWeapon.PlayShootingSound();
            }

            if (ShootingRaycastArea != null)
            {
                RaycastHit hit;

                if (Physics.Raycast(ShootingRaycastArea.transform.position, ShootingRaycastArea.transform.forward, out hit, shootingRadius))
                {
                    PlayerScript player = hit.transform.GetComponentInParent<PlayerScript>();

                    if (player != null)
                    {
                        player.playerHitDamage(giveDamage);
                    }
                }
            }

            previouslyShoot = true;
            Invoke(nameof(ActiveShooting), timebtwShoot);
        }

        if (anim != null)
        {
            SetAnimatorBoolIfExists("Shoot", true);
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("AimRun", false);
            SetAnimatorBoolIfExists("Die", false);
            SetAnimatorBoolIfExists("Running", false);
            SetAnimatorBoolIfExists("IsAiming", true);

            SetAnimatorFloatIfExists("Speed", 0f);
            SetAnimatorFloatIfExists("AimPitch", 0f);
        }
    }

    private void StopStationaryAnimation()
    {
        if (anim == null)
        {
            return;
        }

        SetAnimatorBoolIfExists("Shoot", false);
        SetAnimatorBoolIfExists("Walk", false);
        SetAnimatorBoolIfExists("AimRun", false);
        SetAnimatorBoolIfExists("Die", false);
        SetAnimatorBoolIfExists("Running", false);
        SetAnimatorBoolIfExists("IsAiming", false);

        SetAnimatorFloatIfExists("Speed", 0f);
        SetAnimatorFloatIfExists("AimPitch", 0f);
    }

    private void Guard()
    {
        if (!AgentReady())
        {
            return;
        }

        if (walkPoints == null || walkPoints.Length == 0)
        {
            return;
        }

        if (walkPoints[currentEnemyPosition] == null)
        {
            currentEnemyPosition = Random.Range(0, walkPoints.Length);
            return;
        }

        if (Vector3.Distance(transform.position, walkPoints[currentEnemyPosition].transform.position) <= walkingPointRadius)
        {
            currentEnemyPosition = Random.Range(0, walkPoints.Length);
        }

        if (walkPoints[currentEnemyPosition] != null)
        {
            enemyAgent.isStopped = false;
            enemyAgent.SetDestination(walkPoints[currentEnemyPosition].transform.position);
        }

        if (anim != null)
        {
            SetAnimatorBoolIfExists("Walk", true);
            SetAnimatorBoolIfExists("AimRun", false);
            SetAnimatorBoolIfExists("Shoot", false);
            SetAnimatorBoolIfExists("Die", false);
            SetAnimatorFloatIfExists("Speed", 0.5f);
        }
    }

    private void PursuePlayer()
    {
        if (!AgentReady())
        {
            return;
        }

        if (playerBody == null)
        {
            FindPlayer();

            if (playerBody == null)
            {
                return;
            }
        }

        enemyAgent.isStopped = false;
        enemyAgent.SetDestination(playerBody.position);

        if (anim != null)
        {
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("AimRun", true);
            SetAnimatorBoolIfExists("Shoot", false);
            SetAnimatorBoolIfExists("Die", false);
            SetAnimatorFloatIfExists("Speed", 1f);
        }
    }

    private void ShootPlayer()
    {
        if (!AgentReady())
        {
            return;
        }

        enemyAgent.isStopped = true;

        LookAtPlayerYOnly();

        if (!previouslyShoot)
        {
            if (muzzleSpark != null)
            {
                muzzleSpark.Play();
            }

            if (enemyWeapon != null)
            {
                enemyWeapon.PlayShootingSound();
            }

            if (ShootingRaycastArea != null)
            {
                RaycastHit hit;

                if (Physics.Raycast(ShootingRaycastArea.transform.position, ShootingRaycastArea.transform.forward, out hit, shootingRadius))
                {
                    PlayerScript player = hit.transform.GetComponentInParent<PlayerScript>();

                    if (player != null)
                    {
                        player.playerHitDamage(giveDamage);
                    }
                }
            }

            previouslyShoot = true;
            Invoke(nameof(ActiveShooting), timebtwShoot);
        }

        if (anim != null)
        {
            SetAnimatorBoolIfExists("Shoot", true);
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("AimRun", false);
            SetAnimatorBoolIfExists("Die", false);
            SetAnimatorFloatIfExists("Speed", 0f);
        }
    }

    private void LookAtPlayerYOnly()
    {
        Transform target = null;

        if (LookPoint != null)
        {
            target = LookPoint;
        }
        else if (playerBody != null)
        {
            target = playerBody;
        }

        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    private void ActiveShooting()
    {
        previouslyShoot = false;

        if (anim != null && !isDead)
        {
            SetAnimatorBoolIfExists("Shoot", false);
        }
    }

    public void enemyHitDamage(float takeDamage)
    {
        if (isDead)
        {
            return;
        }

        presentHealth -= takeDamage;

        AlertEnemy();

        if (healthBar != null)
        {
            healthBar.SetHealth(presentHealth);
        }

        if (presentHealth <= 0)
        {
            EnemyDie();
        }
    }

    private void AlertEnemy()
    {
        if (isDead)
        {
            return;
        }

        isAlerted = true;

        if (playerBody == null)
        {
            FindPlayer();
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerBody = player.transform;
        }
    }

    private void EnemyDie()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        CancelInvoke(nameof(ActiveShooting));

        playerInVisionRadius = false;
        playerInShootingRadius = false;
        visionRadius = 0f;
        shootingRadius = 0f;

        if (enemyAgent != null && enemyAgent.enabled)
        {
            if (enemyAgent.isOnNavMesh)
            {
                enemyAgent.isStopped = true;
                enemyAgent.ResetPath();
            }

            enemyAgent.enabled = false;
        }

        if (anim != null)
        {
            SetAnimatorBoolIfExists("Shoot", false);
            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("AimRun", false);
            SetAnimatorBoolIfExists("Die", true);
            SetAnimatorBoolIfExists("Running", false);
            SetAnimatorBoolIfExists("IsAiming", false);

            SetAnimatorFloatIfExists("Speed", 0f);
            SetAnimatorFloatIfExists("AimPitch", 0f);
        }

        BossMissionTarget bossMissionTarget = GetComponent<BossMissionTarget>();

        if (bossMissionTarget != null)
        {
            bossMissionTarget.CompleteBossMission();
        }

        Destroy(gameObject, 5f);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (anim == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in anim.parameters)
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
            anim.SetBool(parameterName, value);
        }
    }

    private void SetAnimatorFloatIfExists(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
        {
            anim.SetFloat(parameterName, value);
        }
    }
}