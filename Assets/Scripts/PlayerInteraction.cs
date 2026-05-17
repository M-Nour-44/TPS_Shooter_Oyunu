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