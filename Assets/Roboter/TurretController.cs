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
    [Tooltip("Maximale Drehgeschwindigkeit des Turms in Grad/Sekunde. 15-25 = schwerer Mech, 45+ = leichter Turm")]
    [SerializeField] private float maxYawSpeed = 20f;
    [Tooltip("Wie weich/träge der Turm beschleunigt und abbremst")]
    [SerializeField] private float yawSmoothTime = 0.4f;

    [Header("Cannon Pitch Speed")]
    [SerializeField] private float pitchSpeed = 45f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 25f;

    [Header("Audio (Surgical)")]
    public AudioSource audioSource; // EXPLICIT
    [Tooltip("Sound-Loop für die Drehung des Oberkörpers")]
    public AudioClip turretRotateLoop;
    [SerializeField, Range(0f, 1f)] private float turretVolume = 0.3f;

    private float currentYawVelocity;
    private float currentPitchVel;

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
            HandleYawRotation(target.position);
            HandlePitchRotation(target.position);
        }
        else if (usePositionTarget)
        {
            HandleYawRotation(positionTarget);
            HandlePitchRotation(positionTarget);
        }
        else
        {
            ResetToNeutral();
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

            float newPitch = Mathf.SmoothDampAngle(currentPitch, 0f, ref currentPitchVel, 1f / pitchSpeed);
            cannonPitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
        }
    }

    void HandleYawRotation(Vector3 targetPos)
    {
        Vector3 localTargetPos = turretYawPivot.parent.InverseTransformPoint(targetPos);
        localTargetPos.y = 0f;

        if (localTargetPos.magnitude < 0.1f) return;

        float targetAngle = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;

        float currentAngle = turretYawPivot.localEulerAngles.y;
        if (currentAngle > 180f) currentAngle -= 360f;

        float newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref currentYawVelocity, yawSmoothTime, maxYawSpeed);
        turretYawPivot.localRotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    void HandlePitchRotation(Vector3 targetPos)
    {
        if (cannonPitchPivot == null) return;

        Vector3 targetDirectionWorld = targetPos - cannonPitchPivot.position;
        float distance = targetDirectionWorld.magnitude;

        float targetPitchAngle = -Mathf.Atan2(targetDirectionWorld.y, distance) * Mathf.Rad2Deg;
        targetPitchAngle = Mathf.Clamp(targetPitchAngle, minPitch, maxPitch);

        float currentPitchAngle = cannonPitchPivot.localEulerAngles.x;
        if (currentPitchAngle > 180f) currentPitchAngle -= 360f;

        float newPitch = Mathf.SmoothDampAngle(currentPitchAngle, targetPitchAngle, ref currentPitchVel, 1f / pitchSpeed);
        cannonPitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
    }
}
