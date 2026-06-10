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

    public float fireRate = 0.3f;
    private float nextFireTime = 0f;
    private int currentMuzzleIndex = 0;

    private bool isFiring = false;
    private float lastFireCommandTime = 0f;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
    }

    public void FireAtTarget()
    {
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
