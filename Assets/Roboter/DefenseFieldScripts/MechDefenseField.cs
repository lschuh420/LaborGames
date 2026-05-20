using UnityEngine;

public class MechDefenseField : MonoBehaviour
{
    [Header("Field Settings")]
    [Tooltip("Wie weit die Explosion reicht")]
    public float fieldRadius = 5f;
    public int damage = 2;

    [Header("Visuals")]
    [Tooltip("Das Partikelsystem für die Energie-Nova")]
    public ParticleSystem novaEffect;

    public void TriggerField()
    {
        Debug.Log("<color=yellow>[1] TriggerField wurde vom Master-Knoten aufgerufen!</color>");

        if (novaEffect != null)
        {
            Debug.Log("<color=green>[2] Partikel gefunden! Spiele es jetzt ab!</color>");
            novaEffect.Play();
        }
        else
        {
            Debug.LogError("<color=red>[FEHLER] novaEffect ist NULL! Du hast das Partikelsystem im Inspector nicht reingezogen!</color>");
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, fieldRadius);
        Debug.Log($"<color=cyan>[3] Nova explodiert! Objekte im Radius gefunden: {hits.Length}</color>");
    }

    // Zeichnet eine hellblaue Kugel im Editor, damit du den Radius perfekt sehen und einstellen kannst
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, fieldRadius);
    }
}