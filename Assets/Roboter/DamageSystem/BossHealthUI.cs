using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    public MechBossHealth bossHealth;
    
    [Header("UI Elements")]
    [Tooltip("Die rote Leiste, die sofort schrumpft")]
    public Image frontHealthBar; 
    [Tooltip("Die weiße/gelbe Leiste, die verzögert hinterherzieht")]
    public Image backHealthBar; 

    [Header("Animation Settings")]
    public float catchUpSpeed = 3f;
    public float catchUpDelay = 0.8f;

    private float delayTimer;

    void Start()
    {
        // Falls nichts zugewiesen wurde, suchen wir den Boss automatisch
        if (bossHealth == null)
        {
            bossHealth = FindAnyObjectByType<MechBossHealth>();
        }

        if (frontHealthBar != null) frontHealthBar.fillAmount = 1f;
        if (backHealthBar != null) backHealthBar.fillAmount = 1f;
    }

    void Update()
    {
        if (bossHealth == null || frontHealthBar == null || backHealthBar == null) return;

        // 1. Ziel-Gesundheit in Prozent berechnen (0.0 bis 1.0)
        float targetPercentage = Mathf.Clamp01((float)bossHealth.currentHealth / bossHealth.maxHealth);
        
        // 2. Front Bar (Rot) sofort anpassen, wenn Schaden genommen wird
        if (targetPercentage < frontHealthBar.fillAmount)
        {
            frontHealthBar.fillAmount = targetPercentage;
            delayTimer = catchUpDelay; // Timer für die hintere Leiste resetten
        }

        // 3. Back Bar (Weiß/Gelb) verzögert und weich hinterherziehen (AAA-Effekt)
        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else
        {
            if (backHealthBar.fillAmount > frontHealthBar.fillAmount)
            {
                // SmoothDamp / Lerp für das softe "Nachrutschen"
                backHealthBar.fillAmount = Mathf.Lerp(backHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * catchUpSpeed);
            }
        }
    }
}
