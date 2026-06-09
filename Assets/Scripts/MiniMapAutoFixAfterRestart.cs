using System.Collections;
using UnityEngine;

public class MiniMapAutoFixAfterRestart : MonoBehaviour
{
    public bl_MiniMap miniMap;
    public string playerTag = "Player";
    public float fixDelay = 0.3f;

    private void Awake()
    {
        if (miniMap == null)
        {
            miniMap = GetComponentInChildren<bl_MiniMap>(true);
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForSeconds(fixDelay);

        FixMiniMap();
    }

    public void FixMiniMap()
    {
        if (miniMap == null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            miniMap.SetTarget(playerObject);
        }

        Canvas.ForceUpdateCanvases();

        miniMap.IsFullScreen = false;
        miniMap.Zoom = miniMap.DefaultHeight;

        if (miniMap.miniMapCamera != null)
        {
            miniMap.miniMapCamera.orthographicSize = miniMap.DefaultHeight;
        }

        if (miniMap.HasTarget() && miniMap.minimapRig != null)
        {
            Transform target = miniMap.Target;

            Vector3 rigPosition = miniMap.minimapRig.position;
            rigPosition.x = target.position.x;
            rigPosition.z = target.position.z;
            rigPosition.y = target.TransformPoint(Vector3.up * 200f).y;

            miniMap.minimapRig.position = rigPosition;
        }

        miniMap.SetToFullscreenSize();
        Canvas.ForceUpdateCanvases();

        miniMap.SetToMiniMapSize();
        Canvas.ForceUpdateCanvases();

        miniMap.GoToTarget();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}