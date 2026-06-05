using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Ally : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public Transform player;
    public float followDistance = 3f;

    [Header("Hız Ayarları")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float runDistanceThreshold = 6f;

    [Header("Savaş Ayarları (Algılama)")]
    public float detectionRadius = 15f;
    public LayerMask enemyLayer;

    [Header("Savaş Ayarları (Ateş Etme)")]
    public float giveDamage = 10f; // Düşmana verilecek hasar
    public float timeBetweenShots = 0.5f; // İki atış arasındaki süre (Fire Rate)
    public Transform shootingPoint; // Merminin çıkacağı nokta (Silahın ucu)
    
    [Header("Savaş Efektleri (Opsiyonel)")]
    public ParticleSystem muzzleSpark; // Silah ateşi efekti
    public AudioSource audioSource; // Ses kaynağı
    public AudioClip shootingSound; // Ateş sesi

    private Transform currentTarget;
    private NavMeshAgent agent;
    private Animator animator;
    
    // Ateş etme zamanlayıcısı
    private float nextTimeToShoot = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = followDistance;
    }

    void Update()
    {
        // 1. Etrafta düşman var mı kontrol et
        FindNearestEnemy();

        if (currentTarget != null)
        {
            // --- SAVAŞ DURUMU ---
            
            // Hedefe olan mesafeyi hesapla
            float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);

            // Düşman algılama menzilinden çıktıysa hedefi bırak
            if (distanceToEnemy > detectionRadius)
            {
                currentTarget = null;
                return;
            }

            // Olduğu yerde dur
            agent.SetDestination(transform.position);

            // Yüzünü düşmana dön (Sadece Y ekseninde dönmeli ki karakter yere veya göğe bakıp eğilmesin)
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);

            // Ateş Etme Mantığı (Bekleme süresi dolduysa)
            if (Time.time >= nextTimeToShoot)
            {
                ShootAtEnemy();
                nextTimeToShoot = Time.time + timeBetweenShots; // Bir sonraki atış zamanını kur
            }
        }
        else if (player != null)
        {
            // --- TAKİP DURUMU ---
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > runDistanceThreshold)
                agent.speed = runSpeed;
            else
                agent.speed = walkSpeed;

            agent.SetDestination(player.position);
        }

        // Animasyon Güncellemesi
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }

    void FindNearestEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Collider enemyCollider in enemiesInRange)
        {
            // Düşmanın ana objesini bul (Enemy scriptinin olduğu yer)
            Enemy enemyScript = enemyCollider.GetComponentInParent<Enemy>();
            
            // Eğer düşman ölmüşse (isDead değişkeni true ise), onu hedef alma!
            // Not: Enemy scriptinde isDead private olduğu için canını (presentHealth) kontrol edeceğiz.
            // Fakat presentHealth de private, o yüzden objenin aktifliğine bakmak en garantisi
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
        // Eğer mermi çıkış noktası atanmamışsa kameranın veya karakterin merkezinden at
        Vector3 rayOrigin = shootingPoint != null ? shootingPoint.position : transform.position + Vector3.up * 1.5f;
        
        // Düşmanın merkezine doğru nişan al (Göğüs hizası için biraz yukarı)
        Vector3 targetDirection = (currentTarget.position + Vector3.up * 1.5f) - rayOrigin;

        // Efektleri oynat
        if (muzzleSpark != null) muzzleSpark.Play();
        if (audioSource != null && shootingSound != null) audioSource.PlayOneShot(shootingSound);

        // Raycast fırlat
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, targetDirection.normalized, out hit, detectionRadius))
        {
            // Eğer ışın (ray) düşmana çarptıysa
            Enemy hitEnemy = hit.transform.GetComponentInParent<Enemy>();
            if (hitEnemy != null)
            {
                // Düşmana hasar ver!
                hitEnemy.enemyHitDamage(giveDamage);
                
                // Konsola bilgi yazdır (test için)
                Debug.Log("Ally, " + hitEnemy.name + " hedefine ateş etti ve " + giveDamage + " hasar verdi!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Mermi çıkış noktasından hedefi görmek için çizgi çiz (Sadece editörde)
        if (shootingPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(shootingPoint.position, shootingPoint.forward * detectionRadius);
        }
    }
}