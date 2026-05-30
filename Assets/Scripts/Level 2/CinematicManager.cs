using System.Collections;
using UnityEngine;

public class CinematicManager : MonoBehaviour
{
    void Start()
    {
        // إيقاف وتصفير جميع تأثيرات النار فور بدء اللعبة لضمان عدم عملها مبكراً
        if (fireEffects != null)
        {
            foreach (ParticleSystem fire in fireEffects)
            {
                if (fire != null)
                {
                    fire.Stop();  // إيقاف انبعاث لهب جديد
                    fire.Clear(); // مسح أي لهب ظاهر في المشهد فوراً
                }
            }
        }
    }
    [Header("Cinematic Settings")]
    public GameObject playerMainCamera;     // كاميرا اللاعب
    public GameObject outsideFactoryCamera; // كاميرا التصوير الخارجي
    public ParticleSystem explosionEffect;  // تأثير الانفجار
    public ParticleSystem[] fireEffects; // 👈 تحول إلى مصفوفة تستقبل نيران متعددة
    public GameObject gameOverPanel;        // واجهة الـ Game Over
    public float waitBeforeGameOver = 3.5f; // مدة المشهد قبل ظهور شاشة الخسارة

    // هذه الدالة العامة سنقوم بمناداتها من سكريبت القنبلة
    public void StartExplosionCinematic()
    {
        StartCoroutine(ExplosionSequence());
    }

    IEnumerator ExplosionSequence()
    {
        // 1. إيقاف الكاميرا واللاعب
        if (playerMainCamera != null) playerMainCamera.SetActive(false);

        // 2. تشغيل الكاميرا الخارجية
        if (outsideFactoryCamera != null) outsideFactoryCamera.SetActive(true);

        // 3. تشغيل تأثير الانفجار البصري
        if (explosionEffect != null) explosionEffect.Play();
        // تشغيل جميع تأثيرات النار المضافة في القائمة دفعة واحدة
        if (fireEffects != null)
        {
            foreach (ParticleSystem fire in fireEffects)
            {
                if (fire != null) fire.Play();

           }
        }

        // 4. الانتظار لعدة ثواني (ليشاهد اللاعب الانفجار)
        yield return new WaitForSeconds(waitBeforeGameOver);

        // 5. إظهار شاشة Game Over وإظهار مؤشر الفأرة
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        

    }
}