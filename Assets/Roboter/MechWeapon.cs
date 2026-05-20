using UnityEngine;

public class MechWeapon : MonoBehaviour
{
    [Header("Weapon Setup")]
    public GameObject bulletPrefab;
    [Tooltip("Die Mündungen, aus denen die Kugeln fliegen")]
    public Transform[] muzzlePoints;

    // NEU: Visuelle Mündungseffekte (Particle Systems, die wir am Lauf platzieren)
    [Header("Visual Effects (Muzzle)")]
    public ParticleSystem[] muzzleFlashes;

    public float fireRate = 0.3f;
    private float nextFireTime = 0f;
    private int currentMuzzleIndex = 0;

    public void FireAtTarget()
    {
        if (Time.time < nextFireTime) return;
        if (bulletPrefab == null || muzzlePoints.Length == 0) return;

        nextFireTime = Time.time + fireRate;

        Transform activeMuzzle = muzzlePoints[currentMuzzleIndex];

        // 1. Kugel instanziieren (Mechanik)
        Instantiate(bulletPrefab, activeMuzzle.position, activeMuzzle.rotation);

        // ====================================================================
        // 2. NEU: Mündungsblitz abspielen (Visuell)
        // ====================================================================
        if (muzzleFlashes != null && muzzleFlashes.Length > currentMuzzleIndex)
        {
            // Wir "Playen" das Partikelsystem, das an diesem Lauf befestigt ist.
            // WICHTIG: Im Partikelsystem muss 'Looping' AUS sein.
            muzzleFlashes[currentMuzzleIndex].Play();
        }

        currentMuzzleIndex = (currentMuzzleIndex + 1) % muzzlePoints.Length;
    }
}