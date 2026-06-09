using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;

    [Header("Complete UI")]
    public GameObject tutorialCompletePanel;
    public TextMeshProUGUI tutorialCompleteText;
    public string tutorialCompleteMessage = "Tutorial Complete";

    [Header("Scene Settings")]
    public string NextSceneName = "Level 1";
    public string loadingScreenSceneName = "LoadingScreen";
    public bool useLoadingScreen = true;

    [Header("Tutorial Settings")]
    public float stepCompleteDelay = 0.6f;
    public bool lockCursorOnStart = true;

    [Header("Training Enemies")]
    public Enemy[] trainingEnemies;

    private int currentStep = 0;
    private float nextStepTime = 0f;
    private bool tutorialFinished = false;

    private string[] tutorialSteps =
    {
        "Press W to move forward",
        "Press S to move backward",
        "Press D to move right",
        "Press A to move left",
        "Hold SHIFT And W to sprint",
        "Press C to crouch",
        "Hold RIGHT MOUSE to aim",
        "Press LEFT MOUSE to shoot",
        "Press R to reload",
        "Press F to command your ally",
        "Press G to regroup your ally",
        "Press M to open the map",
        "Press M again",
        "Eliminate both training enemies"
    };

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(false);
        }

        if (lockCursorOnStart)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        ShowCurrentStep();
    }

    private void Update()
    {
        if (tutorialFinished)
        {
            return;
        }

        if (Time.time < nextStepTime)
        {
            return;
        }

        CheckCurrentStep();
    }

    private void CheckCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                if (Input.GetKey(KeyCode.W))
                {
                    CompleteStep();
                }
                break;

            case 1:
                if (Input.GetKey(KeyCode.S))
                {
                    CompleteStep();
                }
                break;

            case 2:
                if (Input.GetKey(KeyCode.D))
                {
                    CompleteStep();
                }
                break;

            case 3:
                if (Input.GetKey(KeyCode.A))
                {
                    CompleteStep();
                }
                break;

            case 4:
                if ((Input.GetButton("Sprint") || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) & Input.GetKey(KeyCode.W))
                {
                    CompleteStep();
                }
                break;

            case 5:
                if (Input.GetKeyDown(KeyCode.C))
                {
                    CompleteStep();
                }
                break;

            case 6:
                if (Input.GetButton("Fire2"))
                {
                    CompleteStep();
                }
                break;

            case 7:
                if (Input.GetButtonDown("Fire1"))
                {
                    CompleteStep();
                }
                break;

            case 8:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    CompleteStep();
                }
                break;

            case 9:
                if (Input.GetKeyDown(KeyCode.F))
                {
                    CompleteStep();
                }
                break;

            case 10:
                if (Input.GetKeyDown(KeyCode.G))
                {
                    CompleteStep();
                }
                break;

            case 11:
                if (Input.GetKeyDown(KeyCode.M))
                {
                    CompleteStep();
                }
                break;

            case 12:
                if (Input.GetKeyDown(KeyCode.M))
                {
                    CompleteStep();
                }
                break;

            case 13:
                UpdateEnemyObjectiveText();

                if (AreAllTrainingEnemiesDead())
                {
                    FinishTutorial();
                }
                break;
        }
    }

    private void CompleteStep()
    {
        currentStep++;
        nextStepTime = Time.time + stepCompleteDelay;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialText == null)
        {
            return;
        }

        if (currentStep >= tutorialSteps.Length)
        {
            tutorialText.text = "Tutorial:\n" + tutorialCompleteMessage;
            return;
        }

        tutorialText.text = "Tutorial:\n" + tutorialSteps[currentStep];
    }

    private void UpdateEnemyObjectiveText()
    {
        if (tutorialText == null)
        {
            return;
        }

        int aliveEnemies = CountAliveTrainingEnemies();
        int totalEnemies = trainingEnemies != null ? trainingEnemies.Length : 0;
        int killedEnemies = totalEnemies - aliveEnemies;

        tutorialText.text = "Tutorial:\nEliminate training enemies\n" + killedEnemies + " / " + totalEnemies;
    }

    private bool AreAllTrainingEnemiesDead()
    {
        if (trainingEnemies == null || trainingEnemies.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < trainingEnemies.Length; i++)
        {
            if (trainingEnemies[i] != null && !trainingEnemies[i].IsEnemyDead())
            {
                return false;
            }
        }

        return true;
    }

    private int CountAliveTrainingEnemies()
    {
        int count = 0;

        if (trainingEnemies == null)
        {
            return count;
        }

        for (int i = 0; i < trainingEnemies.Length; i++)
        {
            if (trainingEnemies[i] != null && !trainingEnemies[i].IsEnemyDead())
            {
                count++;
            }
        }

        return count;
    }

    public void FinishTutorial()
    {
        if (tutorialFinished)
        {
            return;
        }

        tutorialFinished = true;

        EMM.LevelProgressManager.CompleteTutorial();

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(true);
        }

        if (tutorialCompleteText != null)
        {
            tutorialCompleteText.text = tutorialCompleteMessage;
        }

        Invoke(nameof(LoadLevelOne), 2f);
    }

    private void LoadLevelOne()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        PlayerPrefs.SetInt("OpenLevelSelect", 1);

        if (useLoadingScreen)
        {
            PlayerPrefs.SetString("sceneToLoad", "MainMenu");
            SceneManager.LoadScene(loadingScreenSceneName);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        
        }
    }

    public int GetCurrentStep()
    {
        return currentStep;
    }

    public bool IsTutorialFinished()
    {
        return tutorialFinished;
    }
}