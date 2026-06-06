using UnityEngine;

public class TacticalCommander : MonoBehaviour
{
    [Header("Komut Ayarları")]
    public Camera mainCamera;       
    public Ally allyScript;         
    public float commandRange = 100f; 

    [Header("Hedef Algılama")]
    [Tooltip("Hangi katmanların 'Yürünebilir Zemin' olduğunu buradan seçin")]
    public LayerMask groundLayer; // --- YENİ: Zemin Filtresi ---

    [Header("Görsel İşaretçiler")]
    public GameObject moveMarkerPrefab;   
    public GameObject attackMarkerPrefab; 

    [Header("İşaretçi Yükseklik Ayarları")]
    public float moveMarkerYOffset = 0.05f; 
    public float attackMarkerYOffset = -1.0f; 

    private GameObject activeMoveMarker;
    private GameObject activeAttackMarker;

    void Start()
    {
        if (moveMarkerPrefab != null)
        {
            activeMoveMarker = Instantiate(moveMarkerPrefab);
            activeMoveMarker.SetActive(false);
        }

        if (attackMarkerPrefab != null)
        {
            activeAttackMarker = Instantiate(attackMarkerPrefab);
            activeAttackMarker.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            GiveTacticalCommand();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RegroupCommand();
        }
        
        if (activeAttackMarker != null && activeAttackMarker.activeSelf)
        {
            if (activeAttackMarker.transform.parent == null || !activeAttackMarker.transform.parent.gameObject.activeInHierarchy)
            {
                activeAttackMarker.SetActive(false);
            }
        }
    }

    void GiveTacticalCommand()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, commandRange))
        {
            Enemy enemyHit = hit.transform.GetComponentInParent<Enemy>();

            if (enemyHit != null && enemyHit.gameObject.activeInHierarchy)
            {
                // ================= DÜŞMANA SALDIRI EMRİ =================
                if (allyScript != null) allyScript.CommandAttackTarget(enemyHit.transform);
                
                if (activeMoveMarker != null) activeMoveMarker.SetActive(false);
                if (activeAttackMarker != null)
                {
                    activeAttackMarker.SetActive(true);
                    activeAttackMarker.transform.SetParent(enemyHit.transform);
                    activeAttackMarker.transform.localPosition = new Vector3(0, attackMarkerYOffset, 0); 
                }
            }
            // --- YENİ EKLENDİ: Çarpan nesne 'Zemin' katmanında mı? ---
            else if ((groundLayer.value & (1 << hit.transform.gameObject.layer)) > 0)
            {
                // ================= ZEMİNE GİTME EMRİ =================
                if (allyScript != null) allyScript.CommandMoveToLocation(hit.point);

                if (activeAttackMarker != null) activeAttackMarker.SetActive(false);
                if (activeMoveMarker != null)
                {
                    activeMoveMarker.SetActive(true);
                    activeMoveMarker.transform.SetParent(null); 
                    activeMoveMarker.transform.position = hit.point + new Vector3(0, moveMarkerYOffset, 0); 
                }
            }
            else
            {
                // Duvara, arabaya veya alakasız bir cisme tıklandı. Harekete geçme!
                Debug.Log("Geçersiz komut noktası! Sadece zemine veya düşmana emir verilebilir.");
            }
        }
    }

    void RegroupCommand()
    {
        if (allyScript != null) allyScript.CommandRegroup(); 

        if (activeMoveMarker != null) activeMoveMarker.SetActive(false);
        if (activeAttackMarker != null) activeAttackMarker.SetActive(false);
    }
}