using UnityEngine;

public class LaborProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 10;
    public float speed = 50f;
    public float lifetime = 3f;

    [Header("Collision")]
    public LayerMask hitLayers = ~0;
    public GameObject impactEffectPrefab;
    public GameObject bulletHolePrefab;

    [Header("Audio")]
    public AudioClip[] impactSounds;
    [Range(0f, 1f)] public float volume = 0.5f;

    private Vector3 lastPosition;
    private int ownerLayer;
    private bool hasHit = false;

    // Diese Methode rufen wir beim Spawnen auf
    public void Setup(int damageAmount, float bulletSpeed, int creatorLayer)
    {
        damage = damageAmount;
        speed = bulletSpeed;
        ownerLayer = creatorLayer;

        // Den Layer des Schützen ignorieren
        hitLayers &= ~(1 << ownerLayer);
        
        // Immer den "Ignore Raycast" Layer ignorieren
        hitLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
    }

    void Start()
    {
        lastPosition = transform.position;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasHit) return;

        Vector3 nextPosition = transform.position + transform.forward * speed * Time.deltaTime;
        float travelDist = Vector3.Distance(lastPosition, nextPosition);

        // Raycast-Check für saubere Treffererkennung
        if (Physics.Raycast(lastPosition, transform.forward, out RaycastHit hit, travelDist + 0.1f, hitLayers))
        {
            HandleHit(hit);
            return;
        }

        transform.position = nextPosition;
        lastPosition = transform.position;
    }

    private void HandleHit(RaycastHit hit)
    {
        hasHit = true;
        
        Debug.Log($"<color=white>[Projectile] Treffer auf: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})</color>");

        // 1. Effekte spawnen
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // 2. Schussloch (nur bei festen Objekten, nicht bei Charakteren)
        if (bulletHolePrefab != null && hit.collider.GetComponentInParent<IDamageable>() == null)
        {
            GameObject hole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal));
            hole.transform.SetParent(hit.collider.transform);
        }

        // 3. Schaden verursachen
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable == null) damageable = hit.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"<color=cyan>[Projectile] Schaden ({damage}) an {hit.collider.name} verursacht!</color>");
        }

        // 4. Sound
        if (impactSounds != null && impactSounds.Length > 0)
        {
            AudioClip clip = impactSounds[Random.Range(0, impactSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, hit.point, volume);
        }

        Destroy(gameObject);
    }
}
