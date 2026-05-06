using UnityEngine;

public class WeaponIK : MonoBehaviour
{
    private Animator animator;
    public Transform leftHandTarget;

    [Range(0, 1)]
    public float leftHandWeight = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;
        if (leftHandTarget == null) return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
    }
}