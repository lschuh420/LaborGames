using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // Robot_Root
    public Transform pivot;           // CameraPivot (child of this rig)

    [Header("Follow")]
    public Vector3 followOffset = new Vector3(0f, 0.8f, 0f);
    public float positionSmoothTime = 0.10f;

    [Header("Orbit")]
    public float mouseSensitivity = 140f;
    public float minPitch = -25f;
    public float maxPitch = 55f;
    public float rotationSmoothTime = 0.06f;

    [Header("Zoom")]
    public float distance = 6f;
    public float minDistance = 3.5f;
    public float maxDistance = 9f;
    public float zoomSpeed = 2.0f;

    private float yaw;
    private float pitch;

    private Vector3 posVel;
    private float yawVel;
    private float pitchVel;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[ThirdPersonOrbitCamera] target not set.");
            enabled = false;
            return;
        }

        if (pivot == null)
        {
            Debug.LogError("[ThirdPersonOrbitCamera] pivot not set.");
            enabled = false;
            return;
        }

        // Start with current view
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = pivot.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mouse look
        Vector2 delta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
        float dt = Time.deltaTime;

        yaw += delta.x * mouseSensitivity * dt;
        pitch -= delta.y * mouseSensitivity * dt;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom (Mouse wheel)
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 0.01f * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        // Follow target smoothly
        Vector3 desiredPos = target.position + target.TransformVector(followOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmoothTime);

        // Smooth rotation (yaw on rig, pitch on pivot)
        float smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, yaw, ref yawVel, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);

        float currentPitch = pivot.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        float smoothPitch = Mathf.SmoothDampAngle(currentPitch, pitch, ref pitchVel, rotationSmoothTime);
        pivot.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);

        // Set camera distance (camera is assumed child of pivot)
        Camera cam = Camera.main;
        if (cam != null && cam.transform.parent == pivot)
        {
            cam.transform.localPosition = new Vector3(0f, 0f, -distance);
        }
    }
}