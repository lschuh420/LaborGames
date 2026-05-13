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

    // Other scripts (like PlayerShooting) can read this
    public bool IsAiming { get; private set; }

    void Update()
    {
        // Hold RIGHT click to aim
        IsAiming = Input.GetMouseButton(1);

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
    }
}