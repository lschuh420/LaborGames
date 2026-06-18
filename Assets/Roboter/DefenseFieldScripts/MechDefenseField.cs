using UnityEngine;
using System.Collections.Generic;

public class MechDefenseField : MonoBehaviour
{
    [Header("Field Settings")]
    [Tooltip("Wie weit die Explosion reicht")]
    public float fieldRadius = 12f;

    [Tooltip("Wie viel Schaden die Nova macht")]
    public int damage = 25; // Macht ordentlich Wumms!

    [Header("Visuals")]
    [Tooltip("Das Partikelsystem für die Energie-Nova")]
    public ParticleSystem novaEffect;

    [Header("Audio (Surgical)")]
    public AudioSource audioSource; // EXPLICIT
    [Tooltip("Sound, der während des Aufladens spielt (wird vom Master-Script gesteuert)")]
    public AudioClip chargingSound;
    [Tooltip("Sound der finalen Explosion")]
    public AudioClip novaExplosionSound;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.7f;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null) audioSource = transform.root.GetComponentInChildren<AudioSource>();
    }

    public void PlayChargingSound()
    {
        if (audioSource != null && chargingSound != null)
        {
            audioSource.clip = chargingSound;
            audioSource.loop = false;
            audioSource.Play();
        }
    }

    public void StopChargingSound()
    {
        if (audioSource != null && audioSource.clip == chargingSound)
        {
            audioSource.Stop();
        }
    }

    public void TriggerField()
    {
        if (novaEffect != null)
        {
            novaEffect.Play();
        }

        if (audioSource != null && novaExplosionSound != null)
        {
            audioSource.PlayOneShot(novaExplosionSound, audioVolume);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, fieldRadius, ~0, QueryTriggerInteraction.Collide);
        Transform root = transform.root; // Referenz auf den eigenen Roboter-Stamm

        // Verhindert, dass ein Ziel mit mehreren Collidern mehrfach Schaden bekommt
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            // EIGENSCHUTZ: Wenn der getroffene Collider zum eigenen Roboter gehört, ignorieren
            if (hit.transform.root == root) continue;

            // IDamageable zuerst in den Parents suchen, sonst in den Kindern
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) target = hit.GetComponentInChildren<IDamageable>();

            if (target != null && alreadyHit.Add(target))
            {
                Debug.Log($"<color=cyan>[MechDefenseField] Nova trifft {hit.transform.root.name} für {damage} Schaden.</color>");
                target.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, fieldRadius);
    }
}
