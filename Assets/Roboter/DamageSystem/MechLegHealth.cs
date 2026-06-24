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
    [Tooltip("Continuous sparks effect that stays on the broken joint. If empty, a procedural one is created.")]
    public GameObject continuousSparksPrefab;
    [Tooltip("Die gelbe Kugel (Schwachstelle), die bei Zerstörung verschwinden soll")]
    public GameObject weakSpotMesh;

    [Header("Audio (Surgical)")]
    public AudioClip weakSpotHitSound;
    public AudioClip legDestroyedSound;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.6f;
    private AudioSource audioSource;
    
    [Header("References")]
    public MechBossHealth bossMainHealth;
    public LegIK legIK; // Referenz zur IK, um sie zu deaktivieren
    public Transform footTip; // Wichtig für BodyAdaptation

    private void Awake()
    {
        currentLegHealth = maxLegHealth;
    }

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        // Falls am Bein keine Source ist, suchen wir am Root (Boss)
        if (audioSource == null) audioSource = transform.root.GetComponentInChildren<AudioSource>();
    }

    public void TakeLegDamage(int amount)
    {
        if (isDestroyed) return;

        currentLegHealth -= amount;
        Debug.Log($"[LegHealth] Bein-HP: {currentLegHealth}");

        // Treffer-Sound auf Schwachstelle
        if (audioSource != null && weakSpotHitSound != null)
        {
            audioSource.PlayOneShot(weakSpotHitSound, audioVolume);
        }

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

        // Bein-Zerstörung Sound (wieder auf normale Lautstärke reduziert)
        if (legDestroyedSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(legDestroyedSound, audioVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(legDestroyedSound, transform.position, audioVolume);
            }
        }

        // 0. Schwachstelle ausblenden
        if (weakSpotMesh != null)
        {
            weakSpotMesh.SetActive(false);
        }

        // 1. IK deaktivieren
        if (legIK != null)
        {
            legIK.enabled = false;
        }

        // 2. Procedural System informieren
        Transform root = transform.root;
        StepManager stepManager = root.GetComponentInChildren<StepManager>();
        BodyAdaptation bodyAdapt = root.GetComponentInChildren<BodyAdaptation>();

        if (stepManager != null)
        {
            stepManager.ReportLegDestroyed(this.transform);
        }

        if (bodyAdapt != null && footTip != null)
        {
            bodyAdapt.ReportFootDestroyed(footTip);
        }

        // 3. Visuelles Feedback: Mesh "abtrennen"
        if (legMesh != null)
        {
            Rigidbody rb = legMesh.GetComponent<Rigidbody>();
            if (rb == null) rb = legMesh.AddComponent<Rigidbody>();
            
            Collider col = legMesh.GetComponent<Collider>();
            if (col == null) col = legMesh.AddComponent<BoxCollider>();
            
            legMesh.transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Vector3 forceDir = (legMesh.transform.position - root.position).normalized + Vector3.up;
            rb.AddForce(forceDir * 5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        // 4. Explosion spawnen
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 4.5. Kontinuierliche Funken aus dem kaputten Gelenk spawnen
        SpawnContinuousSparks();

        // 5. Boss informieren
        if (bossMainHealth != null)
        {
            bossMainHealth.ReportLegDestroyed();
        }
    }

    private void SpawnContinuousSparks()
    {
        Vector3 spawnPos = transform.position;
        Transform spawnParent = transform;

        // Das Knie-Gelenk ist vermutlich da, wo der Weakspot saß!
        if (weakSpotMesh != null)
        {
            spawnPos = weakSpotMesh.transform.position;
            spawnParent = weakSpotMesh.transform.parent != null ? weakSpotMesh.transform.parent : transform;
        }

        if (continuousSparksPrefab != null)
        {
            GameObject sparks = Instantiate(continuousSparksPrefab, spawnPos, Quaternion.identity, spawnParent);
        }
        else
        {
            // Procedural Sparks anpassen: dünner, kleiner, weiß und am Knie!
            GameObject sparksObj = new GameObject("ProceduralSparks");
            sparksObj.transform.SetParent(spawnParent);
            sparksObj.transform.position = spawnPos;

            ParticleSystem ps = sparksObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f); // Viel kleiner und dünner
            main.startColor = Color.white; // Weiße Funken
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 15f; // Weniger intensiv

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f; // Sehr kleiner Radius, damit sie exakt aus einem Punkt kommen

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f; // Etwas mehr Stretch für den "schnellen Funken"-Look
            
            Shader pShader = Shader.Find("Particles/Standard Unlit");
            if (pShader != null)
            {
                Material pMat = new Material(pShader);
                if (pMat.HasProperty("_EmissionColor"))
                {
                    pMat.SetColor("_EmissionColor", Color.white * 1.5f); // Leichtes Leuchten
                }
                renderer.material = pMat;
            }
        }
    }
}
