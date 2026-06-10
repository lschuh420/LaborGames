using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Prüft, ob der Spieler innerhalb des Verteidigungs-Radius ist.")]
public class IsPlayerInMeleeRange : Conditional
{
    public SharedGameObject targetEntity;
    public float meleeRange = 5f;

    public override TaskStatus OnUpdate()
    {
        if (targetEntity.Value == null) return TaskStatus.Failure;

        float distance = Vector3.Distance(Owner.transform.position, targetEntity.Value.transform.position);

        if (distance <= meleeRange) return TaskStatus.Success;

        return TaskStatus.Failure;
    }
}