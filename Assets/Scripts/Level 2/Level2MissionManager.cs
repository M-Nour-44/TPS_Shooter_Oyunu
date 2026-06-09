using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.AI;

public class Level2MissionManager : MonoBehaviour
{
    public static Level2MissionManager instance;

    [Header("UI Elements")]
    public GameObject taskListPanel;
    public TextMeshProUGUI[] missionTexts = new TextMeshProUGUI[3];

    [Header("Current Objective UI")]
    public GameObject currentObjectivePanel;
    public TextMeshProUGUI currentObjectiveText;
    public string[] currentObjectiveTexts = new string[3]
    {
        "GO to the marked area",
        "Eliminate the big enemy",
        "Defuse the bomb"
    };

    [Header("Level Complete Audio")]
    public AudioSource levelCompleteAudioSource;
    public AudioClip levelCompleteSound;

    [Header("Start Hint UI")]
    public GameObject startHintUI;
    public CanvasGroup startHintCanvasGroup;
    public float startHintDuration = 12f;
    public float startHintFadeSpeed = 0.6f;

    [Header("Objective Markers")]
    public GameObject reachAreaMarker;
    public GameObject bombMarker;

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteText;
    public string levelCompleteMessage = "LEVEL 2 COMPLETED";
    public float levelCompleteDelay = 5f;

    [Header("Stop Objects On Level Complete")]
    public GameObject[] objectsToStopOnLevelComplete;
    public bool disableObjectsOnLevelComplete = false;

    [Header("After Level Complete")]
    public bool loadSceneAfterComplete = true;
    public string loadingSceneName = "LoadingScreen";
    public string nextSceneName = "MainMenu";

    private bool mission1Completed = false;
    private bool mission2Completed = false;
    private bool mission3Completed = false;
    private bool levelChanging = false;

    private bool currentObjectiveShown = false;
    private Coroutine startHintCoroutine;
    private float[] missionFontSizes;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        SaveMissionTextFontSizes();

        if (reachAreaMarker != null)
        {
            reachAreaMarker.SetActive(true);
        }

        if (bombMarker != null)
        {
            bombMarker.SetActive(false);
        }

