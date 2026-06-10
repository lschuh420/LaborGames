using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float speed = 100f;
    public float lifetime = 2f;

    [Header("Detection Settings")]
    public LayerMask hitLayers = ~0; // Auf was darf die Kugel treffen?
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Visuals")]
    public GameObject hitEffectPrefab; // Funken / Impact-Effekt

    private Vector3 lastPosition;
    private bool hasHit = false;

    void Start()
    {
        lastPosition = transform.position;
        Destroy(gameObject, lifetime);
        
        // Den Player-Layer ignorieren, damit wir uns nicht selbst erschießen
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            hitLayers &= ~(1 << playerLayer);
        }
    }

    void Update()
    {
        if (hasHit) return;

        Vector3 nextPosition = transform.position + transform.forward * speed * Time.deltaTime;
        float travelDist = Vector3.Distance(lastPosition, nextPosition);

        // Visualisierung im Editor (Scene View)
        Debug.DrawRay(lastPosition, transform.forward * travelDist, Color.red, 0.1f);

        // AAA-Lösung: Raycast-Check für High-Speed Projectiles
        if (Physics.Raycast(lastPosition, transform.forward, out RaycastHit hit, travelDist + 0.1f, hitLayers, triggerInteraction))
        {
            Debug.Log($"<color=white>[Bullet] Raycast-Treffer auf: {hit.collider.name}</color>");
            SpawnHitEffect(hit);
            HandleHit(hit.collider);
            return;
        }

        transform.position = nextPosition;
        lastPosition = transform.position;
    }

    // Fallback für normale Trigger/Collider (falls Raycast mal nicht greift)
    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Player ignorieren
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) return;

        Debug.Log($"<color=white>[Bullet] Trigger-Treffer auf: {other.name}</color>");
        
        // Wir brauchen einen Kontaktpunkt für den Effekt, im Trigger nutzen wir einfach die Position
        HandleHit(other);
    }

    private void SpawnHitEffect(RaycastHit hit)
    {
        if (hitEffectPrefab != null)
        {
            // Funken an der Aufschlagstelle spawnen und in Richtung der Normalen drehen
            Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    private void HandleHit(Collider other)
    {
        hasHit = true;
        
        // Zuerst direkt auf dem getroffenen Objekt suchen (für DamageRouter)
        IDamageable damageable = other.GetComponent<IDamageable>();
        
        // Wenn nicht gefunden, in den Parents suchen (Fallback zu MechBossHealth)
        if (damageable == null)
        {
            damageable = other.GetComponentInParent<IDamageable>();
        }
        
        if (damageable != null)
        {
            Debug.Log($"<color=cyan>[Bullet] Ziel gefunden: {damageable.GetType().Name} auf Objekt: {other.name}</color>");
            damageable.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"<color=yellow>[Bullet] {other.name} getroffen, aber kein IDamageable gefunden!</color>");
        }

        // Bei Kollision Kugel zerstören
        Destroy(gameObject);
    }
}
