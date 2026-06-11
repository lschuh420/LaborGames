using UnityEngine;
using System.Collections.Generic;

public class BodyAdaptation : MonoBehaviour
{
    [Header("Leg References")]
    [SerializeField] private List<Transform> footTips = new List<Transform>();
    private bool[] destroyedFeet; // NEU: Tracker für zerstörte Beine

    [Header("Body Height (Anpassen!)")]
    [SerializeField] private float baseBodyHeight = 0.6f;
    [SerializeField] private float heightSmoothTime = 0.15f;
    [SerializeField] private bool enableHeightAdapt = true;

    [Header("Body Tilt (True Math)")]
    [Tooltip("Wie weit darf er sich maximal neigen? (z.B. 25 Grad)")]
    [SerializeField] private float maxBodyTilt = 25f;
    [SerializeField] private float tiltSmoothTime = 0.15f;
    [SerializeField] private bool enableRotationAdapt = true;

    [Header("Impact Dynamics")]
    [SerializeField] private float impactStrength = 0.15f;
    [SerializeField] private float springStiffness = 12f;
    [SerializeField] private float damping = 5f;

    [Header("Aim Assist (Body Leaning)")]
    [Tooltip("Wie viel Grad darf der Körper zusätzlich neigen, um beim Zielen zu helfen?")]
    [SerializeField] private float maxAimAssistTilt = 12f;
    [Tooltip("Wie schnell reagiert die Neigung auf das Ziel?")]
    [SerializeField] private float aimAssistSmoothTime = 0.3f;

    [Header("Stun / Collapse")]
    [Tooltip("Auf wie viel Prozent sinkt der Körper im Stun? (0.2 = 20%)")]
    [SerializeField] private float stunHeightFactor = 0.25f;
    [SerializeField] private float stunSmoothTime = 0.8f; // Langsameres Einsacken

    [Header("Zwingende Zuweisung!")]
    [Tooltip("Ziehe hier das Objekt rein, auf dem der NavMeshAgent liegt (Robot_Root)")]
    public Transform rootTransform;

    private float verticalOffset;
    private float verticalVelocity;
    private StepManager stepManager;
    private MechBossHealth bossHealth;
    private TurretController turretController;
    private float smoothedBaseY;
    private float baseYVel;
    private float currentStunLerp = 1f;
    private float currentAimAssistPitch = 0f;
    private float aimAssistPitchVel;

    // NEU: Methode um ein Bein für die Anpassung zu deaktivieren
    public void ReportFootDestroyed(Transform footTip)
    {
        for (int i = 0; i < footTips.Count; i++)
        {
            if (footTips[i] == footTip)
            {
                destroyedFeet[i] = true;
                Debug.Log($"<color=red>[BodyAdaptation] Fuß {i} wird ignoriert.</color>");
                break;
            }
        }
    }

    void Start()
    {
        FindFootTips();
        destroyedFeet = new bool[footTips.Count]; // Initialisieren
        smoothedBaseY = transform.position.y;

        if (rootTransform != null)
        {
            stepManager = rootTransform.GetComponentInChildren<StepManager>();
            bossHealth = rootTransform.GetComponentInChildren<MechBossHealth>();
            turretController = rootTransform.GetComponentInChildren<TurretController>();
            if (stepManager != null) stepManager.OnStepLanded += ApplyImpact;
        }
        else
        {
            Debug.LogError("[BodyAdaptation] ACHTUNG: Du musst den 'Robot_Root' im Inspector zuweisen, sonst dreht sich der Körper nicht!");
        }
    }

    void OnDestroy()
    {
        if (stepManager != null) stepManager.OnStepLanded -= ApplyImpact;
    }

    void ApplyImpact(float power)
    {
        verticalVelocity -= power * impactStrength;
    }

    void LateUpdate()
    {
        if (footTips.Count < 3) return;

        UpdateSpringPhysics();
        UpdateStunLogic();
        UpdateAimAssistLogic();

        if (enableHeightAdapt) AdaptBodyHeight();
        if (enableRotationAdapt) AdaptBodyRotation();
    }

    void UpdateSpringPhysics()
    {
        float force = -springStiffness * verticalOffset - damping * verticalVelocity;
        verticalVelocity += force * Time.deltaTime;
        verticalOffset += verticalVelocity * Time.deltaTime;
    }

    void UpdateStunLogic()
    {
        float targetStun = (bossHealth != null && bossHealth.IsStunned) ? stunHeightFactor : 1f;

        // PERMANENTE REDUKTION: Wenn Beine fehlen, sinkt die Basis-Höhe dauerhaft
        if (bossHealth != null)
        {
            // Pro zerstörtem Bein sinkt er um 10% seiner Resthöhe (Arc Raiders Style)
            float damageMalus = 1f - (bossHealth.DestroyedLegsCount * 0.1f);
            targetStun *= Mathf.Max(0.4f, damageMalus); 
        }

        // Wir nutzen eine weichere Annäherung für den Stun
        currentStunLerp = Mathf.Lerp(currentStunLerp, targetStun, Time.deltaTime / (stunSmoothTime + 0.001f)); 
    }

