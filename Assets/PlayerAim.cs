using UnityEngine;
using Unity.Cinemachine;

public class PlayerAim : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera normalCam;
    [SerializeField] private CinemachineCamera aimCam;

    [Header("Priorities")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 5;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private int aimLayerIndex = 1;
    [SerializeField] private float aimBlendSpeed = 10f;

    [Header("Weapon Check")]
    [SerializeField] private PlayerShooting playerShooting;

    [Header("Aim Rotation")]
    [SerializeField] private float aimRotationSpeed = 15f;

    public bool IsAiming  { get; private set; }
    public bool HasWeapon { get; private set; }

    void Update()
    {
        // ---- Weapon equipped check ----
        HasWeapon = playerShooting != null
                    && playerShooting.equippedWeapons != null
                    && playerShooting.activeSlot < playerShooting.equippedWeapons.Length
                    && playerShooting.equippedWeapons[playerShooting.activeSlot] != null;

        // Aiming = right-click + weapon equipped (drives camera zoom & body rotation)
        IsAiming = Input.GetMouseButton(1) && HasWeapon;

        // ---- Camera priority swap ----
        if (IsAiming)
        {
            aimCam.Priority    = activePriority;
            normalCam.Priority = inactivePriority;
        }
        else
        {
            aimCam.Priority    = inactivePriority;
            normalCam.Priority = activePriority;
        }

        // ---- Animation: upper body layer + aim pose toggle ----
        if (animator != null)
        {
            // Layer weight = ON whenever a weapon is held (Pistol Idle plays by default)
            float target  = HasWeapon ? 1f : 0f;
            float current = animator.GetLayerWeight(aimLayerIndex);
            animator.SetLayerWeight(
                aimLayerIndex,
                Mathf.Lerp(current, target, Time.deltaTime * aimBlendSpeed)
            );

            // Switch between Pistol Idle (relaxed) and Pistol Aim (raised) states
            animator.SetBool("IsAiming", IsAiming);
        }
    }

    void LateUpdate()
    {
        // While aiming, force the character to face the camera's forward direction.
        // Runs after ThirdPersonController's rotation so this override sticks.
        if (IsAiming && Camera.main != null)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0f; // Keep character upright

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * aimRotationSpeed
                );
            }
        }
    }
}