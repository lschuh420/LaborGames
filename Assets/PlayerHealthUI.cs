using UnityEngine;
using UnityEngine.UI;

// Spieler-Lebensanzeige unten links. Gleicher Front/Back-Effekt wie BossHealthUI.
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

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
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        if (frontHealthBar != null) frontHealthBar.fillAmount = 1f;
        if (backHealthBar != null) backHealthBar.fillAmount = 1f;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        float targetPercentage = Mathf.Clamp01((float)current / max);

        if (frontHealthBar != null)
        {
            // Bei Schaden: Front sofort runter, Back zieht verzögert nach
            if (targetPercentage < frontHealthBar.fillAmount)
                delayTimer = catchUpDelay;

            frontHealthBar.fillAmount = targetPercentage;
        }
    }

    void Update()
    {
        if (frontHealthBar == null || backHealthBar == null) return;

        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else
        {
            // Funktioniert in beide Richtungen: Schaden (Back schrumpft nach)
            // und Heilung (Back wächst weich mit der Front mit)
            backHealthBar.fillAmount = Mathf.Lerp(
                backHealthBar.fillAmount,
                frontHealthBar.fillAmount,
                Time.deltaTime * catchUpSpeed);
        }
    }
}
