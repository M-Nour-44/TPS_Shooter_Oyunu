using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("كمية الصحة التي ستزيد للاعب")]
    public float healAmount = 40f; 

    [Header("Sound Settings")]
    [Tooltip("اسحب ملف صوت التقاط الحقيبة وضعه هنا")]
    public AudioClip healSound; // المتغير الجديد للصوت
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerScript player = other.GetComponentInParent<PlayerScript>();

            if (player != null && !player.IsDead())
            {
                // 1. زيادة صحة اللاعب
                player.HealPlayer(healAmount);

                // 2. تشغيل الصوت في مكان الحقيبة (إذا كان ملف الصوت موجوداً)
                if (healSound != null)
                {
                    AudioSource.PlayClipAtPoint(healSound, transform.position);
                }

                // 3. تدمير الحقيبة فوراً
                Destroy(gameObject);
            }
        }
    }
}