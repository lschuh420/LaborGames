using UnityEngine;

public class WeaponData : MonoBehaviour
{
    public string weaponName = "Rifle";
    public int damage = 25;
    public float fireRate = 0.2f;

    [Header("Grip Alignment (per-weapon override)")]
    [Tooltip("If ON, PlayerShooting uses THIS weapon's offsets below instead of its default " +
             "ones. Leave OFF to use the holder's default alignment.")]
    public bool overrideGripAlignment = false;
    [Tooltip("Local position of the gun relative to the right-hand holder.")]
    public Vector3 gripPositionOffset = Vector3.zero;
    [Tooltip("Local rotation of the gun in degrees, relative to the hand. To un-rotate a " +
             "sideways gun, roll it around the barrel axis (try Z or X = 90 / -90).")]
    public Vector3 gripRotationOffset = Vector3.zero;
}