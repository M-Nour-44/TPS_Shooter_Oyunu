using UnityEngine;
using TMPro;

public class Level2InteractionPromptUI : MonoBehaviour
{
    [Header("Raycast")]
    public Camera playerCamera;
    public float interactRange = 4f;

    [Header("UI")]
    public TextMeshProUGUI promptText;
    public CanvasGroup promptCanvasGroup;

    [Header("Bomb Prompt Objects")]
    public GameObject[] bombPromptObjects;

    [Header("Texts")]
    public string bombText = "Hold \"E\" to defuse bomb";

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

        BombDefusal bomb = hit.collider.GetComponentInParent<BombDefusal>();

        bool hitBombObject = bomb != null || IsHitInObjects(hit.collider.transform, bombPromptObjects);

        if (!hitBombObject)
        {
            return;
        }

        if (Level2MissionManager.instance == null)
        {
            return;
        }

        if (Level2MissionManager.instance.IsMissionCompleted(2) &&
            !Level2MissionManager.instance.IsMissionCompleted(3))
        {
            ShowPrompt(bombText);
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