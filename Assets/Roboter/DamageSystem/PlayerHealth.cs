using UnityEngine;
using System;

// Spieler-Gesundheit. Folgt demselben Muster wie MechBossHealth:
// implementiert IDamageable und feuert OnHealthChanged für die UI.
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Regeneration")]
    [Tooltip("Sekunden ohne Treffer, bevor die Heilung startet")]
    public float regenDelay = 20f;
    [Tooltip("HP pro Sekunde während der Heilung (progressiv, nicht auf einmal)")]
    public float regenRate = 5f;

    [Header("Audio (optional)")]
    public AudioClip hitSound;
    public AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.5f;
    private AudioSource audioSource;

    // Current, Max -> wird von der UI abonniert
    public event Action<int, int> OnHealthChanged;
    public static event Action OnPlayerDeath;

    private float timeSinceLastHit;
    private float regenAccumulator; // sammelt Bruchteile von HP für glatte Regeneration
    private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        timeSinceLastHit = 0f;     // Regen-Timer zurücksetzen
        regenAccumulator = 0f;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound, audioVolume);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
            return;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        // TEST: remove later. Press K to take 15 damage.
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(15);

        if (isDead || currentHealth >= maxHealth) return;

        timeSinceLastHit += Time.deltaTime;
        if (timeSinceLastHit < regenDelay) return;

        // Progressiv heilen: HP über die Zeit aufsammeln
        regenAccumulator += regenRate * Time.deltaTime;
        if (regenAccumulator >= 1f)
        {
            int healed = Mathf.FloorToInt(regenAccumulator);
            regenAccumulator -= healed;

            currentHealth = Mathf.Min(currentHealth + healed, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red><b>[Player] gestorben.</b></color>");

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound, 1f);

        OnPlayerDeath?.Invoke();
    }
}
