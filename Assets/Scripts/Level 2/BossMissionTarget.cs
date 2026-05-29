using UnityEngine;

public class BossMissionTarget : MonoBehaviour
{
    public int missionNumber = 2;
    public string missionCompleteText = "2.Big enemy eliminated";

    private bool completed = false;

    public void CompleteBossMission()
    {
        if (completed)
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