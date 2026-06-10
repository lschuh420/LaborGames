using UnityEngine;

public class MechBullet : MonoBehaviour
{
    public int damage = 1;
    public float speed = 35f;
    public float lifetime = 4f;

    [Header("Visual Effects (Impact)")]
    [Tooltip("Effekt, der beim Einschlag (z.B. Funken) gespawned wird")]
    public GameObject impactEffectPrefab;
    [Tooltip("Das Schussloch-Prefab, das an Wänden kleben bleibt")]
    public GameObject bulletHolePrefab;

    [Header("Audio (Surgical)")]
    public AudioClip[] impactSounds;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.5f;

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
        bool hitPlayer = player != null;

        if (hitPlayer)
        {
            Debug.Log($"[MechBullet] SPIELER GETROFFEN!");
        }

        // ====================================================================
        // 2. NEU: Einschlag-Effekt & Schussloch erzeugen
        // ====================================================================
        
        // Wir machen einen kurzen Raycast nach vorne, um die exakte Einschlagstelle und Normale zu finden
        RaycastHit hit;
        if (Physics.Raycast(transform.position - transform.forward * 0.5f, transform.forward, out hit, 1.0f))
        {
            // Funken-Effekt (immer, auch beim Spieler)
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // Schussloch-Effekt (NUR wenn wir NICHT den Spieler getroffen haben)
            if (bulletHolePrefab != null && !hitPlayer)
            {
                // Zusätzlicher Check: Ist das getroffene Objekt vielleicht auf dem Player-Layer oder hat den Player-Tag?
                if (other.gameObject.tag != "Player" && other.gameObject.layer != LayerMask.NameToLayer("Player"))
                {
                    GameObject hole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                    // Schussloch am getroffenen Objekt befestigen
                    hole.transform.SetParent(other.transform);
                }
            }
        }
        else
        {
            // Fallback
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, transform.position, Quaternion.LookRotation(transform.forward * -1));
            }
        }

        // 3. Sound abspielen
        if (impactSounds != null && impactSounds.Length > 0)
        {
            AudioClip randomClip = impactSounds[Random.Range(0, impactSounds.Length)];
            AudioSource.PlayClipAtPoint(randomClip, transform.position, impactVolume);
        }

        // Zerstört die Kugel
        Destroy(gameObject);
    }
}
