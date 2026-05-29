using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator animator;
    private bool isOpen = false;

    [Header("Sound Efeect")]
    public AudioClip gateSound;
    public AudioSource audioSource;

    [Header("Mission")]
    public bool isMissionDoor = false;
    public int missionNumber = 1;
    public string missionCompleteText = "1.Door opened";

    private bool missionCompleted = false;

    public void ToggleDoor()
    {
        if (animator == null)
        {
            return;
        }

        isOpen = !isOpen;

        animator.SetBool("IsOpen", isOpen);

        if (audioSource != null && gateSound != null)
        {
            audioSource.PlayOneShot(gateSound);
        }

        if (isMissionDoor && isOpen && !missionCompleted)
        {
            if (MissionListManager.instance != null)
            {
                bool completed = MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);

                if (completed)
                {
                    missionCompleted = true;
                }
            }
        }
    }
}