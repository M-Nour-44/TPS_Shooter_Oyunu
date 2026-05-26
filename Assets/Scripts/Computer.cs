using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : MonoBehaviour
{
    [Header("Computer On/Off")]
    public bool lightsOn = true;
    private float radius = 2.5f; 
    public Light lights; 

    [Header("Computer Sound")]
    public AudioSource objectAudioSource;
    public AudioClip pressSound;

    public void PlaySound()
    {
        if (objectAudioSource != null && pressSound != null)
        {
            objectAudioSource.PlayOneShot(pressSound);
        }
    }

    [Header("Computer Assign Things")]
    public PlayerScript player;
    [SerializeField] private GameObject ComputerUI;
    [SerializeField] private int showComputerUIFor = 5;
    
    private void Awake()
    {
        lights = GetComponent<Light>();
    }

    private void Update()
    {
        if(Vector3.Distance(transform.position, player.transform.position) < radius)
        {
            if(Input.GetKeyDown("e"))
            {
                StartCoroutine(ShowComputerUI());
                lightsOn = false;
                lights.intensity = 0;
                //objective completed
                //sound effect
                MissionListManager.instance.CompleteMission(2, "2.Computer Disabled");
                PlaySound();
            }
        }
    }

    IEnumerator ShowComputerUI()
    {
        ComputerUI.SetActive(true);
        yield return new WaitForSeconds(showComputerUIFor);
        ComputerUI.SetActive(false);
    }
}