        SetupCurrentObjective();
        SetupStartHint();
    }

    private void Update()
    {
        if (levelChanging)
        {
            if (taskListPanel != null)
            {
                taskListPanel.SetActive(false);
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowCurrentObjectiveFirstTime();
        }

        if (taskListPanel != null)
        {
            if (Input.GetKey(KeyCode.T))
            {
                taskListPanel.SetActive(true);
                HideStartHint();
            }
            else
            {
                taskListPanel.SetActive(false);
            }
        }
    }

    private void SaveMissionTextFontSizes()
    {
        if (missionTexts == null)
        {
            return;
        }

        missionFontSizes = new float[missionTexts.Length];

        for (int i = 0; i < missionTexts.Length; i++)
        {
            if (missionTexts[i] != null)
            {
                missionTexts[i].enableAutoSizing = false;
                missionFontSizes[i] = missionTexts[i].fontSize;
            }
        }
    }

    private void SetupCurrentObjective()
    {
        if (currentObjectivePanel != null)
        {
            currentObjectivePanel.SetActive(false);
        }

        SetCurrentObjective(1);
    }

    private void ShowCurrentObjectiveFirstTime()
    {
        if (currentObjectiveShown)
        {
            return;
        }

        currentObjectiveShown = true;

        if (currentObjectivePanel != null)
        {
            currentObjectivePanel.SetActive(true);
        }
    }

    private void SetCurrentObjective(int missionNumber)
    {
        if (currentObjectiveText == null)
        {
            return;
        }

        int index = missionNumber - 1;

        if (currentObjectiveTexts != null &&
            index >= 0 &&
            index < currentObjectiveTexts.Length &&
            !string.IsNullOrEmpty(currentObjectiveTexts[index]))
        {
            currentObjectiveText.text = currentObjectiveTexts[index];
        }
    }

    private void HideCurrentObjective()
    {
        if (currentObjectivePanel != null)
        {
            currentObjectivePanel.SetActive(false);
        }
    }

    private void SetupStartHint()
    {
        if (startHintUI == null)
        {
            return;
        }

        startHintUI.SetActive(true);

        if (startHintCanvasGroup == null)
        {
            startHintCanvasGroup = startHintUI.GetComponent<CanvasGroup>();
        }

        if (startHintCanvasGroup == null)
        {
            startHintCanvasGroup = startHintUI.AddComponent<CanvasGroup>();
        }

        startHintCoroutine = StartCoroutine(StartHintRoutine());
    }

    private IEnumerator StartHintRoutine()
    {
        float timer = 0f;

        if (startHintCanvasGroup != null)
        {
            startHintCanvasGroup.alpha = 0f;
        }

        while (timer < startHintDuration)
        {
            timer += Time.unscaledDeltaTime;

            if (startHintCanvasGroup != null)
            {
                startHintCanvasGroup.alpha = Mathf.PingPong(Time.unscaledTime * startHintFadeSpeed, 1f);
            }

            yield return null;
        }

        HideStartHint();
    }

    private void HideStartHint()
    {
        if (startHintCoroutine != null)
        {
            StopCoroutine(startHintCoroutine);
            startHintCoroutine = null;
        }

        if (startHintCanvasGroup != null)
        {
            startHintCanvasGroup.alpha = 0f;
        }

        if (startHintUI != null)
        {
            startHintUI.SetActive(false);
        }
    }

    public bool IsMissionCompleted(int missionNumber)
    {
        if (missionNumber == 1)
        {
            return mission1Completed;
        }

        if (missionNumber == 2)
        {
            return mission2Completed;
        }

        if (missionNumber == 3)
        {
            return mission3Completed;
        }

        return false;
    }

    public bool CanCompleteMission(int missionNumber)
    {
        if (missionNumber == 1)
        {
            return true;
        }

        if (missionNumber == 2)
        {
            return mission1Completed;
        }

        if (missionNumber == 3)
        {
            return mission1Completed && mission2Completed;
        }

        return false;
    }

    public bool CompleteMission(int missionNumber, string newText)
    {
        if (levelChanging)
        {
            return false;
        }

        if (missionNumber < 1 || missionNumber > 3)
        {
            return false;
        }

        if (!CanCompleteMission(missionNumber))
        {
            return false;
        }

        if (IsMissionCompleted(missionNumber))
        {
            return false;
        }

        int index = missionNumber - 1;

        if (missionTexts != null && index < missionTexts.Length && missionTexts[index] != null)
        {
            missionTexts[index].enableAutoSizing = false;
            missionTexts[index].color = Color.green;
            missionTexts[index].text = newText;

            if (missionFontSizes != null && index < missionFontSizes.Length)
            {
                missionTexts[index].fontSize = missionFontSizes[index];
            }
        }

        if (missionNumber == 1)
        {
            mission1Completed = true;

            if (reachAreaMarker != null)
            {
                reachAreaMarker.SetActive(false);
            }

            if (bombMarker != null)
            {
                bombMarker.SetActive(false);
            }

            SetCurrentObjective(2);
        }
        else if (missionNumber == 2)
        {
            mission2Completed = true;

            if (reachAreaMarker != null)
            {
                reachAreaMarker.SetActive(false);
            }

            if (bombMarker != null)
            {
                bombMarker.SetActive(true);
            }

            SetCurrentObjective(3);
        }
        else if (missionNumber == 3)
        {
            mission3Completed = true;

            if (reachAreaMarker != null)
            {
                reachAreaMarker.SetActive(false);
            }

            if (bombMarker != null)
            {
                bombMarker.SetActive(false);
            }

            HideCurrentObjective();
        }

        CheckAllMissionsCompleted();
        return true;
    }

    private void CheckAllMissionsCompleted()
    {
        if (mission1Completed && mission2Completed && mission3Completed)
        {
            if (!levelChanging)
            {
                StartCoroutine(LevelCompleteRoutine());
            }
        }
    }

    private void StopLevelCompleteObjects()
    {
        if (objectsToStopOnLevelComplete == null)
        {
            return;
        }

        foreach (GameObject obj in objectsToStopOnLevelComplete)
        {
            if (obj == null)
            {
                continue;
            }

            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }

                agent.enabled = false;
            }

            Animator animator = obj.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("Shoot", false);
                animator.SetBool("IsAiming", false);
            }

            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem particle in particles)
            {
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>();

            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }

            MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour script in scripts)
            {
                if (script == null)
                {
                    continue;
                }

                if (script == this)
                {
                    continue;
                }

                script.enabled = false;
            }

            if (disableObjectsOnLevelComplete)
            {
                obj.SetActive(false);
            }
        }
    }

    private IEnumerator LevelCompleteRoutine()
    {
        levelChanging = true;

        StopLevelCompleteObjects();

        HideStartHint();
        HideCurrentObjective();

        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        if (levelCompleteAudioSource != null && levelCompleteSound != null)
        {
            levelCompleteAudioSource.PlayOneShot(levelCompleteSound);
        }

        if (levelCompleteText != null)
        {
            levelCompleteText.text = levelCompleteMessage;
        }

        yield return new WaitForSecondsRealtime(levelCompleteDelay);

        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (loadSceneAfterComplete)
        {
            PlayerPrefs.SetString("sceneToLoad", nextSceneName);
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}