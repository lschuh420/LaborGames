using UnityEngine;
using System;

public class MechBossHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 2500;
    public int currentHealth;

    public static event Action OnMechDeath;
    public static event Action<int> OnMechStun; // Passiert beim ersten Bein
    public static event Action OnMechLimp; // Passiert beim zweiten Bein

    private int destroyedLegsCount = 0;
    private bool isDead = false;

    public bool IsStunned { get; private set; }
    public bool IsLimping { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"<color=yellow>[MechBoss] Schaden erhalten: {damageAmount}. Restliche HP: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            TriggerMechDeath();
        }
    }

    public void ReportLegDestroyed()
    {
        destroyedLegsCount++;
        Debug.Log($"<color=orange>[MechBoss] Bein zerstört! Insgesamt: {destroyedLegsCount}</color>");

        if (destroyedLegsCount == 1)
        {
            IsStunned = true;
            OnMechStun?.Invoke(3); // 3 Sekunden Stun
            Invoke(nameof(ResetStun), 3f);
        }
        else if (destroyedLegsCount >= 2)
        {
            IsLimping = true;
            OnMechLimp?.Invoke();
        }
    }

    private void ResetStun()
    {
        IsStunned = false;
        Debug.Log("[MechBoss] Stun beendet. Mech steht wieder auf!");
    }

    public void TriggerMechDeath()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red>CÜSSS! Der Mech ist Schrott!</color>");
        OnMechDeath?.Invoke();
        
        // Hier könnten weitere Todes-Logiken rein (Explosionen, etc.)
        // Destroy(gameObject); // Oder Deaktivieren
    }
}
