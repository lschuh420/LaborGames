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

    [Header("Behavior Settings")]
    [Tooltip("Wie lange bleibt der Mech am Boden liegen, wenn ein Bein zerstört wird?")]
    public float legDestroyedStunDuration = 4.0f;

    [Header("Death Sequence")]
    [Tooltip("Effekt für die Todes-Explosionen")]
    public GameObject deathExplosionPrefab;
    [Tooltip("Wie viele Explosionen sollen vor dem finalen Aus kommen?")]
    public int deathExplosionCount = 6;

    public static event Action OnMechDeath;
    public static event Action<int> OnMechStun; // Passiert beim ersten Bein
    public static event Action OnMechLimp; // Passiert beim zweiten Bein
    public event Action<int, int> OnHealthChanged; // Current, Max
    public static event Action<int> OnLegDestroyed; // Count

    private int destroyedLegsCount = 0;
    private bool isDead = false;

    public int DestroyedLegsCount => destroyedLegsCount;
    public bool IsDead => isDead;

    public bool IsStunned { get; private set; }
    public bool IsLimping { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Falls kein spezielles Todes-Prefab zugewiesen, versuchen wir das Beine-FX zu finden
        if (deathExplosionPrefab == null)
        {
            // Wir suchen im Projekt nach einem passenden Effekt als Fallback
            // (In der Realität würde man das im Editor zuweisen)
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"<color=yellow>[MechBoss] Schaden erhalten: {damageAmount}. Restliche HP: {currentHealth}</color>");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Armor Hit Sound abspielen
        if (audioSource != null && armorHitSound != null)
        {
            audioSource.PlayOneShot(armorHitSound, audioVolume);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DeathSequenceCoroutine());
        }
    }

    private System.Collections.IEnumerator DeathSequenceCoroutine()
    {
        if (isDead) yield break;
        isDead = true;

        Debug.Log("<color=red><b>[MechBoss] INITIALISIERE SELBSTZERSTÖRUNG...</b></color>");
        
        // 1. Alle KI und Bewegung sofort stoppen
        IsStunned = true; // Sorgt für den Collapse-Pose
        
        // 2. Kettenreaktion von Explosionen
        for (int i = 0; i < deathExplosionCount; i++)
        {
            // Zufällige Position am Körper finden (ungefähr)
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 2.5f;
            randomOffset.y = UnityEngine.Random.Range(0.5f, 4f);
            Vector3 explosionPos = transform.position + randomOffset;

            if (deathExplosionPrefab != null)
            {
                Instantiate(deathExplosionPrefab, explosionPos, Quaternion.identity);
            }

            if (audioSource != null && armorHitSound != null)
            {
                audioSource.PlayOneShot(armorHitSound, 1.0f); // Knall-Effekt
            }

            // Kurze Pause zwischen den Knallern
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.5f));
        }

        // 3. Finaler Blow-out
        TriggerMechDeath();
    }

    public void ReportLegDestroyed()
    {
        if (isDead) return;
        destroyedLegsCount++;
        Debug.Log($"<color=orange>[MechBoss] Bein zerstört! Insgesamt: {destroyedLegsCount}</color>");

        OnLegDestroyed?.Invoke(destroyedLegsCount);

        // JEDES Bein löst jetzt einen kurzen Stun/Stagger aus (Arc Raiders Style)
        IsStunned = true;
        
        // Stun Sound abspielen
        if (audioSource != null && stunSound != null)
        {
            audioSource.PlayOneShot(stunSound, 0.8f);
        }

        OnMechStun?.Invoke((int)legDestroyedStunDuration);
        
        // Timer für Reset (Cancel falls bereits einer läuft)
        CancelInvoke(nameof(ResetStun));
        Invoke(nameof(ResetStun), legDestroyedStunDuration);

        if (destroyedLegsCount >= 2)
        {
            IsLimping = true;
            OnMechLimp?.Invoke();
        }
    }

    private void ResetStun()
    {
        if (isDead) return;
        IsStunned = false;
        Debug.Log("[MechBoss] Stun beendet. Mech steht wieder auf!");
    }

    public void TriggerMechDeath()
    {
        // Finale Zerstörung: Alles ausschalten
        Debug.Log("<color=red><b>KOMPLETT AUSFALL. Der Mech ist nur noch Schrott.</b></color>");

        // Todessound abspielen
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, 1.0f);
        }

        OnMechDeath?.Invoke();

        // 1. NavMeshAgent SOFORT ausschalten (verhindert das Versinken!)
        UnityEngine.AI.NavMeshAgent agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) 
        {
            agent.enabled = false;
            agent.velocity = Vector3.zero; // Bewegung sofort stoppen
        }

        // 2. Skripte deaktivieren
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s != this && s.GetType().Name != "BodyAdaptation")
            {
                s.enabled = false;
            }
        }

        // 3. TEILE ABFALLEN LASSEN (Arc Raiders Style)
        // Wir suchen nach Objekten mit bestimmten Namen oder MeshRenderern
        MeshRenderer[] meshParts = GetComponentsInChildren<MeshRenderer>();
        foreach (var part in meshParts)
        {
            // Zufallschance, dass ein Teil wirklich physisch abfällt (nur kleinere Teile)
            if (UnityEngine.Random.value > 0.4f && part.gameObject != this.gameObject)
            {
                // Teil vom Körper lösen
                GameObject obj = part.gameObject;
                
                // Falls es kein Root-Teil ist
                if (obj.transform.parent != null)
                {
                    obj.transform.SetParent(null); // In die Welt entlassen
                    
                    // Physik hinzufügen
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb == null) rb = obj.AddComponent<Rigidbody>();
                    rb.mass = 5f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    
                    // Collider hinzufügen/aktivieren
                    Collider col = obj.GetComponent<Collider>();
                    if (col == null) col = obj.AddComponent<BoxCollider>();
                    col.enabled = true;

                    // Kleiner Impuls für den "Explosions-Effekt"
                    rb.AddExplosionForce(10f, transform.position, 5f, 1f, ForceMode.Impulse);
                    
                    // Nach 20 Sekunden aufräumen
                    Destroy(obj, 20f);
                }
            }
        }

        // 4. Den Haupt-Körper am Boden fixieren
        // Wir sorgen dafür, dass ein Collider aktiv bleibt, der nicht durch den Boden fällt
        Collider mainCol = GetComponentInParent<Collider>();
        if (mainCol != null) mainCol.enabled = true;
        
        Rigidbody mainRb = GetComponentInParent<Rigidbody>();
        if (mainRb != null)
        {
            mainRb.isKinematic = false;
            mainRb.useGravity = true;
            mainRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}
