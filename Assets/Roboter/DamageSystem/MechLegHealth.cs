using UnityEngine;

public class MechLegHealth : MonoBehaviour
{
    [Header("Leg Settings")]
    public int maxLegHealth = 300;
    public int currentLegHealth;
    public bool isDestroyed = false;

    [Header("Visuals & Effects")]
    public GameObject legMesh; // Das Mesh, das abgetrennt wird
    public ParticleSystem explosionPrefab;
    
    [Header("References")]
    public MechBossHealth bossMainHealth;
    public LegIK legIK; // Referenz zur IK, um sie zu deaktivieren
    public Transform footTip; // Wichtig für BodyAdaptation

    private void Awake()
    {
        currentLegHealth = maxLegHealth;
    }

    public void TakeLegDamage(int amount)
    {
        if (isDestroyed) return;

        currentLegHealth -= amount;
        Debug.Log($"[LegHealth] Bein-HP: {currentLegHealth}");

        if (currentLegHealth <= 0)
        {
            DestroyLeg();
        }
    }

    private void DestroyLeg()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("<color=orange>[LegHealth] BOOM! Bein weg!</color>");

        // 1. IK deaktivieren
        if (legIK != null)
        {
            legIK.enabled = false;
        }

        // 2. Procedural System informieren (Kein Animation-Gedöns, pure IK!)
        // Wir suchen die Systeme auf dem Root
        Transform root = transform.root;
        StepManager stepManager = root.GetComponentInChildren<StepManager>();
        BodyAdaptation bodyAdapt = root.GetComponentInChildren<BodyAdaptation>();

        if (stepManager != null)
        {
            stepManager.ReportLegDestroyed(this.transform); // Dieses Bein ist der Root für den StepManager
        }

        if (bodyAdapt != null && footTip != null)
        {
            bodyAdapt.ReportFootDestroyed(footTip);
        }

        // 3. Visuelles Feedback: Mesh "abtrennen"
        if (legMesh != null)
        {
            // Sicherstellen, dass das Bein nicht durch den Boden fällt
            Rigidbody rb = legMesh.GetComponent<Rigidbody>();
            if (rb == null) rb = legMesh.AddComponent<Rigidbody>();
            
            // Collider checken/hinzufügen falls nötig
            Collider col = legMesh.GetComponent<Collider>();
            if (col == null) col = legMesh.AddComponent<BoxCollider>(); // Fallback
            
            legMesh.transform.SetParent(null); // Vom Boss trennen
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Gegen Tunneling

            // Ein kleiner Impuls nach außen/oben
            Vector3 forceDir = (legMesh.transform.position - root.position).normalized + Vector3.up;
            rb.AddForce(forceDir * 5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        // 4. Explosion spawnen
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 5. Boss informieren
        if (bossMainHealth != null)
        {
            bossMainHealth.ReportLegDestroyed();
        }
    }
}
