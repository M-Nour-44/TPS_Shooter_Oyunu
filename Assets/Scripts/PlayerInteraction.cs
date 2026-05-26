using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 4f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        MissionListManager.instance.CompleteMission(1, "1.Door is opened");
        }
    }

    void TryInteract()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Door"))
            {
                DoorController door = hit.collider.GetComponentInParent<DoorController>();

                if (door != null)
                {
                    door.ToggleDoor();
                }
            }
        }
    }
}