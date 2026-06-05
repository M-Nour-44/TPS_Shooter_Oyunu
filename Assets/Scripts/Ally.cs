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
    public float combatStopDistance = 8f; // Düşmana ateş etmek için ne kadar yaklaşacağı
    public LayerMask enemyLayer;

    [Header("Savaş Ayarları (Ateş Etme)")]
    public float giveDamage = 10f;
    public float timeBetweenShots = 0.5f;
    public Transform shootingPoint;
    
    [Header("Savaş Efektleri")]
    public ParticleSystem muzzleSpark;
    public AudioSource audioSource;
    public AudioClip shootingSound;

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    private float nextTimeToShoot = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = followDistance;
    }

    void Update()
    {
        // 1. Düşman Kontrolü (Sensör)
        FindNearestEnemy();

        // 2. Durum Geçişleri (State Transitions)
        if (currentTarget != null)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToEnemy <= detectionRadius)
            {
                currentState = AllyState.Combat; // Menzilde düşman var, SAVAŞA GEÇ
            }
            else
            {
                currentState = AllyState.FollowPlayer; // Düşman uzaklaştı, TAKİBE DÖN
                currentTarget = null;
            }
        }
        else
        {
            currentState = AllyState.FollowPlayer; // Düşman yok, TAKİPTE KAL
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

        // NavMeshAgent'ın anlık hızını Animator'a aktar
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ================= FSM DURUM FONKSİYONLARI =================

    void UpdateFollowState()
    {
        // Takip durumunda normal hareket etsin, yönünü kendi bulsun
        agent.updateRotation = true; 
        agent.isStopped = false; 

        // Savaş animasyonlarını kapat
        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (player != null)
        {
            // Oyuncuya olan mesafeye göre hızını ayarla (Yürü veya Koş)
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            agent.speed = (distanceToPlayer > runDistanceThreshold) ? runSpeed : walkSpeed;
            
            agent.stoppingDistance = followDistance;
            agent.SetDestination(player.position);
        }
    }

    void UpdateCombatState()
    {
        // Savaştayız, nişan alma animasyonunu başlat
        SetAnimatorBoolIfExists("IsAiming", true);

        // --- 1. MESAFE KONTROLÜ (Aiming Run mu, Sabit Shoot mu?) ---
        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToEnemy > combatStopDistance) 
        {
            // Düşman uzaktaysa, nişan alarak üstüne yürü (Aiming Run)
            agent.isStopped = false;
            agent.speed = walkSpeed; 
            agent.stoppingDistance = combatStopDistance; // Düşmanın dibine girmesin
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            // Düşman atış menzilindeyse dur ve pozisyonunu koru (Aiming Shoot)
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
        }

        // --- 2. YÖN KONTROLÜ (Gövdeyi her zaman düşmana dön) ---
        agent.updateRotation = false; // NavMesh'in otomatik dönüşünü yasakla
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        
        // Karakterin yukarı/aşağı eğilmemesi için Y eksenini sıfırla
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);

        // --- 3. ATEŞ ETME ---
        if (Time.time >= nextTimeToShoot)
        {
            SetAnimatorBoolIfExists("Shoot", true); // Kısa ateş animasyonunu tetikle
            ShootAtEnemy();
            nextTimeToShoot = Time.time + timeBetweenShots;
        }
        else
        {
            SetAnimatorBoolIfExists("Shoot", false); // Bekleme süresindeyken tetiği bırak
        }
    }

    // ================= YARDIMCI FONKSİYONLAR =================

    void FindNearestEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Collider enemyCollider in enemiesInRange)
        {
            Enemy enemyScript = enemyCollider.GetComponentInParent<Enemy>();
            
            // Eğer düşman scripti varsa ve obje sahnede aktifse (ölmemişse)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, combatStopDistance);
    }
}