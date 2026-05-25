using UnityEngine;
using UnityEngine.UI;

public class PauseMenuOptions : MonoBehaviour
{
    [Header("UI References")]
    public Text SelectedItemText;
    public Text SelectedItemInfoText;
    public GameObject OptionsContainer;

    [Header("Menus")]
    public GameObject PauseMenu;
    public GameObject DeathMenu;

    public void Init()
    {
        RefreshTextsFromActiveMenu();
        SetInfo("Resume");
    }

    public void InitDeathMenu()
    {
        RefreshTextsFromActiveMenu();
        SetInfo("Restart");
    }

    public void OnHoverTextChange(string name)
    {
        RefreshTextsFromActiveMenu();
        SetInfo(name);
    }

    public void OnHoverText(string name)
    {
        RefreshTextsFromActiveMenu();
        SetInfo(name);
    }

    private void RefreshTextsFromActiveMenu()
    {
        GameObject activeMenu = null;

        if (DeathMenu != null && DeathMenu.activeInHierarchy)
        {
            activeMenu = DeathMenu;
        }
        else if (PauseMenu != null && PauseMenu.activeInHierarchy)
        {
            activeMenu = PauseMenu;
        }

        if (activeMenu == null)
        {
            return;
        }

        Text[] texts = activeMenu.GetComponentsInChildren<Text>(true);

        foreach (Text text in texts)
        {
            if (text.gameObject.name == "SelectedItemText")
            {
                SelectedItemText = text;
            }

            if (text.gameObject.name == "SelectedItemInfoText")
            {
                SelectedItemInfoText = text;
            }
        }
    }

    private void SetInfo(string name)
    {
        string normalizedName = NormalizeName(name);

        if (SelectedItemText != null)
        {
            SelectedItemText.text = normalizedName;
        }

        if (OptionsContainer != null)
        {
            OptionsContainer.SetActive(false);
        }

        if (SelectedItemInfoText == null)
        {
            return;
        }

        switch (normalizedName)
        {
            case "Resume":
                SelectedItemInfoText.text = "Resumes the Game.";
                break;

            case "Options":
                SelectedItemInfoText.text = "Change graphics Options.";

                if (OptionsContainer != null)
                {
                    OptionsContainer.SetActive(true);
                }

                break;

            case "Main Menu":
                SelectedItemInfoText.text = "Go back to Main Menu.";
                break;

            case "Load Game":
                SelectedItemInfoText.text = "Load previously Saved Game.";
                break;

            case "Restart":
                SelectedItemInfoText.text = "Restart the current level.";
                break;

            case "Quit":
                SelectedItemInfoText.text = "Quit the game.";
                break;

            default:
                SelectedItemInfoText.text = "";
                break;
        }
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "";
        }

        string lowerName = name.ToLower();

        if (lowerName == "resume")
        {
            return "Resume";
        }

        if (lowerName == "options")
        {
            return "Options";
        }

        if (lowerName == "main menu" || lowerName == "mainmenu")
        {
            return "Main Menu";
        }

        if (lowerName == "load game" || lowerName == "loadgame")
        {
            return "Load Game";
        }

        if (lowerName == "restart")
        {
            return "Restart";
        }

        if (lowerName == "quit")
        {
            return "Quit";
        }

        return name;
    }
}