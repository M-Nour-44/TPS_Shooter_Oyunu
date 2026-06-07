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

    [Header("Telsiz Sesleri (Voice Lines)")]
    public AudioClip[] acknowledgeMoveSounds;    // F ile zemine tıklayınca (Örn: "Moving out", "Copy that")
    public AudioClip[] acknowledgeAttackSounds;  // F ile düşmana tıklayınca (Örn: "Engaging target", "I have the shot")
    public AudioClip[] acknowledgeRegroupSounds; // G tuşuna basınca (Örn: "Falling back", "On my way")

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

    // ================= SADELEŞTİRİLMİŞ TAKTİKSEL EMİRLER =================

    // G TUŞUNA BASILDIĞINDA ÇALIŞIR: Tüm emirleri iptal eder ve oyuncuya döner
    public void CommandRegroup()
    {
        if (isDead) return;

        // --- YENİ: Regroup sesi çal ---
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

    // F TUŞU DÜŞMANA BASINCA: İşaretli düşmana kilitlenir
    public void CommandAttackTarget(Transform targetEnemy)
    {
        if (isDead) return;

        // --- YENİ: Saldırı sesi çal ---
        PlayVoiceLine(acknowledgeAttackSounds);

        isCommandActive = true;
        currentState = AllyState.CommandFocusFire; 
        currentTarget = targetEnemy;               
    }

    // F TUŞU ZEMİNE BASINCA: Oraya gider ve bekler
    public void CommandMoveToLocation(Vector3 targetPos)
    {
        if (isDead) return;

        // --- YENİ: Hareket sesi çal ---
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
        }
    }

    void UpdateCommandMoveToState()
    {
        agent.updateRotation = true; 

        SetAnimatorBoolIfExists("IsAiming", false);
        SetAnimatorBoolIfExists("Shoot", false);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    // ================= DURUM GÜNCELLEMELERİ =================

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

        UpdateCombatState(); 
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

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToEnemy > combatStopDistance) 
        {
            agent.isStopped = false;
            agent.speed = runSpeed; 
            agent.stoppingDistance = combatStopDistance; 
            agent.updateRotation = true; 
            
            SetAnimatorBoolIfExists("IsAiming", false); 
            SetAnimatorBoolIfExists("Shoot", false); 

            if (!agent.hasPath || Vector3.Distance(agent.destination, currentTarget.position) > 1.5f)
            {
                agent.SetDestination(currentTarget.position);
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
            agent.updateRotation = false; 
            
            SetAnimatorBoolIfExists("IsAiming", true); 

            Vector3 direction = (currentTarget.position - transform.position).normalized;
            if (direction != Vector3.zero) 
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
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

    // ================= TELSİZ SES SİSTEMİ =================
    private void PlayVoiceLine(AudioClip[] voiceClips)
    {
        // Ses listesi boşsa veya adam öldüyse çalma
        if (voiceClips == null || voiceClips.Length == 0 || isDead || audioSource == null) return;

        // SPAM ENGELLEYİCİ: Eğer şu anki zaman, adamın konuşmasının bitiş zamanından erkense yeni ses ÇALMA!
        if (Time.time < nextVoiceTime) return; 

        // Rastgele bir ses seç
        int randomIndex = Random.Range(0, voiceClips.Length);
        AudioClip selectedClip = voiceClips[randomIndex];

        if (selectedClip != null)
        {
            // PlayOneShot yerine direkt hoparlöre atayıp çalıyoruz
            audioSource.clip = selectedClip;
            audioSource.Play();

            // SİHİRLİ KISIM: Sisteme "Bu sesin uzunluğu ne kadarsa (örn: 1.5 saniye), o süre bitene kadar telsizi kilitle" diyoruz. 
            // Sonuna eklediğimiz + 0.5f ise konuşmalar arasına yarım saniyelik gerçekçi bir nefes alma/telsiz bekleme süresi koyar.
            nextVoiceTime = Time.time + selectedClip.length + 0.5f;
        }
    }
}