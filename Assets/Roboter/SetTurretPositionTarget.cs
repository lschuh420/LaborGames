using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Setzt ein Vector3-Positionsziel für den TurretController oder schaltet es ab.")]
public class SetTurretPositionTarget : Action
{
    [UnityEngine.Tooltip("Die Koordinate aus dem Blackboard")]
    public SharedVector3 positionFromBlackboard;

    [UnityEngine.Tooltip("Haken rein = Ziel anschauen, Haken raus = Turm normalisieren")]
    public bool activatePositionTarget = true;

    private TurretController turret;

    public override void OnAwake()
    {
        turret = Owner.GetComponentInChildren<TurretController>();
        if (turret == null)
            Debug.LogError("[SetTurretPositionTarget] Kein TurretController gefunden unter: " + Owner.name);
    }

    public override TaskStatus OnUpdate()
    {
        if (turret == null) return TaskStatus.Failure;

        // Wir räumen das alte Transform-Ziel auf und setzen den neuen Modus
        turret.target = null;
        turret.usePositionTarget = activatePositionTarget;

        if (activatePositionTarget)
        {
            turret.positionTarget = positionFromBlackboard.Value;
        }

        return TaskStatus.Success;
    }
}