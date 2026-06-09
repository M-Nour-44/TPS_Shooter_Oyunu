using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float updateInterval = 0.25f;

    private float timer;
    private int frameCount;
    private float fps;

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            fps = frameCount / timer;

            if (fpsText != null)
            {
                fpsText.text = "FPS: " + Mathf.RoundToInt(fps);
            }

            frameCount = 0;
            timer = 0f;
        }
    }
}