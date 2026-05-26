using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MissionListManager : MonoBehaviour
{
    public static MissionListManager instance;

    [Header("UI Elements")]
    public GameObject taskListPanel;
    public TextMeshProUGUI[] missionTexts = new TextMeshProUGUI[3];

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteText;
    public string levelCompleteMessage = "LEVEL 1 COMPLETED";
    public float levelCompleteDelay = 5f;

    [Header("Loading")]
    public string loadingSceneName = "LoadingScreen";
    public string nextLevelSceneName = "Level 2";

    private bool mission1Completed = false;
    private bool mission2Completed = false;
    private bool mission3Completed = false;
    private bool levelChanging = false;

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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTaskList();
        }
    }

    private void ToggleTaskList()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(!taskListPanel.activeSelf);
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
            missionTexts[index].color = Color.green;
            missionTexts[index].text = newText;
        }

        if (missionNumber == 1)
        {
            mission1Completed = true;
        }
        else if (missionNumber == 2)
        {
            mission2Completed = true;
        }
        else if (missionNumber == 3)
        {
            mission3Completed = true;
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

    private IEnumerator LevelCompleteRoutine()
    {
        levelChanging = true;

        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (levelCompleteText != null)
        {
            levelCompleteText.text = levelCompleteMessage;
        }

        yield return new WaitForSecondsRealtime(levelCompleteDelay);

        Time.timeScale = 1f;

#if !EMM_ES2
        PlayerPrefs.SetString("sceneToLoad", nextLevelSceneName);
#else
        PlayerPrefs.SetString("sceneToLoad", nextLevelSceneName);
        ES2.Save(nextLevelSceneName, "sceneToLoad");
#endif

        SceneManager.LoadScene(loadingSceneName);
    }
}