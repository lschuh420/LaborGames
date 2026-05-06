using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Hierarchy Links")]
    [Tooltip("Der Pivot, der das gesamte Oberteil nach links/rechts dreht (Y-Achse)")]
    [SerializeField] private Transform turretYawPivot;
    [Tooltip("Der Pivot, der nur die Kanonenrohre hoch/runter neigt (X-Achse)")]
    [SerializeField] private Transform cannonPitchPivot;

    [Header("Targeting")]
    [Tooltip("Der Transform des Ziels (wird vom Behavior Tree gesetzt)")]
    public Transform target;

    [Header("Turret Rotation Speed")]
    [Tooltip("Maximale Drehgeschwindigkeit des Turms in Grad/Sekunde. 15-25 = schwerer Mech, 45+ = leichter Turm")]
    [SerializeField] private float maxYawSpeed = 20f;
    [Tooltip("Wie weich/träge der Turm beschleunigt und abbremst")]
    [SerializeField] private float yawSmoothTime = 0.4f;

    [Header("Cannon Pitch Speed")]
    [SerializeField] private float pitchSpeed = 45f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 25f;

    private float currentYawVelocity;  // intern für SmoothDampAngle
    private float currentPitchVel;

    void Start()
    {
        if (turretYawPivot == null)
        {
            Debug.LogError("[TurretController] Kein Yaw Pivot zugewiesen!");
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleYawRotation();
        HandlePitchRotation();
    }

    void HandleYawRotation()
    {
        // 1. Zielposition in den lokalen Raum des Rumpfes transformieren
        Vector3 localTargetPos = turretYawPivot.parent.InverseTransformPoint(target.position);
        localTargetPos.y = 0f;

        if (localTargetPos.magnitude < 0.1f) return;

        // 2. Gewünschten Zielwinkel berechnen (in Grad)
        float targetAngle = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;

        // 3. Aktuellen Winkel auslesen und normalisieren (-180 bis 180)
        float currentAngle = turretYawPivot.localEulerAngles.y;
        if (currentAngle > 180f) currentAngle -= 360f;

        // 4. SmoothDampAngle: Dreht weich zum Ziel, ABER begrenzt durch maxYawSpeed
        float newAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref currentYawVelocity,
            yawSmoothTime,
            maxYawSpeed  // <-- Das ist die harte Geschwindigkeitsgrenze in Grad/Sekunde
        );

        turretYawPivot.localRotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    void HandlePitchRotation()
    {
        if (cannonPitchPivot == null) return;

        Vector3 targetDirectionWorld = target.position - cannonPitchPivot.position;
        float distance = targetDirectionWorld.magnitude;

        float targetPitchAngle = -Mathf.Atan2(targetDirectionWorld.y, distance) * Mathf.Rad2Deg;
        targetPitchAngle = Mathf.Clamp(targetPitchAngle, minPitch, maxPitch);

        float currentPitchAngle = cannonPitchPivot.localEulerAngles.x;
        if (currentPitchAngle > 180f) currentPitchAngle -= 360f;

        float newPitch = Mathf.SmoothDampAngle(currentPitchAngle, targetPitchAngle, ref currentPitchVel, 1f / pitchSpeed);
        cannonPitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
    }
}