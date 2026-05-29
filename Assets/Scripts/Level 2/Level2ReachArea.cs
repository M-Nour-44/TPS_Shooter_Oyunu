using UnityEngine;

public class Level2ReachArea : MonoBehaviour
{
    public int missionNumber = 1;
    public string missionCompleteText = "1.Marked area reached";

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

        if (Level2MissionManager.instance == null)
        {
            return;
        }

        bool done = Level2MissionManager.instance.CompleteMission(missionNumber, missionCompleteText);

        if (done)
        {
            completed = true;
        }
    }
}