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

    [Header("Generator Stop Target")]
    public GameObject fanRoot;
    public bool disableFanRootObject = false;

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

        StopThisGenerator();

        if (pressedButtonsCount >= requiredButtons && !missionCompleted)
        {
            bool completed = MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);

            if (!completed)
            {
                return;
            }

            missionCompleted = true;
        }
    }

    private void StopThisGenerator()
    {
        StopAnimator(animation);

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (fanRoot != null)
        {
            Animator[] animators = fanRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                StopAnimator(animators[i]);
            }

            Animation[] animations = fanRoot.GetComponentsInChildren<Animation>(true);

            for (int i = 0; i < animations.Length; i++)
            {
                if (animations[i] != null)
                {
                    animations[i].Stop();
                    animations[i].enabled = false;
                }
            }

            AudioSource[] audioSources = fanRoot.GetComponentsInChildren<AudioSource>(true);

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                {
                    audioSources[i].Stop();
                }
            }

            ParticleSystem[] particles = fanRoot.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                {
                    particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (disableFanRootObject)
            {
                fanRoot.SetActive(false);
            }
        }
    }

    private void StopAnimator(Animator targetAnimator)
    {
        if (targetAnimator == null)
        {
            return;
        }

        targetAnimator.speed = 0f;
        targetAnimator.enabled = false;
    }
}