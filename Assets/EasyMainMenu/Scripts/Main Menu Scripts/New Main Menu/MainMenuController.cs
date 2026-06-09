using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EMM
{
    public class MainMenuController : MonoBehaviour
    {
        public Animator MenuButtonsAnimator;
        public string newGameSceneName;
        public int quickSaveSlotID;
        public bool UseLevelSelectMenu;

        [Header("Auto Open Level Select")]
        public string openLevelSelectKey = "OpenLevelSelect";
        public float autoOpenDelay = 0.3f;
        public float startToLevelSelectDelay = 0.45f;

        [Header("Anims")]
        public string MainMenuToStartMenu;
        public string StartMenuToMainMenu;

        [Space]
        public string StartMenuToLevelSelectMenu;
        public string LevelSelectMenuToStartMenu;

        [Space]
        public string StartMenuToLoadSlotMenu;
        public string LoadSlotMenuToStartMenu;

        [Space]
        public string MainMenuToCharMenu;
        public string CharMenuToMainMenu;

        [Space]
        public string MainMenuToOptionsMenu;
        public string OptionsMenuToMainMenu;

        [Space]
        public string OptionsMenuToGameplayMenu;
        public string GameplayMenuToOptionsMenu;

        [Space]
        public string OptionsMenuToGraphicsMenu;
        public string GraphicsMenuToOptionsMenu;

        [Header("SFX")]
        public string ButtonClickSFX;
        public string MainMenuSFX;

        UnityEngine.EventSystems.EventSystem eventSystem;

        void Start()
        {
            if (EasyAudioUtility.instance == null)
            {
                Instantiate(Resources.Load("Prefabs/EasyAudioUtility"));
            }

            PlayerPrefs.SetInt("quickSaveSlot", quickSaveSlotID);

            if (EasyAudioUtility.instance != null)
            {
                EasyAudioUtility.instance.Play(MainMenuSFX);
            }

            eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();

            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (PlayerPrefs.GetInt(openLevelSelectKey, 0) == 1)
            {
                PlayerPrefs.SetInt(openLevelSelectKey, 0);
                OpenLevelSelectInstantly();
            }
        }

        void OpenLevelSelectInstantly()
        {
            if (MenuButtonsAnimator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(MainMenuToStartMenu))
            {
                MenuButtonsAnimator.Play(MainMenuToStartMenu, 0, 1f);
                MenuButtonsAnimator.Update(0f);
            }

            if (!string.IsNullOrEmpty(StartMenuToLevelSelectMenu))
            {
                MenuButtonsAnimator.Play(StartMenuToLevelSelectMenu, 0, 1f);
                MenuButtonsAnimator.Update(0f);
            }
        }

        public void FromMainMenuToStartMenu()
        {
            MenuButtonsAnimator.Play(MainMenuToStartMenu);
            PlayClickSound();
        }

        public void FromStartMenuToMainMenu()
        {
            MenuButtonsAnimator.Play(StartMenuToMainMenu);
            PlayClickSound();
        }

        public void FromStartMenuToNewGame()
        {
            if (UseLevelSelectMenu)
            {
                MenuButtonsAnimator.Play(StartMenuToLevelSelectMenu);
            }
            else
            {
                PlayerPrefs.SetString("sceneToLoad", newGameSceneName);

                Fader fader = FindObjectOfType<Fader>();

                if (fader != null)
                {
                    fader.FadeIntoLevel("LoadingScreen");
                }
            }

            PlayClickSound();
        }

        public void FromLevelSelectMenuToStartMenu()
        {
            MenuButtonsAnimator.Play(LevelSelectMenuToStartMenu);
            PlayClickSound();
        }

        public void FromStartMenuToLoadSlotMenu()
        {
            MenuButtonsAnimator.Play(StartMenuToLoadSlotMenu);
            PlayClickSound();
        }

        public void FromLoadSlotMenuToStartMenu()
        {
            MenuButtonsAnimator.Play(LoadSlotMenuToStartMenu);
            PlayClickSound();
        }

        public void FromMainMenuToCharMenu()
        {
            MenuButtonsAnimator.Play(MainMenuToCharMenu);
            PlayClickSound();
        }

        public void FromCharMenuToMainMenu()
        {
            MenuButtonsAnimator.Play(CharMenuToMainMenu);

            if (FindObjectOfType<CharacterSelectMenuController>())
            {
                FindObjectOfType<CharacterSelectMenuController>().GetCharacter();
            }

            PlayClickSound();
        }

        public void FromMainMenuToOptionsMenu()
        {
            MenuButtonsAnimator.Play(MainMenuToOptionsMenu);
            PlayClickSound();
        }

        public void FromOptionsMenuToMainMenu()
        {
            MenuButtonsAnimator.Play(OptionsMenuToMainMenu);
            PlayClickSound();
        }

        public void FromOptionsMenuToGameplayMenu()
        {
            MenuButtonsAnimator.Play(OptionsMenuToGameplayMenu);
            PlayClickSound();
        }

        public void FromGameplayMenuToOptionsMenu()
        {
            MenuButtonsAnimator.Play(GameplayMenuToOptionsMenu);
            PlayClickSound();
        }

        public void FromOptionsMenuToGraphicsMenu()
        {
            MenuButtonsAnimator.Play(OptionsMenuToGraphicsMenu);
            PlayClickSound();
        }

        public void FromGraphicsMenuToOptionsMenu()
        {
            MenuButtonsAnimator.Play(GraphicsMenuToOptionsMenu);
            PlayClickSound();
        }

        public void ChangeSelectedGameobject(GameObject Obj)
        {
            StartCoroutine(SelectButtonInUI(Obj));
        }

        IEnumerator SelectButtonInUI(GameObject Btn)
        {
            yield return new WaitForSeconds(0.25f);

            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(Btn);
            }

            if (Btn != null && Btn.GetComponent<UnityEngine.UI.Button>())
            {
                Btn.GetComponent<UnityEngine.UI.Button>().Select();
            }
        }

        public void QuitGame()
        {
            PlayClickSound();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void PlayClickSound()
        {
            if (EasyAudioUtility.instance)
            {
                EasyAudioUtility.instance.Play(ButtonClickSFX);
            }
        }
    }
}