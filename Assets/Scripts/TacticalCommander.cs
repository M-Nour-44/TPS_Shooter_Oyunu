using UnityEngine;

public class TacticalCommander : MonoBehaviour
{
    [Header("Komut Ayarları")]
    public Camera mainCamera;       
    public Ally allyScript;         
    public float commandRange = 100f; 

    [Header("Hedef Algılama")]
    public LayerMask groundLayer; 

    [Header("Görsel İşaretçiler")]
    public GameObject moveMarkerPrefab;   
    public GameObject attackMarkerPrefab; 

    [Header("İşaretçi Yükseklik Ayarları")]
    public float moveMarkerYOffset = 0.05f; 
    public float attackMarkerYOffset = -1.0f; 

    private GameObject activeMoveMarker;
    private GameObject activeAttackMarker;

    // --- YENİ EKLENDİ: İşaretlenen düşmanı aklımızda tutuyoruz ---
    private Enemy currentTargetEnemy;

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

        if (Input.GetKeyDown(KeyCode.G))
        {
            RegroupCommand();
        }
        
        // ================= MARKER BUG ÇÖZÜMÜ =================
        if (activeAttackMarker != null && activeAttackMarker.activeSelf)
        {
            bool hideMarker = false;

            // Düşman tamamen yok olduysa
            if (currentTargetEnemy == null || !currentTargetEnemy.gameObject.activeInHierarchy)
            {
                hideMarker = true;
            }
            else
            {
                // Düşman henüz silinmedi ama ÖLDÜYSE (Çarpışma kutusu kapandıysa)
                Collider enemyCol = currentTargetEnemy.GetComponentInChildren<Collider>();
                if (enemyCol != null && !enemyCol.enabled)
                {
                    hideMarker = true;
                }
            }

            if (hideMarker)
            {
                // ÇOK ÖNEMLİ: İşaretçiyi cesedin içinden koparıyoruz ki cesetle birlikte yok olmasın!
                activeAttackMarker.transform.SetParent(null); 
                activeAttackMarker.SetActive(false);
                currentTargetEnemy = null; // Hafızayı temizle
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
                if (allyScript != null) allyScript.CommandAttackTarget(enemyHit.transform);
                
                if (activeMoveMarker != null) activeMoveMarker.SetActive(false);
                if (activeAttackMarker != null)
                {
                    currentTargetEnemy = enemyHit; // Düşmanı hafızaya al

                    activeAttackMarker.SetActive(true);
                    activeAttackMarker.transform.SetParent(enemyHit.transform);
                    activeAttackMarker.transform.localPosition = new Vector3(0, attackMarkerYOffset, 0); 
                }
            }
            else if ((groundLayer.value & (1 << hit.transform.gameObject.layer)) > 0)
            {
                if (allyScript != null) allyScript.CommandMoveToLocation(hit.point);

                if (activeAttackMarker != null) 
                {
                    activeAttackMarker.transform.SetParent(null); // Zaten açıksa serbest bırak
                    activeAttackMarker.SetActive(false);
                }
                
                if (activeMoveMarker != null)
                {
                    activeMoveMarker.SetActive(true);
                    activeMoveMarker.transform.SetParent(null); 
                    activeMoveMarker.transform.position = hit.point + new Vector3(0, moveMarkerYOffset, 0); 
                }

                currentTargetEnemy = null; 
            }
        }
    }

    void RegroupCommand()
    {
        if (allyScript != null) allyScript.CommandRegroup(); 

        if (activeMoveMarker != null) activeMoveMarker.SetActive(false);
        if (activeAttackMarker != null) 
        {
            activeAttackMarker.transform.SetParent(null);
            activeAttackMarker.SetActive(false);
        }
        
        currentTargetEnemy = null;
    }
}