using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;

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

    [Header("Movement References")]
    [SerializeField] private ThirdPersonController controller;   // auto-found if left empty
    [SerializeField] private StarterAssetsInputs starterInputs;  // auto-found if left empty

    [Header("Aim Rotation")]
    [SerializeField] private float aimRotationSpeed = 15f;

    public bool IsAiming  { get; private set; }
    public bool HasWeapon { get; private set; }

    void Awake()
    {
        // Fall back to components on the same GameObject so this works without wiring the inspector.
        if (controller == null)    TryGetComponent(out controller);
        if (starterInputs == null) TryGetComponent(out starterInputs);
    }

    void Update()
    {
        // ---- Weapon equipped check ----
        HasWeapon = playerShooting != null
                    && playerShooting.equippedWeapons != null
                    && playerShooting.activeSlot < playerShooting.equippedWeapons.Length
                    && playerShooting.equippedWeapons[playerShooting.activeSlot] != null;

        // A dodge roll temporarily breaks aim (like ARC Raiders): keeps the character framed
        // in the wide cam during the lunge and stops the rotation fight with the roll.
        bool isRolling = controller != null && controller.IsRolling;

        // Aiming = right-click + weapon equipped, but never while rolling.
        IsAiming = Input.GetMouseButton(1) && HasWeapon && !isRolling;

        // Right-click to aim cancels sprinting.
        if (IsAiming && starterInputs != null)
        {
            starterInputs.sprint = false;
        }

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