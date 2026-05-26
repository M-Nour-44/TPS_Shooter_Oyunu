using UnityEngine;

public class GeneratorTurnOff : MonoBehaviour
{
    [Header("Generator Lights and Button")]
    public GameObject greenLight;
    public GameObject redLight;
    public bool button;

    [Header("Generator Sound Effects and Radius")]
    [SerializeField] private float radius = 2f;
    public PlayerScript player;
    public Animator animation;
    public AudioSource audioSource;

    [Header("Mission")]
    public int requiredPreviousMission = 2;
    public int requiredButtons = 2;
    public int missionNumber = 3;
    public string missionCompleteText = "3.Generators turned off";

    private bool thisButtonPressed = false;

    private static int pressedButtonsCount = 0;
    private static bool missionCompleted = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetGeneratorMissionState()
    {
        pressedButtonsCount = 0;
        missionCompleted = false;
    }

    private void Awake()
    {
        button = false;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (greenLight != null)
        {
            greenLight.SetActive(true);
        }

        if (redLight != null)
        {
            redLight.SetActive(false);
        }
    }

    private void Update()
    {
        if (thisButtonPressed || missionCompleted)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && Vector3.Distance(transform.position, player.transform.position) < radius)
        {
            TryPressButton();
        }
    }

    private void TryPressButton()
    {
        if (MissionListManager.instance == null)
        {
            return;
        }

        if (!MissionListManager.instance.IsMissionCompleted(requiredPreviousMission))
        {
            return;
        }

        thisButtonPressed = true;
        button = true;
        pressedButtonsCount++;

        if (greenLight != null)
        {
            greenLight.SetActive(false);
        }

        if (redLight != null)
        {
            redLight.SetActive(true);
        }

        if (pressedButtonsCount >= requiredButtons && !missionCompleted)
        {
            bool completed = MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);

            if (!completed)
            {
                return;
            }

            missionCompleted = true;

            if (animation != null)
            {
                animation.enabled = false;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }
}