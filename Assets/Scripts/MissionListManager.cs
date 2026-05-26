using UnityEngine;
using TMPro;

public class MissionListManager : MonoBehaviour
{
    public static MissionListManager instance;

    [Header("UI Elements")]
    public GameObject taskListPanel; 
    
    public TextMeshProUGUI[] missionTexts; 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTaskList();
        }
    }

    void ToggleTaskList()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(!taskListPanel.activeSelf);
        }
    }

    public void CompleteMission(int missionNumber, string newText)
    {
        int index = missionNumber - 1;

        if (index >= 0 && index < missionTexts.Length)
        {
            if (missionTexts[index] != null)
            {
                missionTexts[index].color = Color.green; 
                missionTexts[index].text = newText;
            }
        }
    }
}