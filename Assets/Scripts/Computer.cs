using System.Collections;
using UnityEngine;

public class Computer : MonoBehaviour
{
    [Header("Computer On/Off")]
    public bool lightsOn = true;
    [SerializeField] private float radius = 2.5f;
    public Light lights;

    [Header("Computer Sound")]
    public AudioSource objectAudioSource;
    public AudioClip pressSound;

    [Header("Computer Assign Things")]
    public PlayerScript player;
    [SerializeField] private GameObject ComputerUI;
    [SerializeField] private int showComputerUIFor = 5;

    [Header("Mission")]
    public int requiredPreviousMission = 1;
    public int missionNumber = 2;
    public string missionCompleteText = "2.Computer Disabled";

    private bool completed = false;

    private void Awake()
    {
        if (lights == null)
        {
            lights = GetComponent<Light>();
        }

        if (objectAudioSource == null)
        {
            objectAudioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (completed)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && Vector3.Distance(transform.position, player.transform.position) < radius)
        {
            TryDisableComputer();
        }
    }

    private void TryDisableComputer()
    {
        if (MissionListManager.instance == null)
        {
            return;
        }

        if (!MissionListManager.instance.IsMissionCompleted(requiredPreviousMission))
        {
            return;
        }

        bool missionDone = MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);

        if (!missionDone)
        {
            return;
        }

        completed = true;
        lightsOn = false;

        if (lights != null)
        {
            lights.intensity = 0;
        }

        if (ComputerUI != null)
        {
            StartCoroutine(ShowComputerUI());
        }

        PlaySound();
    }

    public void PlaySound()
    {
        if (objectAudioSource != null && pressSound != null)
        {
            objectAudioSource.PlayOneShot(pressSound);
        }
    }

    private IEnumerator ShowComputerUI()
    {
        ComputerUI.SetActive(true);
        yield return new WaitForSeconds(showComputerUIFor);
        ComputerUI.SetActive(false);
    }
}