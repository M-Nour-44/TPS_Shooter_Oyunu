using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Slider Component")]
    [Tooltip("اسحب السلايدر الخاص بصحة العدو هنا")]
    public Slider healthbarSlider;

    private void Awake()
    {
        // حماية إضافية: إذا نسيت ربط السلايدر، سيبحث عنه السكريبت بنفسه
        if (healthbarSlider == null)
        {
            healthbarSlider = GetComponent<Slider>();
        }
    }

    // دالة لتحديد الصحة القصوى للعدو عند بداية ظهوره
    public void SetMaxHealth(float maxHealth)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.maxValue = maxHealth;
            healthbarSlider.value = maxHealth;
        }
    }

    // دالة لتحديث شريط الصحة عندما يتلقى العدو رصاصة أو ضرر
    public void SetHealth(float currentHealth)
    {
        if (healthbarSlider != null)
        {
            healthbarSlider.value = currentHealth;
        }
    }
}