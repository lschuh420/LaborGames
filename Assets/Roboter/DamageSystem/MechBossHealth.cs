using UnityEngine;
using System;

public class MechBossHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 2500;
    public int currentHealth;

    [Header("Audio (Surgical)")]
    public AudioClip armorHitSound;
    public AudioClip stunSound;
    public AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.5f;
    private AudioSource audioSource;

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

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"<color=yellow>[MechBoss] Schaden erhalten: {damageAmount}. Restliche HP: {currentHealth}</color>");

        // Armor Hit Sound abspielen
        if (audioSource != null && armorHitSound != null)
        {
            audioSource.PlayOneShot(armorHitSound, audioVolume);
        }

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
            
            // Stun Sound abspielen
            if (audioSource != null && stunSound != null)
            {
                audioSource.PlayOneShot(stunSound, 0.8f);
            }

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

        // Todessound abspielen
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, 1.0f);
        }

        OnMechDeath?.Invoke();
        
        // Hier könnten weitere Todes-Logiken rein (Explosionen, etc.)
    }
}
