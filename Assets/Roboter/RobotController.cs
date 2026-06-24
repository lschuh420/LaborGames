using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour
{
    [Header("Robot Movement (Matched to AI)")]
    public float moveSpeed = 3.5f; // Genau der Wert (maxSpeed) aus MechDriver.cs
    public float turnSpeed = 120f; // Standard NavMeshAgent Angular Speed

    [Header("Smoothing")]
    public float moveAcceleration = 8.0f; // Standard NavMeshAgent Acceleration
    public float moveDeceleration = 5.0f; // Exakt die brakingForce aus MechDriver.cs
    public float turnAcceleration = 8.0f;
    public float turnDeceleration = 5.0f;

    [Header("Turret (Arrow Keys)")]
    public Transform turretRoot;
    public float turretYawSpeed = 60f;
    public float turretPitchSpeed = 40f;
    public float maxPitch = 45f;
    public float minPitch = -15f;

    [Header("Camera Reference")]
    [Tooltip("Die Main Camera (falls zugewiesen, bewegt sich der Mech relativ zur Blickrichtung der Kamera)")]
    public Transform mainCamera;

    // robot smoothing
    private float currentMove;

    // turret angles (LOCAL!)
    private float turretYaw;
    private float turretPitch;

    void Start()
    {
        // Startwerte aus der aktuellen Pose übernehmen
        if (turretRoot != null)
        {
            Vector3 e = turretRoot.localEulerAngles;
            turretYaw = NormalizeAngle(e.y);
            turretPitch = NormalizeAngle(e.x);
        }
    }

    void Update()
    {
        HandleRobotMovement();
        HandleTurretRotation();
    }

    void HandleRobotMovement()
    {
        // W/S = vor/zurück, A/D = links/rechts
        float targetZ = Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;
        float targetX = Keyboard.current.dKey.isPressed ? 1f : Keyboard.current.aKey.isPressed ? -1f : 0f;

        Vector3 inputDir = new Vector3(targetX, 0f, targetZ).normalized;

        if (inputDir.magnitude > 0.01f)
        {
            // Kamera-relative Richtung berechnen
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            
            if (mainCamera != null)
            {
                targetAngle += mainCamera.eulerAngles.y;
            }

            // Roboter-Körper geschmeidig rotieren
            float smoothAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime * 0.1f);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // Beschleunigen
            currentMove = Mathf.Lerp(currentMove, 1f, moveAcceleration * Time.deltaTime);
        }
        else
        {
            // Abbremsen
            currentMove = Mathf.Lerp(currentMove, 0f, moveDeceleration * Time.deltaTime);
        }

        // Bewege den Mech vorwärts in die Richtung, in die er gerade schaut
        transform.Translate(Vector3.forward * currentMove * moveSpeed * Time.deltaTime, Space.Self);
    }

    void HandleTurretRotation()
    {
        if (turretRoot == null) return;

        float yawInput =
            Keyboard.current.rightArrowKey.isPressed ? 1f :
            Keyboard.current.leftArrowKey.isPressed ? -1f : 0f;

        float pitchInput =
            Keyboard.current.upArrowKey.isPressed ? 1f :
            Keyboard.current.downArrowKey.isPressed ? -1f : 0f;

        turretYaw += yawInput * turretYawSpeed * Time.deltaTime;
        turretPitch = Mathf.Clamp(turretPitch + pitchInput * turretPitchSpeed * Time.deltaTime, minPitch, maxPitch);

        turretRoot.localRotation = Quaternion.Euler(turretPitch, turretYaw, 0f);
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }
}