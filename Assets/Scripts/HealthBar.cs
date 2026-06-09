using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Main Slider")]
    public Slider healthbarSlider;

    [Header("Health Bar Objects")]
    public GameObject greenBarObject; 
    public GameObject redBarObject;   

    [Header("Low Health Effects")]
    public GameObject bloodScreen;      // كائن صورة الدم على الشاشة
    public AudioSource heartbeatSource; // مصدر صوت نبضات القلب

    [Header("Settings")]
    public float lowHealthThreshold = 30f; 

    public void GiveFullHealth(float health)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.maxValue = health;
            healthbarSlider.value = health;
        }
        UpdateBarColor(health);
    }

    public void SetHealth(float health)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.value = health;
        }
        UpdateBarColor(health);
    }

    private void UpdateBarColor(float currentHealth)
    {
        if (healthbarSlider == null) return;

        // ==========================================
        // 1. التحكم بألوان شريط الصحة (الأخضر والأحمر)
        // ==========================================
        if (currentHealth <= lowHealthThreshold)
        {
            if (greenBarObject != null) greenBarObject.SetActive(false);
            if (redBarObject != null) redBarObject.SetActive(true);

            if (redBarObject != null)
            {
                healthbarSlider.fillRect = redBarObject.GetComponent<RectTransform>();
            }
        }
        else
        {
            if (greenBarObject != null) greenBarObject.SetActive(true);
            if (redBarObject != null) redBarObject.SetActive(false);

            if (greenBarObject != null)
            {
                healthbarSlider.fillRect = greenBarObject.GetComponent<RectTransform>();
            }
        }

        // ==========================================
        // 2. التحكم بتأثيرات الدم ونبض القلب
        // ==========================================
        if (currentHealth <= 0)
        {
            // 1. حالة الموت: إبقاء صورة الدم ظاهرة، وإيقاف صوت النبض
            if (bloodScreen != null) bloodScreen.SetActive(true);
            
            if (heartbeatSource != null && heartbeatSource.isPlaying)
            {
                heartbeatSource.Stop();
            }
        }
        else if (currentHealth <= lowHealthThreshold)
        {
            // 2. حالة الخطر (الدم قليل): إظهار صورة الدم وتشغيل النبض
            if (bloodScreen != null) bloodScreen.SetActive(true);
            
            if (heartbeatSource != null && !heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
        }
        else
        {
            // 3. حالة التعافي (الدم مليء): إخفاء صورة الدم وإيقاف النبض
            if (bloodScreen != null) bloodScreen.SetActive(false);
            
            if (heartbeatSource != null && heartbeatSource.isPlaying)
            {
                heartbeatSource.Stop();
            }
        }
    }
}