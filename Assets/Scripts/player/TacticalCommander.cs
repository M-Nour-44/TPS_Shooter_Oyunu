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

    private Enemy currentTargetEnemy;

    void Start()
    {
        // ================= AUTO FIX (SCENE 2 BUG FIX) =================
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (allyScript == null)
            allyScript = FindObjectOfType<Ally>();
        // ===============================================================

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
        // ================= SAFE GUARD =================
        if (allyScript == null || mainCamera == null)
            return;

        if (allyScript.isDead)
        {
            if (activeMoveMarker != null)
                activeMoveMarker.SetActive(false);

            if (activeAttackMarker != null)
            {
                activeAttackMarker.transform.SetParent(null);
                activeAttackMarker.SetActive(false);
            }

            return;
        }
        // ============================================

        if (Input.GetKeyDown(KeyCode.F))
        {
            GiveTacticalCommand();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            RegroupCommand();
        }

        // ================= MARKER CLEANUP =================
        if (activeAttackMarker != null && activeAttackMarker.activeSelf)
        {
            bool hideMarker = false;

            if (currentTargetEnemy == null || !currentTargetEnemy.gameObject.activeInHierarchy)
            {
                hideMarker = true;
            }
            else
            {
                Collider enemyCol = currentTargetEnemy.GetComponentInChildren<Collider>();
                if (enemyCol != null && !enemyCol.enabled)
                    hideMarker = true;
            }

            if (hideMarker)
            {
                activeAttackMarker.transform.SetParent(null);
                activeAttackMarker.SetActive(false);
                currentTargetEnemy = null;
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
                if (allyScript != null)
                    allyScript.CommandAttackTarget(enemyHit.transform);

                if (activeMoveMarker != null)
                    activeMoveMarker.SetActive(false);

                if (activeAttackMarker != null)
                {
                    currentTargetEnemy = enemyHit;

                    activeAttackMarker.SetActive(true);
                    activeAttackMarker.transform.SetParent(enemyHit.transform);
                    activeAttackMarker.transform.localPosition =
                        new Vector3(0, attackMarkerYOffset, 0);
                }
            }
            else if ((groundLayer.value & (1 << hit.transform.gameObject.layer)) > 0)
            {
                if (allyScript != null)
                    allyScript.CommandMoveToLocation(hit.point);

                if (activeAttackMarker != null)
                {
                    activeAttackMarker.transform.SetParent(null);
                    activeAttackMarker.SetActive(false);
                }

                if (activeMoveMarker != null)
                {
                    activeMoveMarker.SetActive(true);
                    activeMoveMarker.transform.SetParent(null);
                    activeMoveMarker.transform.position =
                        hit.point + new Vector3(0, moveMarkerYOffset, 0);
                }

                currentTargetEnemy = null;
            }
        }
    }

    void RegroupCommand()
    {
        if (allyScript != null)
            allyScript.CommandRegroup();

        if (activeMoveMarker != null)
            activeMoveMarker.SetActive(false);

        if (activeAttackMarker != null)
        {
            activeAttackMarker.transform.SetParent(null);
            activeAttackMarker.SetActive(false);
        }

        currentTargetEnemy = null;
    }
}