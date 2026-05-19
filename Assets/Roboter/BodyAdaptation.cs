using UnityEngine;
using System.Collections.Generic;

public class BodyAdaptation : MonoBehaviour
{
    [Header("Leg References")]
    [SerializeField] private List<Transform> footTips = new List<Transform>();

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

    [Header("Zwingende Zuweisung!")]
    [Tooltip("Ziehe hier das Objekt rein, auf dem der NavMeshAgent liegt (Robot_Root)")]
    public Transform rootTransform; // Die Lösung des Problems!

    private float verticalOffset;
    private float verticalVelocity;
    private StepManager stepManager;
    private float smoothedBaseY;
    private float baseYVel;

    void Start()
    {
        // rootTransform = transform.root; <--- GELOESCHT! Damit er nicht mehr das "Player" Objekt nimmt.

        FindFootTips();
        smoothedBaseY = transform.position.y;

        if (rootTransform != null)
        {
            stepManager = rootTransform.GetComponentInChildren<StepManager>();
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

        if (enableHeightAdapt) AdaptBodyHeight();
        if (enableRotationAdapt) AdaptBodyRotation();
    }

    void UpdateSpringPhysics()
    {
        float force = -springStiffness * verticalOffset - damping * verticalVelocity;
        verticalVelocity += force * Time.deltaTime;
        verticalOffset += verticalVelocity * Time.deltaTime;
    }

    void AdaptBodyHeight()
    {
        Vector3 avgFootPos = GetAverageFootPos(new int[] { 0, 1, 2, 3, 4, 5 });
        float targetBodyY = avgFootPos.y + baseBodyHeight;
        smoothedBaseY = Mathf.SmoothDamp(smoothedBaseY, targetBodyY, ref baseYVel, heightSmoothTime);

        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, smoothedBaseY + verticalOffset, currentPos.z);
    }

    void AdaptBodyRotation()
    {
        // Sicherstellen, dass das Skript nicht abstürzt, falls das Root-Objekt fehlt
        if (rootTransform == null) return;

        // 1. Die echte 3D-Position der Füße abfragen
        Vector3 frontFeet = GetAverageFootPos(new int[] { 0, 3 });
        Vector3 backFeet = GetAverageFootPos(new int[] { 2, 5 });
        Vector3 leftFeet = GetAverageFootPos(new int[] { 0, 1, 2 });
        Vector3 rightFeet = GetAverageFootPos(new int[] { 3, 4, 5 });

        // 2. Vektoren kreuzen, um die exakte Hangneigung zu finden
        Vector3 forwardDir = frontFeet - backFeet;
        Vector3 rightDir = rightFeet - leftFeet;

        Vector3 terrainNormal = Vector3.Cross(forwardDir, rightDir).normalized;
        if (terrainNormal == Vector3.zero) terrainNormal = Vector3.up;

        // 3. Den Körper auf diese Hangneigung ausrichten
        Vector3 projectedForward = Vector3.ProjectOnPlane(rootTransform.forward, terrainNormal).normalized;
        Quaternion terrainRotation = Quaternion.LookRotation(projectedForward, terrainNormal);

        // 4. Extreme Winkel abschneiden (damit er nicht umkippt)
        Vector3 euler = terrainRotation.eulerAngles;
        float pitch = NormalizeAngle(euler.x);
        float roll = NormalizeAngle(euler.z);

        pitch = Mathf.Clamp(pitch, -maxBodyTilt, maxBodyTilt);
        roll = Mathf.Clamp(roll, -maxBodyTilt, maxBodyTilt);

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
            if (idx >= 0 && idx < footTips.Count && footTips[idx] != null)
            {
                sum += footTips[idx].position; // Hier nehmen wir jetzt XYZ, nicht nur Y!
                count++;
            }
        }
        return count > 0 ? sum / count : Vector3.zero;
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