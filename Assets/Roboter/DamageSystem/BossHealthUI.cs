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

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleHealthChanged;
            // Initialer Stand
            HandleHealthChanged(bossHealth.currentHealth, bossHealth.maxHealth);
        }

        if (frontHealthBar != null) frontHealthBar.fillAmount = 1f;
        if (backHealthBar != null) backHealthBar.fillAmount = 1f;
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        float targetPercentage = Mathf.Clamp01((float)current / max);
        
        if (frontHealthBar != null)
        {
            if (targetPercentage < frontHealthBar.fillAmount)
            {
                frontHealthBar.fillAmount = targetPercentage;
                delayTimer = catchUpDelay;
            }
            else
            {
                frontHealthBar.fillAmount = targetPercentage;
            }
        }
    }

    void Update()
    {
        if (frontHealthBar == null || backHealthBar == null) return;

        // Back Bar (Weiß/Gelb) verzögert und weich hinterherziehen (AAA-Effekt)
        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else
        {
            if (backHealthBar.fillAmount > frontHealthBar.fillAmount)
            {
                backHealthBar.fillAmount = Mathf.Lerp(backHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * catchUpSpeed);
            }
        }
    }
}
