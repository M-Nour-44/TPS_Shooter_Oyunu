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

    [Header("Death Menu")]
    public GameObject deathMenu;

    private Fader fader;

    [HideInInspector]
    public bool isOpen = false;

    private bool isDeathMenuOpen = false;
    private bool isGoingToMainMenu = false;

    [Header("Pause Game and Resume Game Events")]
    public UnityEngine.Events.UnityEvent onPause = new UnityEngine.Events.UnityEvent();
    public UnityEngine.Events.UnityEvent onUnpause = new UnityEngine.Events.UnityEvent();

    [HideInInspector]
    public List<LoadSlotIdentifier> loadSlots;

    [HideInInspector]
    public bool usingUFPS = false;

    [HideInInspector]
    public bool openPMenu = true;

    private void Awake()
    {
        if (saveMenu != null)
        {
            saveMenu.SetActive(false);
        }

        if (pauseMenu != null)
        {
            ForceHideMenu(pauseMenu);
        }

        if (deathMenu != null)
        {
            ForceHideMenu(deathMenu);
        }

        HideCursorForGameplay();

        isOpen = false;
        isDeathMenuOpen = false;
        isGoingToMainMenu = false;
        openPMenu = true;
    }

    private IEnumerator Start()
    {
        fader = FindObjectOfType<Fader>();

        HideCursorForGameplay();

        yield return null;

        HideCursorForGameplay();

        yield return new WaitForSeconds(0.5f);
    }

    private void Update()
    {
        if (isDeathMenuOpen || isGoingToMainMenu)
        {
            return;
        }

        if (usingUFPS)
        {
            return;
        }

        bool saveMenuClosed = saveMenu == null || !saveMenu.activeSelf;

        if (saveMenuClosed && openPMenu)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isOpen)
                {
                    openPauseMenu();
                }
                else
                {
                    closePauseMenu();
                }
            }
        }

        if (isOpen && forceCursorOnWhilePause)
        {
            ShowCursorForMenu();
        }
    }

    private void LateUpdate()
    {
        if (isDeathMenuOpen || isGoingToMainMenu)
        {
            ShowCursorForMenu();
            return;
        }

        if (isOpen)
        {
            ShowCursorForMenu();

            if (pauseMenu != null && !pauseMenu.activeSelf)
            {
                ForceShowMenu(pauseMenu, 999);
            }

            return;
        }

        bool saveMenuClosed = saveMenu == null || !saveMenu.activeSelf;

        if (saveMenuClosed)
        {
            HideCursorForGameplay();
        }
    }

    public void openPauseMenu()
    {
        if (isDeathMenuOpen)
        {
            return;
        }

        isOpen = true;
        openPMenu = true;

        if (saveMenu != null)
        {
            saveMenu.SetActive(false);
        }

        if (deathMenu != null)
        {
            ForceHideMenu(deathMenu);
        }

        if (pauseMenu != null)
        {
            ForceShowMenu(pauseMenu, 999);
        }

        ShowCursorForMenu();

        if (!usingUFPS)
        {
            Time.timeScale = 0.0001f;
        }

        PauseMenuOptions options = GetComponent<PauseMenuOptions>();

        if (options != null)
        {
            options.Init();
        }

        if (useBlur)
        {
            if (Camera.main != null && Camera.main.GetComponent<Animation>() != null)
            {
                Camera.main.GetComponent<Animation>().Play("BlurOff");
            }
        }

        onPause.Invoke();
    }

    public void closePauseMenu()
    {
        if (isDeathMenuOpen)
        {
            return;
        }

        if (!usingUFPS)
        {
            Time.timeScale = 1f;
        }

        onUnpause.Invoke();

        if (pauseMenu != null)
        {
            ForceHideMenu(pauseMenu);
        }

        isOpen = false;
        openPMenu = true;

        HideCursorForGameplay();

        if (useBlur)
        {
            if (Camera.main != null && Camera.main.GetComponent<Animation>() != null)
            {
                Camera.main.GetComponent<Animation>().Play("BlurOff");
            }
        }
    }

    public void openDeathMenu()
    {
        isDeathMenuOpen = true;
        isGoingToMainMenu = false;

        isOpen = false;
        openPMenu = false;

        if (saveMenu != null)
        {
            saveMenu.SetActive(false);
        }

        if (pauseMenu != null)
        {
            ForceHideMenu(pauseMenu);
        }

        if (deathMenu != null)
        {
            ForceShowMenu(deathMenu, 1000);
        }

        // if (!usingUFPS)
        // {
        //     Time.timeScale = 0.0001f;
        // }

        ShowCursorForMenu();

        PauseMenuOptions options = GetComponent<PauseMenuOptions>();

        if (options != null)
        {
            options.InitDeathMenu();
        }

        onPause.Invoke();
    }

    public void restartLevel()
    {
        Time.timeScale = 1f;

        isDeathMenuOpen = false;
        isGoingToMainMenu = false;

        isOpen = false;
        openPMenu = true;

        onUnpause.Invoke();

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void goToMainMenu()
    {
        isGoingToMainMenu = true;

        Time.timeScale = 1f;

        isDeathMenuOpen = false;
        isOpen = false;
        openPMenu = false;

        if (pauseMenu != null)
        {
            ForceHideMenu(pauseMenu);
        }

        if (deathMenu != null)
        {
            ForceHideMenu(deathMenu);
        }

        if (saveMenu != null)
        {
            saveMenu.SetActive(false);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Destroy(player);
        }

        PlayerScript playerScript = FindObjectOfType<PlayerScript>();

        if (playerScript != null)
        {
            Destroy(playerScript.gameObject);
        }

#if !EMM_ES2
        PlayerPrefs.SetString("sceneToLoad", "");
#else
        PlayerPrefs.SetString("sceneToLoad", "");
        ES2.Save("", "sceneToLoad");
#endif

        ShowCursorForMenu();

        if (fader != null)
        {
            fader.FadeIntoLevel("LoadingScreen");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void quitGame()
    {
        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void hideMenus()
    {
        if (saveMenu != null)
        {
            saveMenu.SetActive(false);
        }

        if (pauseMenu != null)
        {
            ForceHideMenu(pauseMenu);
        }

        if (deathMenu != null)
        {
            ForceHideMenu(deathMenu);
        }

        HideCursorForGameplay();
    }

    public void openLoadGame()
    {
        initLoadGameMenu();
        ShowCursorForMenu();
    }

    public void closeLoadGame()
    {
    }

    private void initLoadGameMenu()
    {
        if (loadSlots != null && loadSlots.Count > 0)
        {
            foreach (LoadSlotIdentifier lsi in loadSlots)
            {
                if (lsi != null)
                {
                    lsi.Init();
                }
            }
        }
    }

    public bool canOpen()
    {
        return openPMenu;
    }

    private void ForceShowMenu(GameObject menu, int order)
    {
        menu.SetActive(true);
        menu.transform.SetAsLastSibling();

        CanvasGroup canvasGroup = menu.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Canvas canvas = menu.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }

        GraphicRaycasterFix(menu);
    }

    private void ForceHideMenu(GameObject menu)
    {
        CanvasGroup canvasGroup = menu.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        menu.SetActive(false);
    }

    private void GraphicRaycasterFix(GameObject menu)
    {
        UnityEngine.UI.GraphicRaycaster raycaster = menu.GetComponent<UnityEngine.UI.GraphicRaycaster>();

        if (raycaster != null)
        {
            raycaster.enabled = true;
        }
    }

    private void HideCursorForGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ShowCursorForMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}