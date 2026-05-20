using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Feuert auf das Target, aber nur wenn direkte Sichtlinie (keine Wand) besteht.")]
public class RobotShootTarget : Action
{
    [UnityEngine.Tooltip("Das aktuelle Ziel aus dem Blackboard (Spieler)")]
    public SharedGameObject targetEntity;

    [UnityEngine.Tooltip("Von wo schaut der Mech? (Z.B. das EyesPivot Objekt)")]
    public Transform eyesPivot;

    [UnityEngine.Tooltip("Welche Layer blockieren die Sicht? (Wände/Boden)")]
    public LayerMask obstacleLayer;

    private MechWeapon[] mechWeapons;

    public override void OnAwake()
    {
        // Holt sich alle Waffen-Komponenten am Mech
        mechWeapons = Owner.GetComponentsInChildren<MechWeapon>();

        if (mechWeapons == null || mechWeapons.Length == 0)
        {
            Debug.LogWarning($"[RobotShootTarget] Keine MechWeapon auf {Owner.name} gefunden!");
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (targetEntity.Value == null || mechWeapons == null)
            return TaskStatus.Failure;

        // 1. Positionen für den Sicht-Check berechnen
        // Wir peilen ungefähr die Brust des Spielers an (+ 1 Meter nach oben)
        Vector3 targetPos = targetEntity.Value.transform.position + Vector3.up * 1.0f;

        // Startpunkt ist das Auge des Mechs (oder ersatzweise einfach oben am Rumpf)
        Vector3 startPos = eyesPivot != null ? eyesPivot.position : Owner.transform.position + Vector3.up * 1.5f;

        // 2. Der Linecast (Sichtlinien-Prüfung)
        // Zieht eine unsichtbare Linie vom Auge zur Spieler-Brust. 
        // Wenn die Linie auf dem ObstacleLayer (Wand) etwas trifft, ist die Sicht blockiert!
        if (Physics.Linecast(startPos, targetPos, obstacleLayer))
        {
            // Wand im Weg! Wir brechen den Schuss ab, melden dem Parallel-Knoten aber "Running", 
            // damit der Mech den Spieler weiterhin verfolgt (Seek).
            return TaskStatus.Running;
        }

        // 3. Keine Wand im Weg -> Feuer frei!
        foreach (MechWeapon weapon in mechWeapons)
        {
            weapon.FireAtTarget();
        }

        return TaskStatus.Running;
    }
}