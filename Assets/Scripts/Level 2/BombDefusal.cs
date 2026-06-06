using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BombDefusal : MonoBehaviour
{
    [Header("Bomb Settings")]
    public float interactRange = 3f;
    private bool isDefused = false;
    private bool isExploded = false;
    private bool isPaused = false;

    [Header("Bomb Countdown Timer")]
    public float bombTotalTime = 300f;
    public float phase2StartTime = 30f;
    public float phase3StartTime = 2f; // 👈 تمت إضافة وقت المرحلة الثالثة هنا
    public TextMeshProUGUI bombTimerText;
    private float bombCountdown;
    private bool switchedToPhase2 = false;
    private bool switchedToPhase3 = false; // 👈 تمت إضافة هذا المتغير لضمان عدم تكرار التشغيل

    [Header("References")]
    public Transform player;

    [Header("Game Over On Explosion")]
    public bool killPlayerOnExplosion = true;
    public float explosionDamage = 9999f;

    [Header("Cinematic Link")]
    public CinematicManager cinematicManager; // لربط سكريبت المشهد السينمائي

    [Header("Audio Settings")]
    public AudioSource bombAudioSource;
    public AudioSource explosionAudioSource;
    public AudioClip phase1Clip;
    public AudioClip phase2Clip;
    public AudioClip phase3Clip; // 👈 تمت إضافة مقطع الصوت الثالث هنا
    public AudioClip explosionClip;
    public AudioClip defuseSuccessClip;

    [Header("Point Lights Settings")]
    public Light redLight;
    public Light greenLight;
    public float blinkInterval = 1f;
    private float blinkTimer = 0f;

    [Header("Text Settings")]
    public GameObject activatedText;
    public GameObject defusedText;
    public GameObject failedText;

    [Header("Defuse UI")]
    public GameObject defusePanel;
    public Slider defuseSlider;
    public TextMeshProUGUI defuseMessageText;
    public string defuseMessage = "DEFUSING BOMB...";
    public float defuseHoldTime = 5f;
    public bool resetProgressWhenReleased = true;

    [Header("Visual Settings")]
    public MeshRenderer[] bombRenderers;
    [ColorUsage(true, true)]
    public Color glowColor = Color.red;
    private Material[] bombMaterials;

    [Header("Mission")]
    public bool completeMissionAfterDefuse = true;
    public int requiredPreviousMission = 2;
    public int missionNumber = 3;
    public string missionCompleteText = "3.Bomb defused";

    private float defuseProgress = 0f;

    void Start()
    {
        bombCountdown = bombTotalTime;
        UpdateBombTimerUI();

        if (bombAudioSource != null && phase1Clip != null)
        {
            bombAudioSource.ignoreListenerPause = false;
            bombAudioSource.clip = phase1Clip;
            bombAudioSource.loop = true;
            bombAudioSource.Play();
        }

        if (explosionAudioSource != null)
        {
            explosionAudioSource.ignoreListenerPause = true;
            explosionAudioSource.loop = false;
            explosionAudioSource.playOnAwake = false;
        }

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

        if (redLight != null)
        {
            redLight.enabled = true;
        }

        if (greenLight != null)
        {
            greenLight.enabled = false;
        }

        if (activatedText != null)
        {
            activatedText.SetActive(true);
        }

        if (defusedText != null)
        {
            defusedText.SetActive(false);
        }

        if (failedText != null)
        {
            failedText.SetActive(false);
        }

        SetupDefuseUI();
    }

    void Update()
    {
        if (isDefused || isExploded || player == null)
        {
            return;
        }

        if (isPaused || Time.timeScale == 0f)
        {
            return;
        }

        bombCountdown -= Time.deltaTime;

        if (bombCountdown < 0f)
        {
            bombCountdown = 0f;
        }

        UpdateBombTimerUI();

        if (bombCountdown <= phase2StartTime && !switchedToPhase2)
        {
            switchedToPhase2 = true;
            PlayNewPhaseSound(phase2Clip, true);
            blinkInterval = 0.25f;
        }

        // 👈 هذا هو كود تشغيل الطنين المستمر في آخر ثانيتين
        if (bombCountdown <= phase3StartTime && !switchedToPhase3)
        {
            switchedToPhase3 = true;
            PlayNewPhaseSound(phase3Clip, true); // true لكي يستمر الطنين بالعمل (Loop)
        }

        if (bombCountdown <= 0f)
        {
            ExplodeBomb();
            return;
        }

        if (redLight != null)
        {
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= blinkInterval)
            {
                redLight.enabled = !redLight.enabled;
                blinkTimer = 0f;
            }
        }

        HandleDefuseInput();
    }

    private void SetupDefuseUI()
    {
        if (defusePanel != null)
        {
            defusePanel.SetActive(false);
        }

        if (defuseSlider != null)
        {
            defuseSlider.minValue = 0f;
            defuseSlider.maxValue = 1f;
            defuseSlider.value = 0f;
        }

        if (defuseMessageText != null)
        {
            defuseMessageText.text = defuseMessage;
        }
    }

    private void HandleDefuseInput()
    {
        // sqrMagnitude kullan: kare kök hesabından kaçınır, her frame çalıştığı için daha performanslı
        float sqrDist = (transform.position - player.position).sqrMagnitude;

        if (sqrDist > interactRange * interactRange)
        {
            ResetDefuseProgress();
            return;
        }

        if (!CanDefuseNow())
        {
            ResetDefuseProgress();
            return;
        }

        if (Input.GetKey(KeyCode.E))
        {
            FillDefuseBar();
        }
        else
        {
            if (resetProgressWhenReleased)
            {
                ResetDefuseProgress();
            }
            else
            {
                DecreaseDefuseBar();
            }
        }
    }

    private bool CanDefuseNow()
    {
        if (!completeMissionAfterDefuse)
        {
            return true;
        }

        if (Level2MissionManager.instance == null)
        {
            return true;
        }

        return Level2MissionManager.instance.IsMissionCompleted(requiredPreviousMission);
    }

    private void FillDefuseBar()
    {
        ShowDefuseUI();

        defuseProgress += Time.deltaTime / defuseHoldTime;
        defuseProgress = Mathf.Clamp01(defuseProgress);

        UpdateDefuseUI();

        if (defuseProgress >= 1f)
        {
            DefuseBomb();
        }
    }

    private void DecreaseDefuseBar()
    {
        defuseProgress -= Time.deltaTime / 2f;
        defuseProgress = Mathf.Clamp01(defuseProgress);

        UpdateDefuseUI();

        if (defuseProgress <= 0f)
        {
            HideDefuseUI();
        }
    }

    private void ResetDefuseProgress()
    {
        defuseProgress = 0f;
        UpdateDefuseUI();
        HideDefuseUI();
    }

    private void UpdateDefuseUI()
    {
        if (defuseSlider != null)
        {
            defuseSlider.value = defuseProgress;
        }

        if (defuseMessageText != null)
        {
            defuseMessageText.text = defuseMessage;
        }
    }

    private void UpdateBombTimerUI()
    {
        if (bombTimerText == null)
        {
            return;
        }

        float timeToDisplay = Mathf.Max(bombCountdown, 0f);

        float minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60f);

        bombTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ShowDefuseUI()
    {
        if (defusePanel != null && !defusePanel.activeSelf)
        {
            defusePanel.SetActive(true);
        }
    }

    private void HideDefuseUI()
    {
        if (defusePanel != null)
        {
            defusePanel.SetActive(false);
        }
    }

    public void PauseBomb()
    {
        if (isExploded)
        {
            return;
        }

        isPaused = true;
        HideDefuseUI();

        if (bombAudioSource != null && bombAudioSource.isPlaying)
        {
            bombAudioSource.Pause();
        }
    }

    public void ResumeBomb()
    {
        if (isExploded)
        {
            return;
        }

        isPaused = false;

        if (!isDefused && bombAudioSource != null && bombAudioSource.clip != null)
        {
            bombAudioSource.UnPause();
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
        if (isDefused)
        {
            return;
        }

        isDefused = true;
        HideDefuseUI();

        if (bombAudioSource != null)
        {
            bombAudioSource.Stop();

            if (defuseSuccessClip != null)
            {
                bombAudioSource.PlayOneShot(defuseSuccessClip);
            }
        }

        TurnOffVisuals();

        if (redLight != null)
        {
            redLight.enabled = false;
        }

        if (greenLight != null)
        {
            greenLight.enabled = true;
        }

        if (activatedText != null)
        {
            activatedText.SetActive(false);
        }

        if (defusedText != null)
        {
            defusedText.SetActive(true);
        }

        if (completeMissionAfterDefuse && Level2MissionManager.instance != null)
        {
            Level2MissionManager.instance.CompleteMission(missionNumber, missionCompleteText);
        }

        Debug.Log("Bomb defused successfully!");
    }

    void ExplodeBomb()
    {
        if (isExploded)
        {
            return;
        }

        isExploded = true;
        HideDefuseUI();

        bombCountdown = 0f;
        UpdateBombTimerUI();

        if (bombAudioSource != null)
        {
            bombAudioSource.Stop();
        }

        if (explosionAudioSource != null && explosionClip != null)
        {
            explosionAudioSource.ignoreListenerPause = true;
            explosionAudioSource.PlayOneShot(explosionClip);
        }
        else if (bombAudioSource != null && explosionClip != null)
        {
            bombAudioSource.ignoreListenerPause = true;
            bombAudioSource.PlayOneShot(explosionClip);
        }

        TurnOffVisuals();

        if (redLight != null)
        {
            redLight.enabled = false;
        }

        if (greenLight != null)
        {
            greenLight.enabled = false;
        }

        if (activatedText != null)
        {
            activatedText.SetActive(false);
        }

        if (failedText != null)
        {
            failedText.SetActive(true);
        }
        // في اللحظة التي ينتهي فيها العداد أو تنفجر القنبلة، أضف هذا الشرط:
        if (cinematicManager != null)
        {
            cinematicManager.StartExplosionCinematic(); // إعطاء إشارة البدء للمخرج
        }
        KillPlayerAfterExplosion();

        Debug.Log("Bomb exploded! Player died.");
    }

    private void KillPlayerAfterExplosion()
    {
        if (!killPlayerOnExplosion)
        {
            return;
        }

        PlayerScript playerScript = null;

        if (player != null)
        {
            playerScript = player.GetComponent<PlayerScript>();

            if (playerScript == null)
            {
                playerScript = player.GetComponentInParent<PlayerScript>();
            }

            if (playerScript == null)
            {
                playerScript = player.GetComponentInChildren<PlayerScript>();
            }
        }

        if (playerScript != null)
        {
            playerScript.playerHitDamage(explosionDamage);
        }
    }

    void TurnOffVisuals()
    {
        if (bombMaterials == null)
        {
            return;
        }

        foreach (Material mat in bombMaterials)
        {
            if (mat != null)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}