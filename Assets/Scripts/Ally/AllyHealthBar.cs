using UnityEngine;
using UnityEngine.UI;

public class AllyHealthBar : MonoBehaviour
{
    [Header("Slider Component")]
    [Tooltip("اسحب السلايدر الخاص بصحة الحليف هنا")]
    public Slider healthbarSlider;

    private void Awake()
    {
        // حماية إضافية: إذا نسيت ربط السلايدر، سيبحث عنه السكريبت بنفسه
        if (healthbarSlider == null)
        {
            healthbarSlider = GetComponent<Slider>();
        }
    }

    // دالة لتحديد الصحة القصوى للحليف عند بداية ظهوره
    // لاحظ أننا غيرنا الاسم هنا إلى GiveFullHealth ليتطابق مع سكريبت Ally.cs
    public void GiveFullHealth(float maxHealth)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.maxValue = maxHealth;
            healthbarSlider.value = maxHealth;
        }
    }

    // دالة لتحديث شريط الصحة عندما يتلقى الحليف رصاصة أو ضرر
    public void SetHealth(float currentHealth)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.value = currentHealth;
        }
    }
}