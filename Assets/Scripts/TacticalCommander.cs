using UnityEngine;

public class TacticalCommander : MonoBehaviour
{
    [Header("Komut Ayarları")]
    public Camera mainCamera;       
    public Ally allyScript;         
    public float commandRange = 100f; 

    void Update()
    {
        // F TUŞU: Bağlamsal Eylem (Git veya Saldır)
        if (Input.GetKeyDown(KeyCode.F))
        {
            GiveTacticalCommand();
        }

        // E TUŞU: Yanıma Dön / Serbest Otomatik Mod
        if (Input.GetKeyDown(KeyCode.E))
        {
            RegroupCommand();
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
                // Düşmana çarptıysa: Saldır
                if (allyScript != null)
                {
                    allyScript.CommandAttackTarget(enemyHit.transform);
                }
            }
            else
            {
                // Zemine çarptıysa: Oraya git ve bekle
                if (allyScript != null)
                {
                    allyScript.CommandMoveToLocation(hit.point);
                }
            }
        }
    }

    void RegroupCommand()
    {
        Debug.Log("KOMUT: Yanıma Dön / Takip Et!");
        if (allyScript != null)
        {
            allyScript.CommandRegroup(); // Ally'ı normal FSM takip moduna döndürür
        }
    }
}