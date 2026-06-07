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
    public float combatStopDistance = 24f; 

    [Header("Savaş Ayarları (Ateş Etme)")]
    public float giveDamage = 9f;
    public float timeBetweenShots = 0.5f;
    public Transform shootingPoint;
    
    [Header("Savaş Efektleri")]
    public ParticleSystem muzzleSpark;
    public AudioSource audioSource;
    public AudioClip shootingSound;

    [Header("Telsiz Sesleri (Voice Lines)")]
    public AudioClip[] acknowledgeMoveSounds;    
    public AudioClip[] acknowledgeAttackSounds;  
    public AudioClip[] acknowledgeRegroupSounds; 

    [Header("Ally Can Ayarları")]
    public float allyHealth = 120f;
    private float presentHealth;
    [HideInInspector] public bool isDead = false;

    public HealthBar healthBar; 

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    private float nextTimeToShoot = 0f;
    private float nextVoiceTime = 0f;

    [HideInInspector] public Vector3 commandTargetPosition; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = followDistance;
        presentHealth = allyHealth; 

        if (healthBar != null) healthBar.GiveFullHealth(allyHealth);
    }

    void Update()
    {
        if (isDead) return;

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

    // ================= TAKTİKSEL EMİRLER =================

    public void CommandRegroup()
    {
        if (isDead) return;

        PlayVoiceLine(acknowledgeRegroupSounds);

        isCommandActive = false;
        currentTarget = null;
        currentState = AllyState.FollowPlayer;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.stoppingDistance = followDistance;
        }
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

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true; 
            agent.speed = runSpeed;
            agent.stoppingDistance = 0.2f; 
            agent.SetDestination(commandTargetPosition);
            
            // Yola çıkarken savaşı hemen bırakmasını garantile
            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Shoot", false);
            currentTarget = null; 
        }
    }

    // ================= DİSİPLİNLİ DURUM GÜNCELLEMELERİ =================

    void UpdateFollowState()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. KESİN DÖNÜŞ (G Tuşu İtaati): Oyuncudan uzaksak her şeyi bırakıp koş!
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
            return; // Burası aşağıyı okumayı engeller, yani düşmanı görmezden gelir!
        }

        // 2. OTONOM KORUMA: Oyuncunun yanına geldiysek veya zaten yanındaysak etrafı tara
        Transform autoTarget = FindNearestActiveEnemy(combatStopDistance);

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
        bool isMovingToTarget = agent.pathPending || agent.remainingDistance > agent.stoppingDistance;

        // 1. KESİN İNTİKAL (F Tuşu İtaati): Hedefe varana kadar düşmanla İLGİLENME!
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
                
                if (agent.destination != commandTargetPosition)
                {
                    agent.SetDestination(commandTargetPosition);
                }
            }
        }
        else
        {
            // 2. BÖLGEYİ SAVUN: Hedefe vardık. Dur ve etrafı taramaya başla!
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Transform autoTarget = FindNearestActiveEnemy(combatStopDistance);
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

    void UpdateFocusFireState()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            CommandRegroup(); 
            return;
        }

        Collider enemyCol = currentTarget.GetComponentInChildren<Collider>();
        if (enemyCol != null && !enemyCol.enabled)
        {
            CommandRegroup(); 
            return;
        }

        ExecuteCombatLogic(true); 
    }

    // ================= GARANTİLİ SAVAŞ VE GÖRÜŞ MANTIĞI =================

    void ExecuteCombatLogic(bool allowChasing)
    {
        if (currentTarget == null) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f; 
        Vector3 targetPos = currentTarget.position + Vector3.up * 1.5f;
        Vector3 directionToEnemy = (targetPos - rayOrigin).normalized;
        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);

        bool canSeeEnemy = false;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, directionToEnemy, out hit, distanceToEnemy + 2f))
        {
            if (hit.transform.root == currentTarget.root || hit.transform.GetComponentInParent<Enemy>() != null)
            {
                canSeeEnemy = true;
            }
        }

        if (distanceToEnemy > combatStopDistance || !canSeeEnemy)
        {
            if (allowChasing)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.updateRotation = true; 
                
                if (!agent.hasPath || Vector3.Distance(agent.destination, currentTarget.position) > 1.5f)
                {
                    agent.stoppingDistance = 1.5f;
                    agent.SetDestination(currentTarget.position);
                }
                
                SetAnimatorBoolIfExists("IsAiming", false); 
                SetAnimatorBoolIfExists("Shoot", false); 
            }
            else
            {
                SetAnimatorBoolIfExists("IsAiming", false); 
                SetAnimatorBoolIfExists("Shoot", false);
                currentTarget = null; 
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
            agent.updateRotation = false; 
            
            SetAnimatorBoolIfExists("IsAiming", true); 

            Vector3 lookDirection = (currentTarget.position - transform.position).normalized;
            if (lookDirection != Vector3.zero) 
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f); 
            }

            if (Time.time >= nextTimeToShoot)
            {
                SetAnimatorBoolIfExists("Shoot", true); 
                ShootAtEnemy();
                nextTimeToShoot = Time.time + timeBetweenShots;
            }
            else
            {
                SetAnimatorBoolIfExists("Shoot", false); 
            }
        }
    }

    private Transform FindNearestActiveEnemy(float radius)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Transform nearestEnemy = null;
        float minDistance = radius;

        foreach (Enemy e in enemies)
        {
            if (e == null) continue;

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

    // ================= HASAR VE SES ALTYAPISI =================

    public void allyHitDamage(float takeDamage)
    {
        if (isDead) return;

        presentHealth -= takeDamage;
        if (healthBar != null) healthBar.SetHealth(presentHealth);
        SetAnimatorTriggerIfExists("Hit");
        if (presentHealth <= 0) AllyDie();
    }

    void AllyDie()
    {
        if (isDead) return;
        isDead = true;
        
        if (healthBar != null) healthBar.gameObject.SetActive(false); 

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
        foreach (Collider col in colliders) col.enabled = false;

        Destroy(gameObject, 5f);
    }

    void ShootAtEnemy()
    {
        Vector3 rayOrigin = shootingPoint != null ? shootingPoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetDirection = (currentTarget.position + Vector3.up * 1.5f) - rayOrigin;

        if (muzzleSpark != null) muzzleSpark.Play();
        if (audioSource != null && shootingSound != null) audioSource.PlayOneShot(shootingSound);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, targetDirection.normalized, out hit, 100f))
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
        if (audioSource.isPlaying && audioSource.clip != shootingSound) return; 

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