using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Raycast")]
    public Camera playerCamera;
    public float interactRange = 4f;

    [Header("UI")]
    public TextMeshProUGUI promptText;
    public CanvasGroup promptCanvasGroup;

    [Header("Computer Prompt Object")]
    public GameObject[] computerPromptObjects;

    [Header("Texts")]
    public string doorText = "Press \"E\" to open door";
    public string computerText = "Press \"E\" to disable computer";
    public string generatorText = "Press \"E\" to turn off generator";

    [Header("Fade")]
    public float fadeSpeed = 10f;

    private bool shouldShow = false;

    private void Start()
    {
        HideImmediate();
    }

    private void Update()
    {
        CheckInteractionTarget();
        UpdatePromptFade();
    }

    private void CheckInteractionTarget()
    {
        shouldShow = false;

        if (playerCamera == null || promptText == null)
        {
            return;
        }

        RaycastHit hit;

        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactRange))
        {
            return;
        }

        DoorController door = hit.collider.GetComponentInParent<DoorController>();

        if (door != null && hit.collider.CompareTag("Door"))
        {
            if (MissionListManager.instance == null || !MissionListManager.instance.IsMissionCompleted(1))
            {
                ShowPrompt(doorText);
                return;
            }
        }

        if (IsHitInObjects(hit.collider.transform, computerPromptObjects))
        {
            if (MissionListManager.instance != null &&
                MissionListManager.instance.IsMissionCompleted(1) &&
                !MissionListManager.instance.IsMissionCompleted(2))
            {
                ShowPrompt(computerText);
                return;
            }
        }

        GeneratorTurnOff generator = hit.collider.GetComponentInParent<GeneratorTurnOff>();

        if (generator != null)
        {
            if (MissionListManager.instance != null &&
                MissionListManager.instance.IsMissionCompleted(2) &&
                !generator.button)
            {
                ShowPrompt(generatorText);
                return;
            }
        }
    }

    private bool IsHitInObjects(Transform hitTransform, GameObject[] objects)
    {
        if (hitTransform == null || objects == null)
        {
            return false;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (hitTransform == objects[i].transform || hitTransform.IsChildOf(objects[i].transform))
            {
                return true;
            }
        }

        return false;
    }

    private void ShowPrompt(string text)
    {
        promptText.text = text;
        shouldShow = true;
    }

    private void UpdatePromptFade()
    {
        if (promptCanvasGroup == null)
        {
            return;
        }

        float targetAlpha = shouldShow ? 1f : 0f;

        promptCanvasGroup.alpha = Mathf.MoveTowards(
            promptCanvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }

    private void HideImmediate()
    {
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }
    }
}