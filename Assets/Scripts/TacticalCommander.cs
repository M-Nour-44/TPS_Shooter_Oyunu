using UnityEngine;

public class TacticalCommander : MonoBehaviour
{
    [Header("Komut Ayarları")]
    public Camera mainCamera;       // Ekranın ortasını (nişangahı) bulmak için
    public Ally allyScript;         // Komut vereceğimiz askerimiz
    public float commandRange = 100f; // Lazerin (emrin) ulaşacağı maksimum menzil

    void Update()
    {
        // F tuşuna basıldığında komut lazerini ateşle
        if (Input.GetKeyDown(KeyCode.F))
        {
            GiveCommand();
        }
    }

    void GiveCommand()
    {
        // Kameranın/Ekranın tam ortasından ileriye sanal bir lazer gönder
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, commandRange))
        {
            Debug.Log("Komut verildi. Hedef koordinat: " + hit.point);
            
            // Lazerin çarptığı noktayı (x,y,z) Ally'a gönder ve gitmesini emret
            if (allyScript != null)
            {
                allyScript.CommandMoveToLocation(hit.point);
            }
        }
    }
}