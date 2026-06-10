using UnityEngine;

namespace EMM
{
    public static class LevelProgressManager
    {
        public const string TutorialCompletedKey = "TutorialCompleted";
        public const string Level1CompletedKey = "Level1Completed";
        public const string Level2CompletedKey = "Level2Completed";

        public static void CompleteTutorial()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void CompleteLevel1()
        {
            PlayerPrefs.SetInt(Level1CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void CompleteLevel2()
        {
            PlayerPrefs.SetInt(Level2CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static bool IsLevelUnlocked(string sceneName)
        {
            if (sceneName == "Tutorial")
            {
                return true;
            }

            if (sceneName == "Level 1")
            {
                return PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
            }

            if (sceneName == "Level 2")
            {
                return PlayerPrefs.GetInt(Level1CompletedKey, 0) == 1;
            }

            return false;
        }

        public static string GetLockedMessage(string sceneName)
        {
            if (sceneName == "Level 1")
            {
                return "Complete Tutorial first";
            }

            if (sceneName == "Level 2")
            {
                return "Complete Level 1 first";
            }

            return "This level is locked";
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(TutorialCompletedKey);
            PlayerPrefs.DeleteKey(Level1CompletedKey);
            PlayerPrefs.DeleteKey(Level2CompletedKey);
            PlayerPrefs.Save();
        }
    }
}