using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BombDefusal : MonoBehaviour
{
    [Header("Bomb Settings")]
    public float interactRange = 3f;
    private bool isDefused = false;
    private bool isExploded = false;

    [Header("References")]
    public Transform player;

    [Header("Audio Settings (3 Phases)")]
    public AudioSource bombAudioSource;
    public AudioClip phase1Clip;
    public AudioClip phase2Clip;
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

    [Header("Visual Settings (Emission)")]
    public MeshRenderer[] bombRenderers;
    [ColorUsage(true, true)]
    public Color glowColor = Color.red;
    private Material[] bombMaterials;

    [Header("Mission")]
    public bool completeMissionAfterDefuse = false;
    public int requiredPreviousMission = 2;
    public int missionNumber = 3;
    public string missionCompleteText = "3.Bomb defused";

    private float bombCountdown = 40f;
    private bool switchedToPhase2 = false;
    private float defuseProgress = 0f;

    void Start()
    {
        if (bombAudioSource != null && phase1Clip != null)
        {
            bombAudioSource.clip = phase1Clip;
            bombAudioSource.loop = true;
            bombAudioSource.Play();
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

        bombCountdown -= Time.deltaTime;

        if (bombCountdown <= 10f && !switchedToPhase2)
        {
            switchedToPhase2 = true;
            PlayNewPhaseSound(phase2Clip, true);
            blinkInterval = 0.25f;
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
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > interactRange)
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

        if (MissionListManager.instance == null)
        {
            return true;
        }

        return MissionListManager.instance.IsMissionCompleted(requiredPreviousMission);
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

        if (completeMissionAfterDefuse && MissionListManager.instance != null)
        {
            MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);
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

        if (bombAudioSource != null)
        {
            bombAudioSource.Stop();

            if (explosionClip != null)
            {
                bombAudioSource.PlayOneShot(explosionClip);
            }
        }

        TurnOffVisuals();

        if (redLight != null)
        {
            redLight.enabled = false;
        }

        if (activatedText != null)
        {
            activatedText.SetActive(false);
        }

        if (failedText != null)
        {
            failedText.SetActive(true);
        }

        Debug.Log("Bomb exploded!");
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