using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Setzt das Ziel für den TurretController aus dem Blackboard.")]
public class SetTurretTarget : Action
{
    public SharedGameObject targetFromBlackboard;
    private TurretController turret;

    public override void OnAwake()
    {
        // In Behavior Designer Tasks: Owner ist das GameObject auf dem der BehaviorTree liegt
        turret = Owner.GetComponentInChildren<TurretController>();

        if (turret == null)
            Debug.LogError("[SetTurretTarget] Kein TurretController gefunden unter: " + Owner.name);
        else
            Debug.Log("[SetTurretTarget] TurretController gefunden auf: " + turret.gameObject.name);
    }

    public override TaskStatus OnUpdate()
    {
        if (turret == null)
            return TaskStatus.Failure;

        GameObject go = targetFromBlackboard.Value;
        turret.target = go != null ? go.transform : null;

        return TaskStatus.Success;
    }
}