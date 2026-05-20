using UnityEngine;

public class MechBullet : MonoBehaviour
{
    public int damage = 1;
    public float speed = 35f;
    public float lifetime = 4f;

    // NEU: Ein Partikelsystem-Prefab, das beim Einschlag gespawned wird (z.B. Funken)
    [Header("Visual Effects (Impact)")]
    public GameObject impactEffectPrefab;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Mechanik: Schaden am Spieler (wie gehabt)
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            Debug.Log($"[MechBullet] SPIELER GETROFFEN!");
        }

        // ====================================================================
        // 2. NEU: Einschlag-Effekt erzeugen (Visuell)
        // ====================================================================
        if (impactEffectPrefab != null)
        {
            // Wir spawnen die Funken exakt dort, wo die Kugel gerade ist.
            // Wir drehen die Funken so, dass sie von der Wand wegfliegen (`Quaternion.LookRotation(transform.forward * -1)`)
            GameObject impact = Instantiate(
                impactEffectPrefab,
                transform.position,
                Quaternion.LookRotation(transform.forward * -1)
            );

            // WICHTIG: Das gespawnte Partikelsystem-Prefab muss sich im Inspector bei "Stop Action" auf "Destroy" stellen, damit es sich selbst löscht!
        }

        // Zerstört die Kugel (Mechanik)
        Destroy(gameObject);
    }
}