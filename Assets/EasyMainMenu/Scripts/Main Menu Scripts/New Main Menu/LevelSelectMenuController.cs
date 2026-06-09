using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EMM
{
    public class LevelSelectMenuController : MonoBehaviour
    {
        [Header("UI References")]
        public Text LevelTitleText;
        public Text LevelDescriptionText;
        public Image LevelImage;
        public Button playButton;

        [Header("Locked Image Settings")]
        public Color unlockedImageColor = Color.white;
        public Color lockedImageColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
        public bool disablePlayButtonWhenLocked = true;

        [Header("Locked Description")]
        public string lockedDescriptionText = "LOCKED";

        [HideInInspector]
        public List<AllLevelsData> AllLevelsData = new List<AllLevelsData>();

        int _totalLevels;
        int _currentSelectedLevelCount;
        string _currentSelectedLevelSceneName;
        bool _currentSelectedLevelLocked;
        AllLevelsData _currentLevelData;

        void Start()
        {
            _totalLevels = AllLevelsData.Count;

            if (_totalLevels <= 0)
            {
                return;
            }

            ChangeLevel();
        }

        public void ChangeLevel()
        {
            if (AllLevelsData == null || AllLevelsData.Count == 0)
            {
                return;
            }

            _currentLevelData = AllLevelsData[_currentSelectedLevelCount];
            _currentSelectedLevelSceneName = _currentLevelData.SceneToLoad;

            RefreshLockState();

            if (LevelTitleText != null)
            {
                LevelTitleText.text = _currentLevelData.LevelTitle;
            }

            if (LevelDescriptionText != null)
            {
                LevelDescriptionText.text = _currentSelectedLevelLocked ? lockedDescriptionText : _currentLevelData.LevelDescription;
            }

            if (LevelImage != null)
            {
                LevelImage.sprite = _currentLevelData.LevelSprite;
            }

            if (_currentSelectedLevelCount < _totalLevels - 1)
            {
                _currentSelectedLevelCount++;
            }
            else
            {
                _currentSelectedLevelCount = 0;
            }

            PlayClickSound();
        }

        private void RefreshLockState()
        {
            _currentSelectedLevelLocked = !LevelProgressManager.IsLevelUnlocked(_currentSelectedLevelSceneName);

            if (LevelImage != null)
            {
                LevelImage.color = _currentSelectedLevelLocked ? lockedImageColor : unlockedImageColor;
            }

            if (playButton != null && disablePlayButtonWhenLocked)
            {
                playButton.interactable = !_currentSelectedLevelLocked;
            }
        }

        void PlayClickSound()
        {
            if (EasyAudioUtility.instance)
            {
                MainMenuController mainMenuController = FindObjectOfType<MainMenuController>();

                if (mainMenuController != null)
                {
                    EasyAudioUtility.instance.Play(mainMenuController.ButtonClickSFX);
                }
            }
        }

        public void PlayLevel()
        {
            RefreshLockState();

            if (_currentSelectedLevelLocked)
            {
                PlayClickSound();
                return;
            }

            PlayerPrefs.SetString("sceneToLoad", _currentSelectedLevelSceneName);
            PlayerPrefs.SetInt("slotLoaded_", -1);

            Fader fader = FindObjectOfType<Fader>();

            if (fader != null)
            {
                fader.FadeIntoLevel("LoadingScreen");
            }
        }
    }

    [System.Serializable]
    public class AllLevelsData
    {
        public string LevelTitle;
        public string LevelDescription;
        public string SceneToLoad;
        public Sprite LevelSprite;
    }
}