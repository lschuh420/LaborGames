using UnityEngine;

public class LegSkeletonVisualizer : MonoBehaviour
{
    public Transform hip;
    public Transform upperLeg;
    public Transform lowerLeg;
    public Transform footTip;

    // Diese Unity-Funktion zeichnet Hilfslinien im Editor (sichtbar im Scene-Fenster)
    private void OnDrawGizmos()
    {
        // Abbruch, falls noch nicht alle Gelenke zugewiesen wurden
        if (hip == null || upperLeg == null || lowerLeg == null || footTip == null) return;

        // Zeichne grüne Linien ("Knochen") zwischen den Gelenken
        Gizmos.color = Color.green;
        Gizmos.DrawLine(hip.position, upperLeg.position);
        Gizmos.DrawLine(upperLeg.position, lowerLeg.position);
        Gizmos.DrawLine(lowerLeg.position, footTip.position);

        // Zeichne kleine gelbe Kugeln an die Gelenkpunkte
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hip.position, 0.05f);
        Gizmos.DrawSphere(upperLeg.position, 0.05f);
        Gizmos.DrawSphere(lowerLeg.position, 0.05f);
        Gizmos.DrawSphere(footTip.position, 0.05f);
    }
}