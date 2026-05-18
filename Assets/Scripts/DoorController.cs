using UnityEngine;

public class DoorController : MonoBehaviour
{

    public Animator animator;

    private bool isOpen = false;

    [Header("Sound Efeect")]
    public AudioClip gateSound;
    public AudioSource audioSource;
 
    public void ToggleDoor()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
        audioSource.PlayOneShot(gateSound);
    }

    
}