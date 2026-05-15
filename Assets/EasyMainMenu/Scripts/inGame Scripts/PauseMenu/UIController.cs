using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Tooltip("Use Blur in Pause Menu?")]
    public bool useBlur;

    public bool forceCursorOnWhilePause;

    [Header("Both UI Panels")]
    public GameObject saveMenu;
    public GameObject pauseMenu;

    Fader fader;

    [HideInInspector]
    public bool isOpen;

    Canvas[] allUI;

    [Header("Pause Game and Resume Game Events")]
    public UnityEngine.Events.UnityEvent onPause = new UnityEngine.Events.UnityEvent();
    public UnityEngine.Events.UnityEvent onUnpause = new UnityEngine.Events.UnityEvent();

    [HideInInspector]
    public List<LoadSlotIdentifier> loadSlots;

    [HideInInspector]
    public bool usingUFPS = false;

    void Awake()
    {
        HideCursorForGameplay();
    }

    IEnumerator Start()
    {
        // Find fader
        fader = FindObjectOfType<Fader>();

        // Hide cursor immediately when gameplay starts
        HideCursorForGameplay();

        // Wait one frame, then hide again in case another script shows it
        yield return null;

        HideCursorForGameplay();

        yield return new WaitForSeconds(0.5f);
    }

    void Update()
    {
        // If using UFPS
        if (usingUFPS)
            return;

        // Show cursor only while pause menu or save menu is open
        if (forceCursorOnWhilePause && (isOpen || saveMenu.activeSelf))
        {
            ShowCursorForMenu();
        }

        // If save menu is not opened
        if (!saveMenu.activeSelf && canOpen())
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isOpen)
                    openPauseMenu();
                else
                    closePauseMenu();
            }
        }
    }

    void LateUpdate()
    {
        // While gameplay is active, keep cursor hidden and locked
        if (!isOpen && saveMenu != null && !saveMenu.activeSelf)
        {
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                HideCursorForGameplay();
            }
        }
    }

    public void openPauseMenu()
    {
        allUI = FindObjectsOfType<Canvas>();

        // Disable all UI
        for (int i = 0; i < allUI.Length; i++)
        {
            if (allUI[i].name != "Fader")
                allUI[i].gameObject.SetActive(false);
        }

        saveMenu.SetActive(false);
        pauseMenu.SetActive(true);

        // Show mouse in pause menu if enabled
        if (forceCursorOnWhilePause)
        {
            ShowCursorForMenu();
        }

        // Play sound
        GetComponent<SaveGameUI>().playClickSound();

        // Play animation
        GetComponent<Animator>().Play("OpenPauseMenu");

        // Time almost stopped
        if (!usingUFPS)
            Time.timeScale = 0.0001f;

        isOpen = true;

        // Init pause menu options
        GetComponent<PauseMenuOptions>().Init();

        // Enable blur
        if (useBlur)
        {
            if (Camera.main.GetComponent<Animator>())
                Camera.main.GetComponent<Animator>().Play("BlurOff");
        }

        onPause.Invoke();
    }

    public void closePauseMenu()
    {
        // Enable all UI
        for (int i = 0; i < allUI.Length; i++)
        {
            allUI[i].gameObject.SetActive(true);
        }

        // Time = 1
        if (!usingUFPS)
            Time.timeScale = 1;

        // Play sound
        GetComponent<SaveGameUI>().playClickSound();

        // Play animation
        GetComponent<Animator>().Play("ClosePauseMenu");

        isOpen = false;

        // Hide cursor again when returning to gameplay
        HideCursorForGameplay();

        // Enable blur
        if (useBlur)
        {
            if (Camera.main.GetComponent<Animator>())
                Camera.main.GetComponent<Animator>().Play("BlurOff");
        }

        onUnpause.Invoke();
    }

    public void hideMenus()
    {
        saveMenu.SetActive(false);
        pauseMenu.SetActive(false);

        HideCursorForGameplay();
    }

    public void goToMainMenu()
    {
        // Delete player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            Destroy(player);

        // Restore time scale
        Time.timeScale = 1f;

        // Load main menu
#if !EMM_ES2
        PlayerPrefs.SetString("sceneToLoad", "");
#else
        PlayerPrefs.SetString("sceneToLoad", "");
        ES2.Save("", "sceneToLoad");
#endif

        // Hide all menus
        hideMenus();

        // Show cursor in main menu
        ShowCursorForMenu();

        // Load level via fader
        fader.FadeIntoLevel("LoadingScreen");
    }

    public void openLoadGame()
    {
        GetComponent<Animator>().Play("loadGameOpen");
        initLoadGameMenu();

        if (forceCursorOnWhilePause)
        {
            ShowCursorForMenu();
        }
    }

    public void closeLoadGame()
    {
        GetComponent<Animator>().Play("loadGameClose");
    }

    void initLoadGameMenu()
    {
        if (loadSlots.Count > 0)
        {
            foreach (LoadSlotIdentifier lsi in loadSlots)
            {
                lsi.Init();
            }
        }
    }

    [HideInInspector]
    public bool openPMenu = true;

    public bool canOpen()
    {
        return openPMenu;
    }

    void HideCursorForGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void ShowCursorForMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}