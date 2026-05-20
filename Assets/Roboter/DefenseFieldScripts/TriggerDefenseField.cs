using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Löst die Energie-Nova aus.")]
public class TriggerDefenseField : Action
{
    private MechDefenseField defenseField;

    public override void OnAwake()
    {
        defenseField = Owner.GetComponentInChildren<MechDefenseField>();
    }

    public override TaskStatus OnUpdate()
    {
        if (defenseField == null) return TaskStatus.Failure;

        defenseField.TriggerField();
        return TaskStatus.Success;
    }
}