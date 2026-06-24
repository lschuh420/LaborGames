using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Dieses Skript ist nur für die Präsentation gedacht. 
/// Es deaktiviert die Kampf-KI und lässt den Roboter einfach zu einem Ziel laufen,
/// ideal um zu zeigen, wie die IK-Beine über Hindernisse klettern.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PresentationWalker : MonoBehaviour
{
    [Header("Presentation Setup")]
    [Tooltip("Erstelle ein leeres GameObject in der Szene und ziehe es hier rein. Der Roboter wird dorthin laufen.")]
    public Transform walkTarget;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // 1. Die echte KI (Behavior Designer) für diese Szene ausschalten, 
        // damit sie nicht anfängt zu schießen oder zu patrouillieren.
        var behaviorTree = GetComponent("BehaviorDesigner.Runtime.BehaviorTree") as MonoBehaviour;
        if (behaviorTree != null)
        {
            behaviorTree.enabled = false;
        }

        // 2. Den manuellen Controller ausschalten, falls er aktiv ist.
        var manualController = GetComponent("RobotController") as MonoBehaviour;
        if (manualController != null)
        {
            manualController.enabled = false;
        }

        // 3. Kampf-Skripte ausschalten (optional, damit er wirklich nur läuft)
        var combatMaster = GetComponent("RobotCombatMaster") as MonoBehaviour;
        if (combatMaster != null) combatMaster.enabled = false;
    }

    void Update()
    {
        // Solange ein Ziel da ist, sagen wir dem NavMeshAgent, dass er dorthin laufen soll
        if (agent != null && walkTarget != null)
        {
            // Er läuft automatisch los und nutzt weiterhin MechDriver.cs für die saubere Beschleunigung!
            agent.SetDestination(walkTarget.position);
        }
    }
}
