using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    

    [Header("Enemy Health and Damage")]
    private float enemyHealth = 120f;
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
    public float timebtwShoot;
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

    


    private void Awake()
    {
       

        enemyAgent = GetComponent<NavMeshAgent>();
        enemyAgent.speed = enemySpeed;
        presentHealth = enemyHealth;
        healthBar.GiveFullHealth(enemyHealth);

        // Eğer inspector'dan verilmemişse otomatik bul
        if (playerBody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerBody = player.transform;
            }
        }
    }

    private void Update()
    {
        playerInVisionRadius =
            Physics.CheckSphere(transform.position, visionRadius, PlayerLayer);

        playerInShootingRadius =
            Physics.CheckSphere(transform.position, shootingRadius, PlayerLayer);

        // STATE SYSTEM
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

    private void Guard()
    {
        if (walkPoints.Length == 0) return;

        // Walkpoint'e ulaştıysa yeni hedef seç
        if (Vector3.Distance(
            transform.position,
            walkPoints[currentEnemyPosition].transform.position)
            <= walkingPointRadius)
        {
            currentEnemyPosition = Random.Range(0, walkPoints.Length);
        }

        // NavMeshAgent hareket ettiriyor
        enemyAgent.SetDestination(
            walkPoints[currentEnemyPosition].transform.position
        );
    }

    private void PursuePlayer()
    {
        if (playerBody == null) return;

        enemyAgent.isStopped = false;

        enemyAgent.SetDestination(playerBody.position);
        
            anim.SetBool("Walk", false);
            anim.SetBool("AimRun", true);
            anim.SetBool("Shoot", false);
            anim.SetBool("Die", false);
        
    }

    private void ShootPlayer()
    {
        // Şimdilik durdur
        enemyAgent.SetDestination(transform.position);

        // Oyuncuya bak
        transform.LookAt(LookPoint);

        if(!previouslyShoot)
        {

            muzzleSpark.Play();
            enemyWeapon.PlayShootingSound();
            

            RaycastHit hit;
            if(Physics.Raycast(ShootingRaycastArea.transform.position, ShootingRaycastArea.transform.forward, out hit, shootingRadius))
            {
                Debug.Log("Shooting" + hit.transform.name);
                // Buraya oyuncuya hasar verme kodunu ekleyebilirsin
                PlayerScript playerBody = hit.transform.GetComponent<PlayerScript>();

                if(playerBody != null)
                {
                    playerBody.playerHitDamage(giveDamage);
                }

            }

            previouslyShoot = true;
            Invoke(nameof(ActiveShooting), timebtwShoot);
        }

        // Buraya ateş etme animasyonu koyabilirsin
        anim.SetBool("Shoot", true);
        anim.SetBool("Walk", false);
        anim.SetBool("AimRun", false);
        anim.SetBool("Die", false);
    }
    

    

    // Vision ve shooting radiusları görmek için
    /*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRadius);
    }
    */
    private void ActiveShooting()
    {
        previouslyShoot = false;
        anim.SetBool("Shoot", false);

    }

    public void enemyHitDamage(float takeDamage)
    {
        presentHealth -= takeDamage;
        healthBar.SetHealth(presentHealth);

        if (presentHealth <= 0)
        {
            anim.SetBool("Shoot", false);
            anim.SetBool("Walk", false);
            anim.SetBool("AimRun", false);
            anim.SetBool("Die", true);
            enemyDie();
        }
    }

    private void enemyDie()
{
    enemyAgent.SetDestination(transform.position); 
    enemyAgent.enabled = false; 
    
    shootingRadius = 0f;
    visionRadius = 0f;
    playerInVisionRadius = false;
    playerInShootingRadius = false;
    Object.Destroy(gameObject, 5.0f);
}
}