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

    // NEU: Für die Suche nach Koordinaten (Gelber Modus)
    [HideInInspector] public Vector3 positionTarget;
    [HideInInspector] public bool usePositionTarget;

    [Header("Turret Rotation Speed")]
    [Tooltip("Maximale Drehgeschwindigkeit des Turms in Grad/Sekunde. 40 = Kompetent, 20 = Träge")]
    [SerializeField] private float maxYawSpeed = 40f;
    [Tooltip("Wie weich/träge der Turm beschleunigt und abbremst")]
    [SerializeField] private float yawSmoothTime = 0.2f;

    [Header("Cannon Pitch Speed")]
    [SerializeField] private float pitchSpeed = 45f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 45f;

    [Header("Precision Settings")]
    [Tooltip("Winkel-Toleranz: Ab wie viel Grad Abweichung gilt das Ziel als 'anvisiert'?")]
    [SerializeField] private float aimTolerance = 2f;
    [Tooltip("Höhen-Offset auf das Ziel. 1.0 = Brusthöhe statt Füße")]
    [SerializeField] private float aimHeightOffset = 1.0f;

    public bool IsAimedAtTarget { get; private set; }

    // Exakter Punkt, auf den der Turm zielt -> die Waffen richten die Kugeln genau hierauf aus
    public Vector3 AimPoint { get; private set; }
    public bool HasAimPoint { get; private set; }

    [Header("Audio (Surgical)")]
    public AudioSource audioSource; // EXPLICIT
    [Tooltip("Sound-Loop für die Drehung des Oberkörpers")]
    public AudioClip turretRotateLoop;
    [SerializeField, Range(0f, 1f)] private float turretVolume = 0.3f;

    private float currentYawVelocity;
    private float currentPitchVel;
    private float currentYawDiff;
    private float currentPitchDiff;

    void Start()
    {
        if (turretYawPivot == null)
        {
            Debug.LogError("[TurretController] Kein Yaw Pivot zugewiesen!");
            enabled = false;
            return;
        }

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        
        if (audioSource != null)
        {
            // Sicherstellen, dass der Sound 3D ist und von der Position des Roboters kommt
            audioSource.spatialBlend = 1f; 
            
            if (turretRotateLoop != null)
            {
                audioSource.clip = turretRotateLoop;
                audioSource.loop = true;
                audioSource.Play();
                audioSource.volume = 0;
            }
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Auf Brusthöhe zielen statt auf die Füße
            Vector3 aimPos = target.position + Vector3.up * aimHeightOffset;
            AimPoint = aimPos;
            HasAimPoint = true;

            HandleYawRotation(aimPos);
            HandlePitchRotation(aimPos);

            // Nur wenn BEIDE Achsen im Toleranzbereich sind, gilt das Ziel als anvisiert
            IsAimedAtTarget = (currentYawDiff <= aimTolerance) && (currentPitchDiff <= aimTolerance);
        }
        else if (usePositionTarget)
        {
            AimPoint = positionTarget;
            HasAimPoint = true;

            HandleYawRotation(positionTarget);
            HandlePitchRotation(positionTarget);
            IsAimedAtTarget = (currentYawDiff <= aimTolerance) && (currentPitchDiff <= aimTolerance);
        }
        else
        {
            ResetToNeutral();
            IsAimedAtTarget = false;
            HasAimPoint = false;
        }

        UpdateRotationAudio();
    }

    void UpdateRotationAudio()
    {
        if (audioSource == null || turretRotateLoop == null) return;

        float totalVel = Mathf.Abs(currentYawVelocity) + Mathf.Abs(currentPitchVel);
        float intensity = Mathf.Clamp01(totalVel / maxYawSpeed);

        audioSource.volume = intensity * turretVolume;
        audioSource.pitch = 0.85f + (intensity * 0.3f);
    }

    void ResetToNeutral()
    {
        float currentYaw = turretYawPivot.localEulerAngles.y;
        if (currentYaw > 180f) currentYaw -= 360f;

        float newYaw = Mathf.SmoothDampAngle(currentYaw, 0f, ref currentYawVelocity, yawSmoothTime, maxYawSpeed);
        turretYawPivot.localRotation = Quaternion.Euler(0f, newYaw, 0f);

        if (cannonPitchPivot != null)
        {
            float currentPitch = cannonPitchPivot.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;

            float newPitch = Mathf.SmoothDampAngle(currentPitch, 0f, ref currentPitchVel, 0.2f);
            cannonPitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
        }
    }

    void HandleYawRotation(Vector3 targetPos)
    {
        // 1. Ziel-Position in den lokalen Raum des Yaw-Pivot-Elternteils transformieren
        // Dies berücksichtigt automatisch JEDE Neigung des Körpers (Terrain + Aim Assist)
        Vector3 localTargetPos = turretYawPivot.parent.InverseTransformPoint(targetPos);
        
        // Wir betrachten nur die horizontale Ebene für Yaw
        float targetAngle = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;

        float currentAngle = turretYawPivot.localEulerAngles.y;
        if (currentAngle > 180f) currentAngle -= 360f;

        float newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref currentYawVelocity, yawSmoothTime, maxYawSpeed);
        turretYawPivot.localRotation = Quaternion.Euler(0f, newAngle, 0f);

        currentYawDiff = Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle));
    }

    void HandlePitchRotation(Vector3 targetPos)
    {
        if (cannonPitchPivot == null) return;

        // 1. Ziel-Position in den lokalen Raum des Pitch-Pivot-Elternteils (der bereits gedrehte Yaw-Turm!)
        Vector3 localTargetPos = cannonPitchPivot.parent.InverseTransformPoint(targetPos);
        
        // 2. Erforderlichen lokalen Pitch-Winkel berechnen
        // localTargetPos.z ist "vorne", localTargetPos.y ist "oben" relativ zum Turm
        float targetPitchAngle = -Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
        
        // 3. Mechanische Limits anwenden
        targetPitchAngle = Mathf.Clamp(targetPitchAngle, minPitch, maxPitch);

        float currentPitchAngle = cannonPitchPivot.localEulerAngles.x;
        if (currentPitchAngle > 180f) currentPitchAngle -= 360f;

        // 4. Weich ansteuern
        float newPitch = Mathf.SmoothDampAngle(currentPitchAngle, targetPitchAngle, ref currentPitchVel, 0.12f);
        cannonPitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
        
        currentPitchDiff = Mathf.Abs(Mathf.DeltaAngle(newPitch, targetPitchAngle));
    }
}
