using UnityEngine;

public class BombDefusal : MonoBehaviour
{
    [Header("Bomb Settings")]
    public float interactRange = 3f;
    private bool isDefused = false;
    private bool isExploded = false; // للتأكد من أن القنبلة لم تنفجر بعد

    [Header("References")]
    public Transform player;

    [Header("Audio Settings (3 Phases)")]
    public AudioSource bombAudioSource;
    public AudioClip phase1Clip;       // الملف الأول (مدته 30 ثانية)
    public AudioClip phase2Clip;       // الملف الثاني (مدته 10 ثواني - تكتكة سريعة)
    public AudioClip explosionClip;    // الملف الثالث (الانفجار أو نهاية الوقت) يشتغل مرة واحدة
    public AudioClip defuseSuccessClip; // صوت النجاح عند التفكيك بـ E

    [Header("Point Lights Settings")]
    public Light redLight;         
    public Light greenLight;       
    public float blinkInterval = 1f; 
    private float blinkTimer = 0f;

    [Header("Text Settings")]
    public GameObject activatedText; 
    public GameObject defusedText;   
    public GameObject failedText;    // نص اختياري يظهر إذا انفجرت القنبلة (مثلاً FAILED)

    [Header("Visual Settings (Emission)")]
    public MeshRenderer[] bombRenderers; 
    [ColorUsage(true, true)]
    public Color glowColor = Color.red; 
    private Material[] bombMaterials;

    // العداد الزمني الداخلي (30 + 10 = 40 ثانية إجمالاً)
    private float bombCountdown = 40f; 
    private bool switchedToPhase2 = false;

    void Start()
    {
        // 1. تشغيل الملف الصوتي الأول تلقائياً عند بدء المهمة
        if (bombAudioSource != null && phase1Clip != null)
        {
            bombAudioSource.clip = phase1Clip;
            bombAudioSource.loop = true; 
            bombAudioSource.Play();
        }

        // 2. إعداد الـ Emission الثابت
        bombMaterials = new Material[bombRenderers.Length]; 
        for (int i = 0; i < bombRenderers.Length; i++)
        {
            if (bombRenderers[i] != null)
            {
                bombMaterials[i] = bombRenderers[i].material;
                bombMaterials[i].EnableKeyword("_EMISSION"); 
                bombMaterials[i].SetColor("_EmissionColor", glowColor);
            }
        }

        // 3. إعداد الأضواء والنصوص الافتراضية
        if (redLight != null) redLight.enabled = true;
        if (greenLight != null) greenLight.enabled = false; 
        if (activatedText != null) activatedText.SetActive(true);
        if (defusedText != null) defusedText.SetActive(false);
        if (failedText != null) failedText.SetActive(false);
    }

    void Update()
    {
        // إذا فكك اللاعب القنبلة أو انفجرت بالفعل، يتوقف العداد تماماً
        if (isDefused || isExploded || player == null) 
            return;

        // طرح الوقت المستغرق من العداد في كل إطار (Frame)
        bombCountdown -= Time.deltaTime;

        // ---- إدارة المراحل الصوتية ----
        
        // الانتقال إلى الملف الثاني (عندما يتبقى 10 ثوانٍ أو أقل)
        if (bombCountdown <= 10f && !switchedToPhase2)
        {
            switchedToPhase2 = true;
            PlayNewPhaseSound(phase2Clip, true);
            
            // تسريع وميض الضوء الأحمر في آخر 10 ثوانٍ لزيادة التوتر!
            blinkInterval = 0.25f; 
        }

        // انتهاء الوقت تماماً وتشغيل الملف الثالث (الانفجار)
        if (bombCountdown <= 0f)
        {
            ExplodeBomb();
            return; // الخروج من الدالة فوراً
        }

        // --- تأثير الوميض الحاد للضوء الأحمر ---
        if (redLight != null)
        {
            blinkTimer += Time.deltaTime; 
            if (blinkTimer >= blinkInterval)
            {
                redLight.enabled = !redLight.enabled;
                blinkTimer = 0f; 
            }
        }

        // --- نظام التفكيك بضغط زر E ---
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= interactRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                DefuseBomb();
            }
        }
    }

    void PlayNewPhaseSound(AudioClip newClip, bool shouldLoop)
    {
        if (bombAudioSource != null && newClip != null)
        {
            bombAudioSource.Stop();
            bombAudioSource.clip = newClip;
            bombAudioSource.loop = shouldLoop;
            bombAudioSource.Play();
        }
    }

    void DefuseBomb()
    {
        isDefused = true;

        if (bombAudioSource != null)
        {
            bombAudioSource.Stop(); 
            if (defuseSuccessClip != null)
            {
                bombAudioSource.PlayOneShot(defuseSuccessClip);
            }
        }

        TurnOffVisuals();

        if (redLight != null) redLight.enabled = false;
        if (greenLight != null) greenLight.enabled = true;

        if (activatedText != null) activatedText.SetActive(false);
        if (defusedText != null) defusedText.SetActive(true);

        Debug.Log("تم إنقاذ الموقف وتفكيك القنبلة بنجاح!");
    }

    void ExplodeBomb()
    {
        isExploded = true;

        if (bombAudioSource != null)
        {
            bombAudioSource.Stop();
            // تشغيل الملف الصوتي الثالث مرة واحدة فقط
            if (explosionClip != null)
            {
                bombAudioSource.PlayOneShot(explosionClip);
            }
        }

        TurnOffVisuals();
        if (redLight != null) redLight.enabled = false;

        // تحديث الشاشات لإظهار الفشل
        if (activatedText != null) activatedText.SetActive(false);
        if (failedText != null) failedText.SetActive(true);

        Debug.Log("بوووم! انتهى الوقت وانفجرت القنبلة!");
        
        // 💡 يمكنك هنا استدعاء دالة لإنقاص صحة اللاعب أو فتح قائمة الموت (PlayerDie)
    }

    void TurnOffVisuals()
    {
        foreach (Material mat in bombMaterials)
        {
            if (mat != null) 
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}