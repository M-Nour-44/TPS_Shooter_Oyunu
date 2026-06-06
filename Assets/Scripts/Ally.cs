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
    public float combatStopDistance = 15f; 

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

        // ARTIK OTOMATİK DÜŞMAN ARAMA (FindNearestEnemy) BURADAN KALDIRILDI!
        // Ally sadece ve sadece emir aktif değilse oyuncuyu takip edecek.
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

        if (agent.isStopped) animator.SetFloat("Speed", 0f);
        else animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ================= SADELEŞTİRİLMİŞ TAKTİKSEL EMİRLER =================

    // E TUŞUNA BASILDIĞINDA ÇALIŞIR: Tüm emirleri iptal eder ve oyuncuya döner
    public void CommandRegroup()
    {
        if (isDead) return;

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

    // F TUŞU DÜŞMANA BASINCA: İşaretli düşmana kilitlenir
    public void CommandAttackTarget(Transform targetEnemy)
    {
        if (isDead) return;

        isCommandActive = true;
        currentState = AllyState.CommandFocusFire; 
        currentTarget = targetEnemy;               
    }

    // F TUŞU ZEMİNE BASINCA: Oraya gider ve bekler
    public void CommandMoveToLocation(Vector3 targetPos)
    {
        if (isDead) return;

        isCommandActive = true; 
        currentState = AllyState.CommandMoveTo; 
        commandTargetPosition = targetPos;      

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true; 
            agent.speed = runSpeed;
            agent.SetDestination(commandTargetPosition);
        }
    }

    // ================= DURUM GÜNCELLEMELERİ =================

    void UpdateFocusFireState()
    {
        // Hedef yok olduysa otomatik olarak takibe (oyuncunun yanına) geri dön
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            CommandRegroup();
            return;
        }

        // Düşmanın çarpışma kutusu kapandıysa (öldüyse) sıkmayı kes ve yanıma dön
        Collider enemyCol = currentTarget.GetComponentInChildren<Collider>();
        if (enemyCol != null && !enemyCol.enabled)
        {
            CommandRegroup();
            return;
        }

        UpdateCombatState(); 
    }

    void UpdateCommandMoveToState()
    {
        agent.updateRotation = true; 

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
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
        if (currentTarget == null) return;

        SetAnimatorBoolIfExists("IsAiming", true);

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        
        agent.updateRotation = false; 
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        
        if (direction != Vector3.zero) 
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);
        }

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

    // ================= SAVAŞ VE HASAR ALTYAPISI =================

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
}