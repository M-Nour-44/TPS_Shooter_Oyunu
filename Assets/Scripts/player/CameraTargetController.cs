using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [Tooltip("PlayerScript referansı (Karakterin çömelme durumunu okumak için)")]
    public PlayerScript playerScript;

    [Tooltip("Ayakta kamera hedefinin yerel Y pozisyonu")]
    public float standCameraHeight = 0.0f;
    [Tooltip("Cömelince kamera hedefinin yerel Y pozisyonu")]
    public float crouchCameraHeight = -0.8f;
    [Tooltip("Geçiş pürüzsüzlüğü (saniyedeki hız)")]
    public float transitionSpeed = 10f;

    void Start()
    {
        // Eğer playerScript atanmamışsa, üst objeden otomatik bulmayı dene
        if (playerScript == null && transform.parent != null)
        {
            playerScript = transform.parent.GetComponent<PlayerScript>();
        }
    }

    void Update()
    {
        if (playerScript == null) return;

        // Çömelme durumuna göre hedef yüksekliği belirle
        float targetHeight = playerScript.IsSitting() ? crouchCameraHeight : standCameraHeight;

        // Yumuşak bir şekilde pozisyonu güncelle
        Vector3 localPos = transform.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, targetHeight, transitionSpeed * Time.deltaTime);
        transform.localPosition = localPos;
    }
}
