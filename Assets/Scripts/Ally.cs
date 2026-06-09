using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Ally : MonoBehaviour
{
    public enum AllyState
    {
        FollowPlayer,
        CommandMoveTo,
        CommandFocusFire
    }

    [Header("Durum Makinesi (FSM)")]
    public AllyState currentState = AllyState.FollowPlayer;
    public bool isCommandActive = false;

    [Header("Hedef Ayarları")]
    public Transform player;
    public float followDistance = 3f;

    [Header("Hız Ayarları")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float runDistanceThreshold = 6f;

    [Header("Savaş Ayarları (Menziller)")]
    public float enemyDetectionRadius = 30f;
    public float shootingRange = 18f;

    [Header("Savaş Ayarları (Ateş Etme)")]
    public float giveDamage = 9f;
    public float timeBetweenShots = 0.5f;
    public Transform shootingPoint;

    [Header("Savaş Efektleri")]
    public ParticleSystem muzzleSpark;
    public AllyWeaponSound allyWeaponSound;

    [Header("Telsiz Sesleri (Voice Lines)")]
    public AudioSource audioSource;
    public AudioClip[] acknowledgeMoveSounds;
    public AudioClip[] acknowledgeAttackSounds;
    public AudioClip[] acknowledgeRegroupSounds;

    [Header("Ally Can Ayarları")]
    public float allyHealth = 120f;
    private float presentHealth;
    [HideInInspector] public bool isDead = false;
    public AllyHealthBar healthBar;

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    private float nextTimeToShoot = 0f;
    private float nextVoiceTime = 0f;
    private bool isCommandMoveInterruptedByCombat = false;

    [HideInInspector] public Vector3 commandTargetPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (allyWeaponSound == null)
        {
            allyWeaponSound = GetComponentInChildren<AllyWeaponSound>();
        }

        if (agent != null)
        {
            agent.stoppingDistance = followDistance;
        }

        presentHealth = allyHealth;

        if (healthBar != null)
        {
            healthBar.GiveFullHealth(allyHealth);
        }

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
        SetAnimatorBoolIfExists("Die", false);
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        if (!isCommandActive)
        {
            currentState = AllyState.FollowPlayer;
        }

        switch (currentState)
        {
            case AllyState.FollowPlayer:
                UpdateFollowState();
                break;

            case AllyState.CommandMoveTo:
                UpdateCommandMoveToState();
                break;

            case AllyState.CommandFocusFire:
                UpdateFocusFireState();
                break;
        }

        if (agent.isStopped || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
    }

    public void CommandRegroup()
    {
        if (isDead)
        {
            return;
        }

        PlayVoiceLine(acknowledgeRegroupSounds);

        isCommandActive = false;
        isCommandMoveInterruptedByCombat = false;
        currentTarget = null;
        currentState = AllyState.FollowPlayer;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.stoppingDistance = followDistance;
        }

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
    }

    public void CommandAttackTarget(Transform targetEnemy)
    {
        if (isDead)
        {
            return;
        }

        PlayVoiceLine(acknowledgeAttackSounds);

        isCommandActive = true;
        isCommandMoveInterruptedByCombat = false;
        currentState = AllyState.CommandFocusFire;
        currentTarget = targetEnemy;
    }

    public void CommandMoveToLocation(Vector3 targetPos)
    {
        if (isDead)
        {
            return;
        }

        PlayVoiceLine(acknowledgeMoveSounds);

        isCommandActive = true;
        isCommandMoveInterruptedByCombat = false;
        currentState = AllyState.CommandMoveTo;
        commandTargetPosition = targetPos;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.stoppingDistance = 0.2f;
            agent.SetDestination(commandTargetPosition);

            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Shoot", false);

            currentTarget = null;
        }
    }

    void UpdateFollowState()
    {
        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > runDistanceThreshold)
        {
            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Shoot", false);

            currentTarget = null;

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.stoppingDistance = followDistance;
            agent.SetDestination(player.position);
            return;
        }

        Transform autoTarget = FindNearestVisibleEnemy(enemyDetectionRadius);

        if (autoTarget != null)
        {
            currentTarget = autoTarget;
            ExecuteCombatLogic(false);
        }
        else
        {
            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Shoot", false);

            if (distanceToPlayer > followDistance)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = walkSpeed;
                agent.stoppingDistance = followDistance;
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
    }

    void UpdateCommandMoveToState()
    {
        if (isCommandMoveInterruptedByCombat)
        {
            if (!IsTargetValid(currentTarget))
            {
                ResumeCommandMoveAfterCombat();
                return;
            }

            ExecuteCombatLogic(true);

            if (!IsTargetValid(currentTarget))
            {
                ResumeCommandMoveAfterCombat();
            }

            return;
        }

        Transform enemyOnPath = FindNearestVisibleEnemy(enemyDetectionRadius);

        if (enemyOnPath != null)
        {
            BeginCommandMoveCombatInterrupt(enemyOnPath);
            ExecuteCombatLogic(true);
            return;
        }

        ContinueCommandMoveToPosition();
    }

    private void ContinueCommandMoveToPosition()
    {
        bool isMovingToTarget = agent.pathPending || agent.remainingDistance > agent.stoppingDistance;

        if (isMovingToTarget)
        {
            agent.updateRotation = true;

            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Shoot", false);

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.stoppingDistance = 0.2f;

                if (!agent.hasPath || Vector3.Distance(agent.destination, commandTargetPosition) > 0.5f)
                {
                    agent.SetDestination(commandTargetPosition);
                }
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Transform autoTarget = FindNearestVisibleEnemy(enemyDetectionRadius);

            if (autoTarget != null)
            {
                currentTarget = autoTarget;
                ExecuteCombatLogic(false);
            }
            else
            {
                SetAnimatorBoolIfExists("IsAiming", false);
                SetAnimatorBoolIfExists("Shoot", false);
            }
        }
    }

    private void BeginCommandMoveCombatInterrupt(Transform targetEnemy)
    {
        if (targetEnemy == null)
        {
            return;
        }

        currentTarget = targetEnemy;
        isCommandMoveInterruptedByCombat = true;
        currentState = AllyState.CommandMoveTo;
    }

    private void ResumeCommandMoveAfterCombat()
    {
        isCommandMoveInterruptedByCombat = false;
        currentTarget = null;

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.stoppingDistance = 0.2f;
            agent.SetDestination(commandTargetPosition);
        }
    }

    void UpdateFocusFireState()
    {
        if (!IsTargetValid(currentTarget))
        {
            CommandRegroup();
            return;
        }

        ExecuteCombatLogic(true);
    }

    void ExecuteCombatLogic(bool allowChasing)
    {
        if (currentTarget == null)
        {
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        bool canSeeEnemy = HasLineOfSightToTarget(currentTarget);

        if (distanceToEnemy > shootingRange || !canSeeEnemy)
        {
            SetAnimatorBoolIfExists("Shoot", false);
            SetAnimatorBoolIfExists("IsAiming", false);

            if (allowChasing)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.updateRotation = true;
                agent.stoppingDistance = Mathf.Max(1.5f, shootingRange * 0.75f);

                if (!agent.hasPath || Vector3.Distance(agent.destination, currentTarget.position) > 1.5f)
                {
                    agent.SetDestination(currentTarget.position);
                }
            }
            else
            {
                currentTarget = null;
            }

            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.updateRotation = false;

        SetAnimatorBoolIfExists("IsAiming", true);
        SetAnimatorBoolIfExists("Shoot", true);

        Vector3 lookDirection = (currentTarget.position - transform.position).normalized;

        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0f, lookDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        if (Time.time >= nextTimeToShoot)
        {
            ShootAtEnemy();
            nextTimeToShoot = Time.time + timeBetweenShots;
        }
    }

    private bool HasLineOfSightToTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        Vector3 targetPos = target.position + Vector3.up * 1.5f;
        Vector3 directionToEnemy = (targetPos - rayOrigin).normalized;
        float distanceToEnemy = Vector3.Distance(rayOrigin, targetPos);

        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, directionToEnemy, out hit, distanceToEnemy + 2f))
        {
            Enemy hitEnemy = hit.transform.GetComponentInParent<Enemy>();
            Enemy targetEnemy = target.GetComponentInParent<Enemy>();

            if (hit.transform.root == target.root)
            {
                return true;
            }

            if (hitEnemy != null && targetEnemy != null && hitEnemy == targetEnemy)
            {
                return true;
            }
        }

        return false;
    }

    private Transform FindNearestActiveEnemy(float radius)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Transform nearestEnemy = null;
        float minDistance = radius;

        foreach (Enemy e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            Collider enemyCol = e.GetComponentInChildren<Collider>();

            if (enemyCol != null && enemyCol.enabled)
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestEnemy = e.transform;
                }
            }
        }

        return nearestEnemy;
    }

    private Transform FindNearestVisibleEnemy(float radius)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Transform nearestEnemy = null;
        float minDistance = radius;

        foreach (Enemy e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            Collider enemyCol = e.GetComponentInChildren<Collider>();

            if (enemyCol == null || !enemyCol.enabled)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, e.transform.position);

            if (dist < minDistance && HasLineOfSightToTarget(e.transform))
            {
                minDistance = dist;
                nearestEnemy = e.transform;
            }
        }

        return nearestEnemy;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (!target.gameObject.activeInHierarchy)
        {
            return false;
        }

        Collider enemyCol = target.GetComponentInChildren<Collider>();

        if (enemyCol == null || !enemyCol.enabled)
        {
            return false;
        }

        return target.GetComponentInParent<Enemy>() != null;
    }

    private bool ShouldReturnToAimAfterHit()
    {
        if (!IsTargetValid(currentTarget))
        {
            return false;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget > shootingRange)
        {
            return false;
        }

        if (!HasLineOfSightToTarget(currentTarget))
        {
            return false;
        }

        return true;
    }

    public void allyHitDamage(float takeDamage)
    {
        if (isDead)
        {
            return;
        }

        presentHealth -= takeDamage;

        if (healthBar != null)
        {
            healthBar.SetHealth(presentHealth);
        }

        if (presentHealth <= 0)
        {
            AllyDie();
            return;
        }

        if (currentState == AllyState.CommandMoveTo && isCommandActive)
        {
            Transform attackerGuess = FindNearestVisibleEnemy(enemyDetectionRadius);

            if (attackerGuess == null)
            {
                attackerGuess = FindNearestActiveEnemy(enemyDetectionRadius);
            }

            if (attackerGuess != null)
            {
                BeginCommandMoveCombatInterrupt(attackerGuess);
            }
        }

        bool returnToAim = ShouldReturnToAimAfterHit();

        SetAnimatorBoolIfExists("IsAiming", returnToAim);
        SetAnimatorTriggerIfExists("Hit");
    }

    void AllyDie()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        transform.position += Vector3.up * 0.15f;

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
        SetAnimatorBoolIfExists("Die", true);

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 5f);
    }

    void ShootAtEnemy()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 rayOrigin = shootingPoint != null ? shootingPoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetDirection = (currentTarget.position + Vector3.up * 1.5f) - rayOrigin;

        if (muzzleSpark != null)
        {
            muzzleSpark.Play();
        }

        if (allyWeaponSound != null)
        {
            allyWeaponSound.PlayShootingSound();
        }

        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, targetDirection.normalized, out hit, shootingRange))
        {
            Enemy hitEnemy = hit.transform.GetComponentInParent<Enemy>();

            if (hitEnemy != null)
            {
                hitEnemy.enemyHitDamage(giveDamage);
            }
        }
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void SetAnimatorTriggerIfExists(string parameterName)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    public void step()
    {
    }

    private void PlayVoiceLine(AudioClip[] voiceClips)
    {
        if (voiceClips == null || voiceClips.Length == 0 || isDead || audioSource == null)
        {
            return;
        }

        if (!audioSource.enabled || !audioSource.gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.time < nextVoiceTime)
        {
            return;
        }

        if (audioSource.isPlaying)
        {
            return;
        }

        int randomIndex = Random.Range(0, voiceClips.Length);
        AudioClip selectedClip = voiceClips[randomIndex];

        if (selectedClip != null)
        {
            audioSource.clip = selectedClip;
            audioSource.Play();
            nextVoiceTime = Time.time + selectedClip.length + 0.5f;
        }
    }
}