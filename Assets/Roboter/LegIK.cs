using UnityEngine;

public class LegIK : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Gelenk-Zuweisungen")]
    public Transform hip;
    public Transform upperLeg;
    public Transform lowerLeg;
    public Transform footTip;
    public Transform target;

    [Header("Berechnete Längen")]
    private float lengthFemur;
    private float lengthTibia;

    private Quaternion hipInitRot;
    private Quaternion upperInitRot;

    void Start()
    {
        if (hip != null && upperLeg != null && footTip != null)
        {
            lengthFemur = Vector3.Distance(hip.position, upperLeg.position);
            lengthTibia = Vector3.Distance(upperLeg.position, footTip.position);

            hipInitRot = hip.localRotation;
            upperInitRot = upperLeg.localRotation;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        SolveIK();
    }

    private void SolveIK()
    {
        hip.localRotation = hipInitRot;
        upperLeg.localRotation = upperInitRot;

        Vector3 targetDirYaw = target.position - hip.position;
        targetDirYaw.y = 0f;
        if (targetDirYaw.sqrMagnitude > 0.001f)
        {
            hip.rotation = Quaternion.LookRotation(targetDirYaw);
        }

        Vector3 toTarget = target.position - hip.position;
        float targetDist = toTarget.magnitude;
        float maxLength = lengthFemur + lengthTibia;

        // KRITISCHER FEHLER-LOG: Wenn der Fuß den Boden nicht erreichen kann
        if (enableDebugLogs && targetDist >= maxLength - 0.005f)
        {
            Debug.LogWarning($"[LegIK] {gameObject.name} ÜBERDEHNT! Distanz zum Ziel: {targetDist}, Max. Bein-Länge: {maxLength}");
        }

        targetDist = Mathf.Clamp(targetDist, 0.001f, maxLength - 0.001f);

        float cosHip = (lengthFemur * lengthFemur + targetDist * targetDist - lengthTibia * lengthTibia) / (2 * lengthFemur * targetDist);
        cosHip = Mathf.Clamp(cosHip, -1f, 1f);
        float hipAngle = Mathf.Acos(cosHip) * Mathf.Rad2Deg;

        Vector3 hingeAxis = hip.right;
        Vector3 kneeDirection = Quaternion.AngleAxis(-hipAngle, hingeAxis) * toTarget.normalized;
        Vector3 targetKneePos = hip.position + kneeDirection * lengthFemur;

        Vector3 currentFemurDir = upperLeg.position - hip.position;
        Vector3 targetFemurDir = targetKneePos - hip.position;
        Quaternion hipPitch = Quaternion.FromToRotation(currentFemurDir, targetFemurDir);
        hip.rotation = hipPitch * hip.rotation;

        Vector3 currentTibiaDir = footTip.position - upperLeg.position;
        Vector3 targetTibiaDir = target.position - upperLeg.position;
        Quaternion kneeBend = Quaternion.FromToRotation(currentTibiaDir, targetTibiaDir);
        upperLeg.rotation = kneeBend * upperLeg.rotation;
    }

#if UNITY_EDITOR
    [Header("Presentation Visuals")]
    [Tooltip("Aktivieren, um Bones, Gelenke und Ziel-Vektoren in der Scene View zu sehen")]
    public bool showPresentationGizmos = false;

    private void OnDrawGizmos()
    {
        if (!showPresentationGizmos) return;

        if (hip != null && upperLeg != null && footTip != null && target != null)
        {
            // Bones (Oberschenkel & Unterschenkel)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(hip.position, upperLeg.position);
            Gizmos.DrawLine(upperLeg.position, footTip.position);

            // Gelenke (Kugeln)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hip.position, 0.15f);
            Gizmos.DrawWireSphere(upperLeg.position, 0.15f);
            Gizmos.DrawWireSphere(footTip.position, 0.15f);

            // Hypotenuse (Hüfte zu IK Target)
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawLine(hip.position, target.position);
            
            // Ziel-Würfel
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(target.position, Vector3.one * 0.2f);

            // Beschriftungen
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(hip.position + Vector3.up * 0.3f, "Hip");
            UnityEditor.Handles.Label(upperLeg.position + Vector3.up * 0.3f, "Knee Joint (IK)");
            UnityEditor.Handles.Label(target.position + Vector3.down * 0.3f, "IK Target");
        }
    }
#endif
}