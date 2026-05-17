using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator animator;

    private bool isOpen = false;

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }
}