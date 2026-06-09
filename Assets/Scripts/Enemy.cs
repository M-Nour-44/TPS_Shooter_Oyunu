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
    public EnemyHealthBar healthBar;

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
    public float walkSpeed = 1.8f;
    public float runSpeed = 4f;

    [Header("Patrol Wait")]
    public float waitAtPointTime = 2f;
    private bool isWaitingAtPoint = false;
    private Coroutine waitAtPointCoroutine;

    [Header("Enemy Shooting Var")]
    public float timebtwShoot = 0.7f;
    bool previouslyShoot;

    [Header("Enemy Accuracy")]
    public float closeAccuracySpread = 0.01f;
    public float farAccuracySpread = 0.08f;
    public float accuracyCloseDistance = 4f;

    [Header("Enemy Hit Animation")]
    public float hitAnimationCooldown = 0.35f;
    private float nextHitAnimationTime = 0f;
    private float hitStunEndTime = 0f;

    [Header("Loot Drop")]
    public GameObject ammoDropPrefab;
    public Transform lootDropPoint;
    [Range(0f, 1f)] public float ammoDropChance = 0.5f;
    public float ammoDropHeight = 0.05f;

    [Header("Enemy Animation and Spark effect")]
    public Animator anim;
    public ParticleSystem muzzleSpark;
    public EnemyWeapon enemyWeapon;

    [Header("Enemy Mood/Situation")]
    public float visionRadius = 20f;
    public float fieldOfViewAngle = 180f;
    public bool useFieldOfView = true;

    [Header("Line of Sight (Duvar Tespiti)")]
    public bool useLineOfSight = true;
    [Tooltip("Düşmanın göz yüksekliği (yerel Y offset)")]
    public float eyeHeight = 1.5f;
    [Tooltip("Düşmanın silahla ateş edebileceği maksimum mesafe")]
    public float shootingRadius = 20f;
    [Tooltip("Hedefin göz yüksekliği (yerel Y offset)")]
    public float targetEyeHeight = 1.3f;
    [Tooltip("Görüşü engelleyen katmanlar (Walls, Environment vb.) — Inspector'dan atayın")]
    public LayerMask obstacleMask;

    public bool playerInVisionRadius;
    public bool playerInShootingRadius;

    [Header("Enemy Hearing")]
    public bool useHearing = true;
    public bool playerHeard;
    public float searchPointRadius = 1.5f;
    public float searchWaitTime = 3f;
    private Vector3 lastHeardPosition;
    private bool hasLastHeardPosition = false;
    private bool isWaitingAtSearchPoint = false;
    private Coroutine searchCoroutine;

    [Header("Gunshot Alert")]
    public bool alertNearbyEnemiesWhenHit = true;
    public float hitAlertRadius = 25f;
    public bool hitAlertMakesEnemiesChasePlayer = true;

    [Header("Stationary Guard")]
    public bool stationaryGuard = false;
    public bool stationaryCanShoot = true;

    private bool isDead = false;
    private bool isAlerted = false;
    
    // Nöbet yerine geri dönmek için
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // --- PERFORMANS: Cache alanları (her frame Find çağrısından kaçınmak için) ---
    private static readonly List<Enemy> allEnemies = new List<Enemy>();
    private Ally cachedAlly;
    private PlayerScript cachedPlayerScript;
    private Transform cachedPlayerTransform;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (enemyAgent == null)
        {
            enemyAgent = GetComponent<NavMeshAgent>();
        }

        if (enemyAgent != null)
        {
            enemyAgent.speed = walkSpeed;
        }

        // ==========================================
        // التعديل الذكي لربط شريط الصحة تلقائياً!
        // ==========================================
        if (healthBar == null)
        {
            // السكريبت سيبحث عن شريط الصحة داخل مجسم العدو تلقائياً
            healthBar = GetComponentInChildren<EnemyHealthBar>();
        }

        presentHealth = enemyHealth;

        if (healthBar != null)
        {
            // تم تعديل الأمر ليتوافق مع السكريبت الجديد
            healthBar.SetMaxHealth(enemyHealth);
        }

        if (playerBody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerBody = player.transform;
            }
        }

        cachedPlayerTransform = playerBody;
        if (cachedPlayerTransform != null)
        {
            cachedPlayerScript = cachedPlayerTransform.GetComponentInParent<PlayerScript>();
            if (cachedPlayerScript == null)
                cachedPlayerScript = cachedPlayerTransform.GetComponent<PlayerScript>();
        }
        cachedAlly = FindObjectOfType<Ally>(); 

        SetEnemyAnimation(0f, false, false);
    }

    private void OnEnable()
    {
        if (!allEnemies.Contains(this))
        {
            allEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        allEnemies.Remove(this);
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        SelectClosestTarget();

        if (stationaryGuard)
        {
            StationaryGuardUpdate();
            return;
        }

        if (!AgentReady())
        {
            return;
        }

        bool playerInsideVision = IsPlayerInsideRadiusAndFOV(visionRadius);
        bool playerInsideShooting = IsPlayerInsideRadiusAndFOV(shootingRadius);

        if (playerInsideVision && playerBody != null)
        {
            lastHeardPosition = playerBody.position;
            hasLastHeardPosition = true;
        }

        playerHeard = useHearing && CanHearPlayer();

        playerInVisionRadius = playerInsideVision;
        playerInShootingRadius = playerInsideShooting;

        if (playerInShootingRadius)
        {
            CancelSearch();
            ShootPlayer();
        }
        else if (playerInVisionRadius)
        {
            CancelSearch();
            PursuePlayer();
        }
        else if (playerHeard || hasLastHeardPosition)
        {
            SearchLastHeardPosition();
        }
        else
        {
            Guard();
        }
    }

    private bool AgentReady()
    {
        return enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh;
    }

    private void SelectClosestTarget()
    {
        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                cachedPlayerTransform = playerObj.transform;
                cachedPlayerScript = playerObj.GetComponentInParent<PlayerScript>()
                    ?? playerObj.GetComponent<PlayerScript>();
            }
        }

        if (cachedAlly == null)
        {
            cachedAlly = FindObjectOfType<Ally>();
        }

        float playerDist = cachedPlayerTransform != null ? Vector3.Distance(transform.position, cachedPlayerTransform.position) : float.MaxValue;
        float allyDist = (cachedAlly != null && cachedAlly.gameObject.activeInHierarchy && !cachedAlly.isDead) ? Vector3.Distance(transform.position, cachedAlly.transform.position) : float.MaxValue;

        bool playerVisible = (playerDist <= visionRadius) && CheckVisibility(cachedPlayerTransform);
        bool allyVisible = (allyDist <= visionRadius) && CheckVisibility(cachedAlly.transform);

        Transform target = null;

        if (playerVisible && allyVisible)
        {
            target = playerDist < allyDist ? cachedPlayerTransform : cachedAlly.transform;
        }
        else if (playerVisible)
        {
            target = cachedPlayerTransform;
        }
        else if (allyVisible)
        {
            target = cachedAlly.transform;
        }
        else
        {
            if (playerDist < allyDist)
            {
                target = cachedPlayerTransform;
            }
            else if (allyDist < float.MaxValue)
            {
                target = cachedAlly.transform;
            }
        }

        if (target != null)
        {
            playerBody = target;
        }
    }

    private void StationaryGuardUpdate()
    {
        if (enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh)
        {
            enemyAgent.isStopped = true;
            enemyAgent.ResetPath();
        }

        bool playerInsideVision = IsPlayerInsideRadiusAndFOV(visionRadius);
        if (playerInsideVision && playerBody != null)
        {
            lastHeardPosition = playerBody.position;
            hasLastHeardPosition = true;
        }

        bool playerInsideShooting = IsPlayerInsideRadiusAndFOV(shootingRadius);
        playerHeard = useHearing && CanHearPlayer();

        playerInVisionRadius = false;
        playerInShootingRadius = playerInsideShooting;

        if (playerInShootingRadius && stationaryCanShoot)
        {
            StationaryShootPlayer();
        }
        else if (hasLastHeardPosition)
        {
            LookAtPositionYOnly(lastHeardPosition);
            SetEnemyAnimation(0f, true, false);
        }
        else
        {
            SetEnemyAnimation(0f, false, false);
        }
    }

    private void StationaryShootPlayer()
    {
        LookAtPlayerYOnly();

        if (!previouslyShoot)
        {
            FireAtPlayer();
        }

        SetEnemyAnimation(0f, true, true);
    }

    private void Guard()
    {
        if (!AgentReady())
        {
            return;
        }

        if (walkPoints == null || walkPoints.Length == 0)
        {
            float distanceToInit = Vector3.Distance(transform.position, initialPosition);
            if (distanceToInit > 0.5f)
            {
                enemyAgent.speed = walkSpeed;
                enemyAgent.isStopped = false;
                enemyAgent.SetDestination(initialPosition);
                SetEnemyAnimation(0.5f, false, false); 
            }
            else
            {
                enemyAgent.isStopped = true;
                enemyAgent.ResetPath();
                transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * 5f);
                SetEnemyAnimation(0f, false, false);
            }
            return;
        }

        if (isWaitingAtPoint)
        {
            enemyAgent.isStopped = true;
            SetEnemyAnimation(0f, false, false);
            return;
        }

        if (walkPoints[currentEnemyPosition] == null)
        {
            ChooseNextWalkPoint();
            return;
        }

        float distanceToPoint = Vector3.Distance(
            transform.position,
            walkPoints[currentEnemyPosition].transform.position
        );

        if (distanceToPoint <= walkingPointRadius)
        {
            waitAtPointCoroutine = StartCoroutine(WaitAtWalkPoint());
            return;
        }

        enemyAgent.speed = walkSpeed;
        enemyAgent.isStopped = false;
        enemyAgent.SetDestination(walkPoints[currentEnemyPosition].transform.position);

        SetEnemyAnimation(0.5f, false, false);
    }

    private IEnumerator WaitAtWalkPoint()
    {
        isWaitingAtPoint = true;

        if (enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh)
        {
            enemyAgent.isStopped = true;
            enemyAgent.ResetPath();
        }

        SetEnemyAnimation(0f, false, false);

        yield return new WaitForSeconds(waitAtPointTime);

        ChooseNextWalkPoint();

        isWaitingAtPoint = false;
        waitAtPointCoroutine = null;
    }

    private void ChooseNextWalkPoint()
    {
        if (walkPoints == null || walkPoints.Length == 0)
        {
            return;
        }

        if (walkPoints.Length == 1)
        {
            currentEnemyPosition = 0;
            return;
        }

        int newPosition = currentEnemyPosition;

        while (newPosition == currentEnemyPosition)
        {
            newPosition = Random.Range(0, walkPoints.Length);
        }

        currentEnemyPosition = newPosition;
    }

    private void CancelPatrolWait()
    {
        if (waitAtPointCoroutine != null)
        {
            StopCoroutine(waitAtPointCoroutine);
            waitAtPointCoroutine = null;
        }

        isWaitingAtPoint = false;
    }

    private void PursuePlayer()
    {
        if (!AgentReady())
        {
            return;
        }

        CancelPatrolWait();

        if (playerBody == null)
        {
            FindPlayer();

            if (playerBody == null)
            {
                return;
            }
        }

        enemyAgent.speed = runSpeed;
        enemyAgent.isStopped = false;
        enemyAgent.SetDestination(playerBody.position);

        SetEnemyAnimation(1f, true, false);
    }

    private void ShootPlayer()
    {
        if (!AgentReady())
        {
            return;
        }

        CancelPatrolWait();

        enemyAgent.isStopped = true;
        enemyAgent.velocity = Vector3.zero;
        enemyAgent.ResetPath();
        enemyAgent.speed = 0f;

        LookAtPlayerYOnly();

        if (!previouslyShoot)
        {
            FireAtPlayer();
        }

        SetEnemyAnimation(0f, true, true);
    }

    private void SearchLastHeardPosition()
    {
        if (!AgentReady())
        {
            return;
        }

        if (!hasLastHeardPosition)
        {
            return;
        }

        CancelPatrolWait();

        if (isWaitingAtSearchPoint)
        {
            enemyAgent.isStopped = true;
            SetEnemyAnimation(0f, true, false);
            return;
        }

        float distanceToHeardPosition = Vector3.Distance(transform.position, lastHeardPosition);

        if (distanceToHeardPosition <= searchPointRadius)
        {
            searchCoroutine = StartCoroutine(WaitAtSearchPoint());
            return;
        }

        enemyAgent.speed = runSpeed;
        enemyAgent.isStopped = false;
        enemyAgent.SetDestination(lastHeardPosition);

        SetEnemyAnimation(1f, true, false);
    }

    private IEnumerator WaitAtSearchPoint()
    {
        isWaitingAtSearchPoint = true;

        if (enemyAgent != null && enemyAgent.enabled && enemyAgent.isOnNavMesh)
        {
            enemyAgent.isStopped = true;
            enemyAgent.ResetPath();
        }

        SetEnemyAnimation(0f, true, false);

        yield return new WaitForSeconds(searchWaitTime);

        hasLastHeardPosition = false;
        isWaitingAtSearchPoint = false;
        searchCoroutine = null;
        isAlerted = false;

        SetEnemyAnimation(0f, false, false);
    }

    private void CancelSearch()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }

        isWaitingAtSearchPoint = false;
    }

    private void FireAtPlayer()
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

            float distanceToPlayer = shootingRadius;

            if (playerBody != null)
            {
                distanceToPlayer = Vector3.Distance(transform.position, playerBody.position);
            }

            float distanceFactor = Mathf.InverseLerp(accuracyCloseDistance, shootingRadius, distanceToPlayer);
            float currentSpread = Mathf.Lerp(closeAccuracySpread, farAccuracySpread, distanceFactor);

            Vector3 shootDirection =
                ShootingRaycastArea.transform.forward +
                ShootingRaycastArea.transform.right * Random.Range(-currentSpread, currentSpread) +
                ShootingRaycastArea.transform.up * Random.Range(-currentSpread, currentSpread);

            shootDirection.Normalize();

            if (Physics.Raycast(ShootingRaycastArea.transform.position, shootDirection, out hit, shootingRadius))
            {
                PlayerScript player = hit.transform.GetComponentInParent<PlayerScript>();
                Ally ally = hit.transform.GetComponentInParent<Ally>(); 

                if (player != null)
                {
                    player.playerHitDamage(giveDamage);
                }
                else if (ally != null)
                { 
                    ally.allyHitDamage(giveDamage);
                }
            }
        }

        previouslyShoot = true;
        Invoke(nameof(ActiveShooting), timebtwShoot);
    }

    private bool CanHearPlayer()
    {
        if (cachedPlayerTransform == null)
        {
            return false;
        }

        if (cachedPlayerScript == null)
        {
            cachedPlayerScript = cachedPlayerTransform.GetComponentInParent<PlayerScript>();
            if (cachedPlayerScript == null) return false;
        }

        float noiseRadius = cachedPlayerScript.GetCurrentNoiseRadius();

        if (noiseRadius <= 0f)
        {
            return false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, cachedPlayerTransform.position);

        if (distanceToPlayer <= noiseRadius)
        {
            lastHeardPosition = cachedPlayerTransform.position;
            hasLastHeardPosition = true;
            return true;
        }

        return false;
    }

    private bool IsPlayerInsideRadiusAndFOV(float radius)
    {
        if (playerBody == null) return false;

        float distance = Vector3.Distance(transform.position, playerBody.position);

        if (distance > radius) return false;

        return CheckVisibility(playerBody);
    }

    private bool CheckVisibility(Transform targetTransform)
    {
        if (targetTransform == null) return false;

        if (useFieldOfView)
        {
            Vector3 directionToTarget = targetTransform.position - transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);
                if (angle > fieldOfViewAngle * 0.5f) return false; 
            }
        }

        if (useLineOfSight)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPos = GetTargetPos(targetTransform);
            Vector3 direction = targetPos - origin;
            float dist = direction.magnitude;

            if (dist > 0f && Physics.Raycast(origin, direction.normalized, out RaycastHit hit, dist, obstacleMask))
            {
                if (!hit.transform.IsChildOf(targetTransform) && hit.transform != targetTransform)
                {
                    return false; 
                }
            }
        }

        return true;
    }

    private Vector3 GetTargetPos(Transform target)
    {
        Collider c = target.GetComponent<Collider>();
        if (c != null)
        {
            return c.bounds.center + Vector3.up * (c.bounds.extents.y * 0.5f);
        }
        return target.position + Vector3.up * targetEyeHeight;
    }

    private void LookAtPlayerYOnly()
    {
        if (playerBody != null)
        {
            LookAtPositionYOnly(playerBody.position);
        }
    }

    private void LookAtPositionYOnly(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
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
    }

    public void enemyHitDamage(float takeDamage)
    {
        if (isDead)
        {
            return;
        }

        presentHealth -= takeDamage;

        AlertEnemy();

        if (alertNearbyEnemiesWhenHit)
        {
            AlertEnemiesAround(transform.position, hitAlertRadius, hitAlertMakesEnemiesChasePlayer);
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(presentHealth);
        }

        if (presentHealth <= 0)
        {
            EnemyDie();
        }
        else
        {
            if (Time.time >= nextHitAnimationTime)
            {
                SetAnimatorTriggerIfExists("Hit");
                nextHitAnimationTime = Time.time + hitAnimationCooldown;
            }
        }
    }

    public void ReceiveGunshotAlert(Vector3 noisePosition, bool chasePlayer)
    {
        if (isDead)
        {
            return;
        }

        lastHeardPosition = noisePosition;
        hasLastHeardPosition = true;

        if (chasePlayer)
        {
            isAlerted = true;
        }

        CancelPatrolWait();
        CancelSearch();

        if (playerBody == null)
        {
            FindPlayer();
        }
    }

    public static void AlertEnemiesAround(Vector3 position, float radius, bool chasePlayer)
    {
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = allEnemies[i];

            if (enemy == null)
            {
                allEnemies.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(enemy.transform.position, position);

            if (distance <= radius)
            {
                enemy.ReceiveGunshotAlert(position, chasePlayer);
            }
        }
    }

    private void AlertEnemy()
    {
        if (isDead)
        {
            return;
        }

        isAlerted = true;

        CancelPatrolWait();
        CancelSearch();

        if (playerBody == null)
        {
            FindPlayer();
        }

        if (playerBody != null)
        {
            lastHeardPosition = playerBody.position;
            hasLastHeardPosition = true;
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerBody = player.transform;
            cachedPlayerTransform = playerBody;
            if (cachedPlayerScript == null)
            {
                cachedPlayerScript = player.GetComponentInParent<PlayerScript>()
                    ?? player.GetComponent<PlayerScript>();
            }
        }
    }

    private void EnemyDie()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        CancelPatrolWait();
        CancelSearch();
        CancelInvoke(nameof(ActiveShooting));

        playerInVisionRadius = false;
        playerInShootingRadius = false;
        playerHeard = false;
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

        DisableEnemyColliders();

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        if (muzzleSpark != null)
        {
            muzzleSpark.Stop();
        }

        if (anim != null)
        {
            SetAnimatorFloatIfExists("Speed", 0f);
            SetAnimatorBoolIfExists("Shoot", false);
            SetAnimatorBoolIfExists("IsAiming", false);
            SetAnimatorBoolIfExists("Die", true);

            SetAnimatorBoolIfExists("Walk", false);
            SetAnimatorBoolIfExists("AimRun", false);
            SetAnimatorBoolIfExists("Running", false);
        }

        BossMissionTarget bossMissionTarget = GetComponent<BossMissionTarget>();

        if (bossMissionTarget != null)
        {
            bossMissionTarget.CompleteBossMission();
        }

        DropAmmo();

        Destroy(gameObject, 5f);
    }

    private void DisableEnemyColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void DropAmmo()
    {
        if (ammoDropPrefab == null)
        {
            return;
        }

        if (Random.value > ammoDropChance)
        {
            return;
        }

        Vector3 dropPosition = transform.position + Vector3.up * ammoDropHeight;

        if (lootDropPoint != null)
        {
            dropPosition = lootDropPoint.position + Vector3.up * ammoDropHeight;
        }

        GameObject ammoObject = Instantiate(ammoDropPrefab, dropPosition, Quaternion.identity);

        AmmoPickup ammoPickup = ammoObject.GetComponent<AmmoPickup>();

        if (ammoPickup != null)
        {
            ammoPickup.magazineAmount = 1;
        }
    }

    private void SetEnemyAnimation(float speed, bool isAiming, bool isShooting)
    {
        if (anim == null)
        {
            return;
        }

        SetAnimatorFloatIfExists("Speed", speed);
        SetAnimatorBoolIfExists("IsAiming", isAiming);
        SetAnimatorBoolIfExists("Shoot", isShooting);
        SetAnimatorBoolIfExists("Die", false);

        SetAnimatorBoolIfExists("Walk", false);
        SetAnimatorBoolIfExists("AimRun", false);
        SetAnimatorBoolIfExists("Running", false);
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

    private void SetAnimatorTriggerIfExists(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            anim.SetTrigger(parameterName);
        }
    }
}