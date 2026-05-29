using UnityEngine;

public class FinalLocationTrigger : MonoBehaviour
{
    public int requiredPreviousMission = 3;
    public int missionNumber = 4;
    public string missionCompleteText = "4.Go to the marked location";

    private bool completed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (MissionListManager.instance == null)
        {
            return;
        }

        if (!MissionListManager.instance.IsMissionCompleted(requiredPreviousMission))
        {
            return;
        }

        bool done = MissionListManager.instance.CompleteMission(missionNumber, missionCompleteText);

        if (done)
        {
            completed = true;
        }
    }
}