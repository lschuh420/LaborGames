using UnityEngine;

public class WeaponIK : MonoBehaviour
{
    private Animator animator;

    [Header("Left Hand (grip support)")]
    public Transform leftHandTarget;
    [Range(0, 1)]
    public float leftHandWeight = 1f;

    [Header("Aim IK (right arm extends forward while aiming)")]
    [Tooltip("Drives WHEN the aim IK is active. Auto-found on this object if left empty.")]
    public PlayerAim playerAim;
    [Tooltip("Master switch for the forward-aim IK.")]
    public bool aimArmForward = true;
    [Range(0, 1)]
    public float aimWeight = 1f;
    [Tooltip("How far in front of the shoulder the hand is pushed, in metres. Bigger = arm more extended.")]
    public float aimReach = 0.45f;
    [Tooltip("Hand position offset in BODY space: X = right, Y = up, Z = forward.")]
    public Vector3 aimHandOffset = new Vector3(0.08f, -0.05f, 0f);
    [Tooltip("Hand rotation (degrees) applied on top of facing forward. Tune until the barrel points forward.")]
    public Vector3 aimHandRotationOffset = Vector3.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (playerAim == null) playerAim = GetComponent<PlayerAim>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // ---- Right arm: extend forward to aim ----
        // The body already faces the aim (camera) direction while aiming, so we push the
        // right hand out in front of the shoulder. The humanoid solver then straightens
        // the arm toward that point instead of holding the weak across-body aim pose.
        bool aiming = aimArmForward && playerAim != null && playerAim.IsAiming;
        if (aiming)
        {
            Transform shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (shoulder != null)
            {
                Vector3 fwd = transform.forward;
                Vector3 handPos = shoulder.position
                                + fwd * aimReach
                                + transform.right * aimHandOffset.x
                                + transform.up * aimHandOffset.y
                                + transform.forward * aimHandOffset.z;

                Quaternion handRot = Quaternion.LookRotation(fwd, transform.up)
                                   * Quaternion.Euler(aimHandRotationOffset);

                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, aimWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, aimWeight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, handPos);
                animator.SetIKRotation(AvatarIKGoal.RightHand, handRot);
            }
        }

        // ---- Left hand: keep it on the gun's grip ----
        if (leftHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
    }
}
