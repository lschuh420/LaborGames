using UnityEngine;

public class MechWeapon : MonoBehaviour
{
    [Header("Weapon Setup")]
    public GameObject bulletPrefab;
    [Tooltip("Die Mündungen, aus denen die Kugeln fliegen")]
    public Transform[] muzzlePoints;

    [Header("Visual Effects (Muzzle)")]
    public ParticleSystem[] muzzleFlashes;

    [Header("Audio (Surgical)")]
    public AudioSource audioSource; // EXPLICIT
    [Tooltip("Sound, der kurz vor dem ersten Schuss spielt (Telegraphing)")]
    public AudioClip preFireSound;
    public AudioClip fireSound;
    [SerializeField, Range(0f, 1f)] private float fireVolume = 0.5f;

    [Header("Burst Settings")]
    [Tooltip("Wie viele Schüsse in einer Salve?")]
    public int burstSize = 15;
    [Tooltip("Pause zwischen zwei Salven in Sekunden")]
    public float reloadTime = 1.3f;

    public float fireRate = 0.3f;
    private float nextFireTime = 0f;
    private int currentMuzzleIndex = 0;
    private int shotsFiredInBurst = 0;
    private float burstCooldownEndTime = 0f;

    private bool isFiring = false;
    private float lastFireCommandTime = 0f;
    private TurretController turretController;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        turretController = GetComponentInParent<TurretController>();
    }

    public void FireAtTarget()
    {
        // 1. Pause zwischen Salven prüfen
        if (Time.time < burstCooldownEndTime)
        {
            // Debug.Log("[MechWeapon] Nachladen...");
            return;
        }

        // 2. Nur schießen, wenn der Turm auch wirklich auf das Ziel ausgerichtet ist (Toleranz-Check)
        if (turretController != null && !turretController.IsAimedAtTarget)
        {
            return;
        }

        float currentTime = Time.time;
        
        if (!isFiring || (currentTime - lastFireCommandTime > 1.0f))
        {
            if (audioSource != null && preFireSound != null)
            {
                audioSource.PlayOneShot(preFireSound, fireVolume);
            }
            isFiring = true;
        }
        
        lastFireCommandTime = currentTime;

        if (Time.time < nextFireTime) return;
        if (bulletPrefab == null || muzzlePoints.Length == 0) return;

        // 3. Salven-Logik anwenden
        ExecuteShoot();

        shotsFiredInBurst++;
        if (shotsFiredInBurst >= burstSize)
        {
            shotsFiredInBurst = 0;
            burstCooldownEndTime = Time.time + reloadTime;
            isFiring = false; // Telegraphing für die nächste Salve neu triggern
            Debug.Log($"<color=white>[MechWeapon] Salve beendet ({burstSize} Schuss). Pause für {reloadTime}s.</color>");
        }
    }

    private void ExecuteShoot()
    {
        nextFireTime = Time.time + fireRate;

        Transform activeMuzzle = muzzlePoints[currentMuzzleIndex];

        Instantiate(bulletPrefab, activeMuzzle.position, activeMuzzle.rotation);

        if (muzzleFlashes != null && muzzleFlashes.Length > currentMuzzleIndex)
        {
            muzzleFlashes[currentMuzzleIndex].Play();
        }

        if (audioSource != null && fireSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(fireSound, fireVolume);
        }

        currentMuzzleIndex = (currentMuzzleIndex + 1) % muzzlePoints.Length;
    }

    private void Update()
    {
        if (isFiring && (Time.time - lastFireCommandTime > 1.0f))
        {
            isFiring = false;
        }
    }
}
