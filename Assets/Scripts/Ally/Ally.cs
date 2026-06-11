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
    
    // Geri çağırma kilidi (G tuşu için)
    private bool isForcedRegrouping = false;

    [HideInInspector] public Vector3 commandTargetPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (allyWeaponSound == null)
            allyWeaponSound = GetComponentInChildren<AllyWeaponSound>();

        if (agent != null)
            agent.stoppingDistance = followDistance;

        presentHealth = allyHealth;

        if (healthBar != null)
            healthBar.GiveFullHealth(allyHealth);

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
        SetAnimatorBoolIfExists("Die", false);
    }

    void Update()
    {
        if (isDead || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!isCommandActive)
            currentState = AllyState.FollowPlayer;

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
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        else
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    public void CommandRegroup()
    {
        if (isDead) return;

        PlayVoiceLine(acknowledgeRegroupSounds);

        isCommandActive = false;
        currentTarget = null;
        currentState = AllyState.FollowPlayer;
        
        // G tuşuna basıldığında savaşı bırakıp sana koşmasını sağlayan kilit
        isForcedRegrouping = true;

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
        if (isDead) return;

        PlayVoiceLine(acknowledgeAttackSounds);

        isCommandActive = true;
        currentState = AllyState.CommandFocusFire;
        currentTarget = targetEnemy;
    }

    public void CommandMoveToLocation(Vector3 targetPos)
    {
        if (isDead) return;

        PlayVoiceLine(acknowledgeMoveSounds);

        isCommandActive = true;
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
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // G ile çağrıldıysa ve oyuncunun menziline girdiyse kilidi aç (Tekrar savaşabilir)
        if (isForcedRegrouping && distanceToPlayer <= followDistance + 1f)
        {
            isForcedRegrouping = false;
        }

        // 1. ÖNCELİK: SAVAŞ (Eğer G tuşu zorlaması yoksa)
        if (!isForcedRegrouping)
        {
            // DÜZELTME: Eğer vurulduğu için zaten bir hedef atanmışsa, onu unutma!
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                currentTarget = FindNearestVisibleEnemy(enemyDetectionRadius);
            }

            if (currentTarget != null)
            {
                ExecuteCombatLogic(false);
                return;
            }
        }

        // 2. ÖNCELİK: OYUNCUYU TAKİP ET (Savaş yoksa veya G tuşuna basıldıysa)
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
        currentTarget = null;

        if (distanceToPlayer > followDistance)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = (distanceToPlayer > runDistanceThreshold) ? runSpeed : walkSpeed;
            agent.stoppingDistance = followDistance;

            if (!agent.hasPath || Vector3.Distance(agent.destination, player.position) > 1f)
            {
                agent.SetDestination(player.position);
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void UpdateCommandMoveToState()
    {
        // F ile bir yere giderken yolda düşman görürse
        Transform enemyOnPath = FindNearestVisibleEnemy(enemyDetectionRadius);

        if (enemyOnPath != null)
        {
            // Gitmeyi bırak ve adama dal
            currentTarget = enemyOnPath;
            ExecuteCombatLogic(true);
            return;
        }

        // Etrafta düşman yoksa seçilen yere gitmeye devam et
        currentTarget = null;
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.stoppingDistance = 0.2f;

            if (!agent.hasPath || Vector3.Distance(agent.destination, commandTargetPosition) > 1f)
            {
                agent.SetDestination(commandTargetPosition);
            }
        }
    }

    void UpdateFocusFireState()
    {
        // Hedef geçerli değilse (öldüyse veya silindiyse) görev biter
        if (!IsTargetValid(currentTarget))
        {
            CommandRegroup(); // Otomatik olarak oyuncuya dön
            return;
        }

        ExecuteCombatLogic(true); // Yaşıyorsa peşini bırakma
    }

    void ExecuteCombatLogic(bool allowChasing)
    {
        if (currentTarget == null) return;

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
                
                // Siper/Duvar arkası mantığı
                agent.stoppingDistance = (!canSeeEnemy) ? 1.5f : Mathf.Max(1.5f, shootingRange * 0.75f);

                if (!agent.hasPath || Vector3.Distance(agent.destination, currentTarget.position) > 1.5f)
                {
                    agent.SetDestination(currentTarget.position);
                }
            }
            else
            {
                // DÜZELTME: Eğer peşinden koşamıyorsa ama adam menzildeyse, hedefi silmek yerine hızla ona dön!
                if (!canSeeEnemy && distanceToEnemy <= shootingRange)
                {
                    Vector3 turnDir = (currentTarget.position - transform.position).normalized;
                    turnDir.y = 0;
                    if (turnDir != Vector3.zero)
                    {
                        // 10f dönüş hızıdır, düşmana doğru döner
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(turnDir), Time.deltaTime * 10f);
                    }
                }
                else
                {
                    currentTarget = null;
                }
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
        if (target == null) return false;

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = target.position + Vector3.up * 1.5f;
        Vector3 directionToEnemy = (targetPos - rayOrigin).normalized;
        float distanceToEnemy = Vector3.Distance(rayOrigin, targetPos);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, directionToEnemy, out hit, distanceToEnemy + 2f))
        {
            if (hit.transform.root == target.root) return true;

            Enemy hitEnemy = hit.transform.GetComponentInParent<Enemy>();
            Enemy targetEnemy = target.GetComponentInParent<Enemy>();

            if (hitEnemy != null && targetEnemy != null && hitEnemy == targetEnemy)
                return true;
        }
        return false;
    }

    private Transform FindNearestVisibleEnemy(float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        Transform nearestEnemy = null;
        float minDistance = radius;

        foreach (Collider col in hitColliders)
        {
            Enemy e = col.GetComponentInParent<Enemy>();
            if (e != null && col.enabled)
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDistance && HasLineOfSightToTarget(e.transform))
                {
                    minDistance = dist;
                    nearestEnemy = e.transform;
                }
            }
        }
        return nearestEnemy;
    }

    private Transform FindNearestActiveEnemy(float radius)
{
    // Görüş açısından bağımsız, 360 derece bir küre ile etrafı tarar
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
    Transform nearestEnemy = null;
    float minDistance = radius;

    foreach (Collider col in hitColliders)
    {
        Enemy e = col.GetComponentInParent<Enemy>();
        if (e != null && col.enabled)
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

    private bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        Collider enemyCol = target.GetComponentInChildren<Collider>();
        if (enemyCol == null || !enemyCol.enabled) return false;
        return target.GetComponentInParent<Enemy>() != null;
    }

    private bool ShouldReturnToAimAfterHit()
    {
        if (!IsTargetValid(currentTarget)) return false;
        if (Vector3.Distance(transform.position, currentTarget.position) > shootingRange) return false;
        if (!HasLineOfSightToTarget(currentTarget)) return false;
        return true;
    }

    public void allyHitDamage(float takeDamage)
{
    if (isDead) return;

    presentHealth -= takeDamage;

    if (healthBar != null)
        healthBar.SetHealth(presentHealth);

    if (presentHealth <= 0)
    {
        AllyDie();
        return;
    }

    // --- YENİ: REFLEKS VE İNTİKAM MANTIĞI ---
    // Eğer G tuşuyla sana dönmeye zorlanmıyorsa ve F ile özel bir hedefe kilitli değilse:
    if (!isForcedRegrouping && currentState != AllyState.CommandFocusFire)
    {
        // Görüş açısında olmasa bile (arkasında olsa bile) en yakın düşmanı bul
        Transform attackerGuess = FindNearestActiveEnemy(enemyDetectionRadius);

        if (attackerGuess != null)
        {
            currentTarget = attackerGuess;
            ExecuteCombatLogic(true); // Anında o düşmana dal!
        }
    }
    // ----------------------------------------

    bool returnToAim = ShouldReturnToAimAfterHit();
    SetAnimatorBoolIfExists("IsAiming", returnToAim);
    SetAnimatorTriggerIfExists("Hit");
}

    void AllyDie()
    {
        if (isDead) return;

        isDead = true;

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

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
    }

    void ShootAtEnemy()
    {
        if (currentTarget == null) return;

        Vector3 rayOrigin = shootingPoint != null ? shootingPoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetDirection = (currentTarget.position + Vector3.up * 1.5f) - rayOrigin;

        if (muzzleSpark != null)
            muzzleSpark.Play();

        if (allyWeaponSound != null)
            allyWeaponSound.PlayShootingSound();

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
        if (animator == null) return;
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
        if (animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    public void step() { }

    private void PlayVoiceLine(AudioClip[] voiceClips)
    {
        if (voiceClips == null || voiceClips.Length == 0 || isDead || audioSource == null) return;
        if (!audioSource.enabled || !audioSource.gameObject.activeInHierarchy) return;
        if (Time.time < nextVoiceTime || audioSource.isPlaying) return;

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