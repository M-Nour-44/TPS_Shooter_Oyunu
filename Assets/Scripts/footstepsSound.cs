using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepsSound : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("FootSteps Sources")]
    [SerializeField] private AudioClip[] footstepsSound;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private AudioClip GetRandomFootStep()
    {
        return footstepsSound[UnityEngine.Random.Range(0, footstepsSound.Length)];
    }

    private void step()
    {
        AudioClip clip = GetRandomFootStep();
        audioSource.PlayOneShot(clip);
    }
}