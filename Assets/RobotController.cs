using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour
{
    [Header("Robot Movement")]
    public float moveSpeed = 1.45f;
    public float turnSpeed = 32f;

    [Header("Smoothing")]
    public float moveAcceleration = 4.0f;
    public float moveDeceleration = 5.5f;
    public float turnAcceleration = 5.0f;
    public float turnDeceleration = 6.5f;

    [Header("Turret (Arrow Keys)")]
    public Transform turretRoot;
    public float turretYawSpeed = 120f;
    public float turretPitchSpeed = 80f;
    public float maxPitch = 45f;
    public float minPitch = -15f;

    // robot smoothing
    private float currentMove;
    private float currentTurn;

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
        // W/S = vor/zurück
        float targetMove =
            Keyboard.current.wKey.isPressed ? 1f :
            Keyboard.current.sKey.isPressed ? -1f : 0f;

        // A/D = Robot drehen
        float targetTurn =
            Keyboard.current.dKey.isPressed ? 1f :
            Keyboard.current.aKey.isPressed ? -1f : 0f;

        float moveLerp = (Mathf.Abs(targetMove) > 0.01f ? moveAcceleration : moveDeceleration) * Time.deltaTime;
        float turnLerp = (Mathf.Abs(targetTurn) > 0.01f ? turnAcceleration : turnDeceleration) * Time.deltaTime;

        currentMove = Mathf.Lerp(currentMove, targetMove, moveLerp);
        currentTurn = Mathf.Lerp(currentTurn, targetTurn, turnLerp);

        transform.Translate(Vector3.forward * currentMove * moveSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(Vector3.up * currentTurn * turnSpeed * Time.deltaTime, Space.Self);
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