    void UpdateAimAssistLogic()
    {
        if (turretController == null || turretController.target == null || bossHealth.IsStunned)
        {
            currentAimAssistPitch = Mathf.SmoothDamp(currentAimAssistPitch, 0f, ref aimAssistPitchVel, aimAssistSmoothTime);
            return;
        }

        // Winkel zum Ziel berechnen
        Vector3 dir = turretController.target.position - transform.position;
        float horizontalDist = new Vector2(dir.x, dir.z).magnitude;
        float targetPitch = -Mathf.Atan2(dir.y, horizontalDist) * Mathf.Rad2Deg;

        // Wir neigen den Körper nur, wenn das Ziel außerhalb der normalen Kanonen-Range liegt 
        // oder um das Zielen allgemein zu unterstützen.
        float desiredTilt = Mathf.Clamp(targetPitch, -maxAimAssistTilt, maxAimAssistTilt);
        
        currentAimAssistPitch = Mathf.SmoothDamp(currentAimAssistPitch, desiredTilt, ref aimAssistPitchVel, aimAssistSmoothTime);
    }

    void AdaptBodyHeight()
    {
        // Wir nehmen alle verfügbaren Beine
        Vector3 avgFootPos = GetAverageFootPos(new int[] { 0, 1, 2, 3, 4, 5 });
        
        // Der Mech sinkt im Stun ab
        float targetBodyY = avgFootPos.y + (baseBodyHeight * currentStunLerp);
        
        smoothedBaseY = Mathf.SmoothDamp(smoothedBaseY, targetBodyY, ref baseYVel, heightSmoothTime);

        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, smoothedBaseY + verticalOffset, currentPos.z);
    }

    void AdaptBodyRotation()
    {
        if (rootTransform == null) return;

        // 1. Die echte 3D-Position der Füße abfragen (Filtert automatisch zerstörte aus)
        Vector3 frontFeet = GetAverageFootPos(new int[] { 0, 3 });
        Vector3 backFeet = GetAverageFootPos(new int[] { 2, 5 });
        Vector3 leftFeet = GetAverageFootPos(new int[] { 0, 1, 2 });
        Vector3 rightFeet = GetAverageFootPos(new int[] { 3, 4, 5 });

        // Wenn auf einer Seite gar kein Bein mehr ist, sinkt diese Seite massiv ab (Procedural Fall)
        
        Vector3 forwardDir = frontFeet - backFeet;
        Vector3 rightDir = rightFeet - leftFeet;

        Vector3 terrainNormal = Vector3.Cross(forwardDir, rightDir).normalized;
        if (terrainNormal == Vector3.zero) terrainNormal = Vector3.up;

        Vector3 projectedForward = Vector3.ProjectOnPlane(rootTransform.forward, terrainNormal).normalized;
        Quaternion terrainRotation = Quaternion.LookRotation(projectedForward, terrainNormal);

        Vector3 euler = terrainRotation.eulerAngles;
        float pitch = NormalizeAngle(euler.x);
        float roll = NormalizeAngle(euler.z);

        // AIM ASSIST: Hier addieren wir die dynamische Körper-Neigung
        pitch += currentAimAssistPitch;

        pitch = Mathf.Clamp(pitch, -maxBodyTilt * 2f, maxBodyTilt * 2f);
        roll = Mathf.Clamp(roll, -maxBodyTilt * 1.5f, maxBodyTilt * 1.5f);

        // 5. Rotation weich anwenden
        Quaternion targetRot = Quaternion.Euler(pitch, rootTransform.eulerAngles.y, roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime / (tiltSmoothTime + 0.01f));
    }

Vector3 GetAverageFootPos(int[] indices)
{
Vector3 sum = Vector3.zero;
int count = 0;
for (int i = 0; i < indices.Length; i++)
{
    int idx = indices[i];
    if (idx >= 0 && idx < footTips.Count && footTips[idx] != null && !destroyedFeet[idx]) // NEU: destroyedFeet Check
    {
        sum += footTips[idx].position;
        count++;
    }
}

// Fallback: Wenn kein Bein in der Gruppe aktiv ist, nehmen wir die Root-Position (Mech sinkt massiv ab!)
if (count == 0) return rootTransform.position - Vector3.up * 2.5f; 

return sum / count;
}

    float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }

    void FindFootTips()
    {
        footTips.Clear();
        string[] legPrefixes = { "FL", "ML", "BL", "FR", "MR", "BR" };
        Transform legsContainer = transform.Find("Legs_Container");

        if (legsContainer == null) return;

        for (int i = 0; i < legPrefixes.Length; i++)
        {
            Transform legRoot = legsContainer.Find($"Leg_{legPrefixes[i]}_Root");
            if (legRoot == null) continue;

            foreach (Transform child in legRoot.GetComponentsInChildren<Transform>())
            {
                if (child.name.Contains("Foot_") && child.name.Contains("_Tip"))
                {
                    footTips.Add(child);
                    break;
                }
            }
        }
    }
}
