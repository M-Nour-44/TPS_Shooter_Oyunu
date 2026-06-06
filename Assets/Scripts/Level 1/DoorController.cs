using UnityEngine;
using UnityEngine.AI;

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

    [Header("Guards & NavMesh")]
    [Tooltip("Kapı açıldığında serbest bırakılacak sabit nöbetçiler")]
    public Enemy[] guardsToRelease;
    [Tooltip("Kapı kapalıyken yapay zekanın geçişini engelleyen engelleyici")]
    public NavMeshObstacle navObstacle;

    public void ToggleDoor()
    {
        if (animator == null)
        {
            return;
        }

        isOpen = !isOpen;

        animator.SetBool("IsOpen", isOpen);

        // Kapı açıldıysa engeli kaldır, kapandıysa engeli koy
        if (navObstacle != null)
        {
            navObstacle.carving = !isOpen;
            navObstacle.enabled = !isOpen;
        }

        // Kapı açıldığında bağlı nöbetçileri serbest bırak
        if (isOpen && guardsToRelease != null)
        {
            foreach (Enemy guard in guardsToRelease)
            {
                if (guard != null)
                {
                    guard.stationaryGuard = false;
                }
            }
        }

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