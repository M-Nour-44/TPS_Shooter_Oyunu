using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Ally : MonoBehaviour
{
    public enum AllyState
    {
        FollowPlayer,
        Combat,
        CommandRetreat, 
        CommandAttack,  
        CommandCover,
        CommandMoveTo   // --- YENİ EKLENDİ: İşaretlenen yere gitme durumu ---
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

    [Header("Savaş Ayarları (Algılama)")]
    public float detectionRadius = 15f;
    public float combatStopDistance = 15f; 
    public LayerMask enemyLayer;

    [Header("Savaş Ayarları (Ateş Etme)")]
    public float giveDamage = 10f;
    public float timeBetweenShots = 0.5f;
    public Transform shootingPoint;
    
    [Header("Savaş Efektleri")]
    public ParticleSystem muzzleSpark;
    public AudioSource audioSource;
    public AudioClip shootingSound;

    [Header("Ally Can Ayarları")]
    public float allyHealth = 120f;
    private float presentHealth;
    [HideInInspector] public bool isDead = false;

    public HealthBar healthBar; 

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    private float nextTimeToShoot = 0f;

    // --- YENİ EKLENDİ: Lazerin çarptığı hedef koordinatı aklında tutması için ---
    [HideInInspector] public Vector3 commandTargetPosition; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = followDistance;
        presentHealth = allyHealth; 

        if (healthBar != null)
        {
            healthBar.GiveFullHealth(allyHealth);
        }
    }

    void Update()
    {
        if (isDead) return;

        ListenForCommands();

        if (!isCommandActive)
        {
            FindNearestEnemy(detectionRadius); 

            if (currentTarget != null)
            {
                currentState = AllyState.Combat;
            }
            else
            {
                currentState = AllyState.FollowPlayer;
            }
        }

        switch (currentState)
        {
            case AllyState.FollowPlayer:
                UpdateFollowState();
                break;
            case AllyState.Combat:
                UpdateCombatState();
                break;
            case AllyState.CommandRetreat:
                UpdateRetreatState();
                break;
            case AllyState.CommandAttack:
                UpdateForceAttackState();
                break;
            case AllyState.CommandCover:
                UpdateTakeCoverState();
                break;
            case AllyState.CommandMoveTo: // --- YENİ EKLENDİ ---
                UpdateCommandMoveToState();
                break;
        }

        if (agent.isStopped)
        {
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void ListenForCommands()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            isCommandActive = false;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            isCommandActive = true;
            currentState = AllyState.CommandAttack;
            FindNearestEnemy(100f); 
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            isCommandActive = true;
            currentState = AllyState.CommandRetreat;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) 
        {
            isCommandActive = true;
            currentState = AllyState.CommandCover;
            FindCoverPosition();
        }
    }

    // ================= YENİ EKLENEN FONKSİYONLAR =================

    // TacticalCommander scripti tarafından çağrılacak olan emir alma fonksiyonu
    public void CommandMoveToLocation(Vector3 targetPos)
    {
        if (isDead) return;

        isCommandActive = true; 
        currentState = AllyState.CommandMoveTo; 
        commandTargetPosition = targetPos;      

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(commandTargetPosition);
        }
    }

    // Hedefe varana kadar çalışacak bekleme mantığı
    void UpdateCommandMoveToState()
    {
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        // Hedefe (1.5 metre kala) yaklaştıysa/vardıysa dur
        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }
    // ==============================================================

    void UpdateRetreatState()
    {
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = runSpeed;
        
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (player != null)
        {
            agent.SetDestination(player.position);
            
            if (Vector3.Distance(transform.position, player.position) <= followDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
    }

    void UpdateForceAttackState()
    {
        if (currentTarget == null)
        {
            FindNearestEnemy(100f);
        }

        if (currentTarget != null)
        {
            UpdateCombatState(); 
        }
        else
        {
            isCommandActive = false; 
        }
    }

    void FindCoverPosition()
    {
        NavMeshHit hit;
        if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void UpdateTakeCoverState()
    {
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = runSpeed;

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void UpdateFollowState()
    {
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > followDistance)
            {
                agent.isStopped = false;
                agent.updateRotation = true; 
                agent.speed = (distanceToPlayer > runDistanceThreshold) ? runSpeed : walkSpeed;
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

    void UpdateCombatState()
    {
        SetAnimatorBoolIfExists("IsAiming", true);

        if (currentTarget == null) return;

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        
        agent.updateRotation = false; 
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);

        if (distanceToEnemy > combatStopDistance) 
        {
            agent.isStopped = false;
            agent.speed = walkSpeed; 
            agent.stoppingDistance = combatStopDistance; 
            agent.SetDestination(currentTarget.position);

            SetAnimatorBoolIfExists("Shoot", false); 
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 

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

    public void allyHitDamage(float takeDamage)
    {
        if (isDead) return;

        presentHealth -= takeDamage;
        
        if (healthBar != null)
        {
            healthBar.SetHealth(presentHealth);
        }
        
        SetAnimatorTriggerIfExists("Hit");

        if (presentHealth <= 0)
        {
            AllyDie();
        }
    }

    void AllyDie()
    {
        if (isDead) return;
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

    void FindNearestEnemy(float radius)
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Collider enemyCollider in enemiesInRange)
        {
            Enemy enemyScript = enemyCollider.GetComponentInParent<Enemy>();
            
            if (enemyScript != null && enemyScript.gameObject.activeInHierarchy) 
            {
                 float distanceToEnemy = Vector3.Distance(transform.position, enemyCollider.transform.position);
                 if (distanceToEnemy < shortestDistance)
                 {
                     shortestDistance = distanceToEnemy;
                     nearestEnemy = enemyScript.transform; 
                 }
            }
        }
        currentTarget = nearestEnemy;
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

    public void step()
    {
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}