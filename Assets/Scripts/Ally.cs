using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Ally : MonoBehaviour
{
    public enum AllyState
    {
        FollowPlayer,
        Combat
    }
    
    [Header("Durum Makinesi (FSM)")]
    public AllyState currentState = AllyState.FollowPlayer;

    [Header("Hedef Ayarları")]
    public Transform player;
    public float followDistance = 3f;

    [Header("Hız Ayarları")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float runDistanceThreshold = 6f;

    [Header("Savaş Ayarları (Algılama)")]
    public float detectionRadius = 15f;
    public float combatStopDistance = 8f; 
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
    public float allyHealth = 10;
    private float presentHealth;
    [HideInInspector] public bool isDead = false; // Düşmanların bu değişkeni okuyabilmesi için public yaptık

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    private float nextTimeToShoot = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = followDistance;
        
        presentHealth = allyHealth; // Canı doldurarak başlıyoruz
    }

    void Update()
    {
        // Eğer Ally öldüyse hiçbir mantık arama, çalışmayı durdur
        if (isDead) return;

        // 1. Düşman Kontrolü (Sensör)
        FindNearestEnemy();

        // 2. Durum Geçişleri
        if (currentTarget != null)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
            if (distanceToEnemy <= detectionRadius)
            {
                currentState = AllyState.Combat;
            }
            else
            {
                currentState = AllyState.FollowPlayer;
                currentTarget = null;
            }
        }
        else
        {
            currentState = AllyState.FollowPlayer;
        }

        // 3. Mevcut Durumu Çalıştır
        switch (currentState)
        {
            case AllyState.FollowPlayer:
                UpdateFollowState();
                break;
            case AllyState.Combat:
                UpdateCombatState();
                break;
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    void UpdateFollowState()
    {
        agent.updateRotation = true; 
        agent.isStopped = false; 

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            agent.speed = (distanceToPlayer > runDistanceThreshold) ? runSpeed : walkSpeed;
            agent.stoppingDistance = followDistance;
            agent.SetDestination(player.position);
        }
    }

    void UpdateCombatState()
    {
        SetAnimatorBoolIfExists("IsAiming", true);

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToEnemy > combatStopDistance) 
        {
            agent.isStopped = false;
            agent.speed = walkSpeed; 
            agent.stoppingDistance = combatStopDistance; 
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
        }

        agent.updateRotation = false; 
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);

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

    // Düşmanların Ally'a hasar verebilmesi için çağıracağı fonksiyon
    public void allyHitDamage(float takeDamage)
    {
        if (isDead) return;

        presentHealth -= takeDamage;
        
        // Hasar alma animasyon tetikleyicisi
        SetAnimatorTriggerIfExists("Hit");

        if (presentHealth <= 0)
        {
            AllyDie();
        }
    }

    void AllyDie()
    {
        isDead = true;
        
        // Hareket sistemlerini kapat
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

        // Animasyonları sıfırla ve ölümü tetikle
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);
        SetAnimatorBoolIfExists("Die", true);

        // Çarpışma kutularını kapat ki ölü beden mermileri engellemesin
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 5 saniye sonra cesedi sahneden temizle (isteğe bağlı)
        Destroy(gameObject, 5f);
    }

    void FindNearestEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
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
        if (Physics.Raycast(rayOrigin, targetDirection.normalized, out hit, detectionRadius))
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}