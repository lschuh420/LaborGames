using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MechDriver : MonoBehaviour
{
    [Header("Speed Setup")]
    [Tooltip("Die normale Laufgeschwindigkeit")]
    [SerializeField] private float maxSpeed = 3.5f;
    [Tooltip("Wie hart tritt er auf die Bremse? Höher = stoppt abrupter")]
    [SerializeField] private float brakingForce = 5f;

    [Header("Stress Throttling")]
    [Tooltip("Unter diesem Stress-Wert wird gar nicht gebremst (verhindert Ruckeln bei normalen Schritten)")]
    [SerializeField] private float stressDeadzone = 0.15f;
    [Tooltip("Minimale Geschwindigkeit, die dem Agent IMMER garantiert wird (verhindert Seek-Failure)")]
    [SerializeField] private float minSpeed = 0.4f;

    private NavMeshAgent agent;
    private StepManager stepManager;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stepManager = GetComponentInChildren<StepManager>();
        agent.speed = maxSpeed;
    }

    void Update()
    {
        if (!agent.hasPath) return;

        float stress = stepManager != null ? stepManager.currentSystemStress : 0f;

        // Deadzone: Kleiner Stress wird ignoriert, damit normale Schritte
        // nicht ständig eine Minibremsung auslösen.
        if (stress < stressDeadzone) stress = 0f;

        // Garantierte Mindestgeschwindigkeit: Der Agent steht NIE komplett still
        // durch Stress allein — das würde den Seek-Task zum sofortigen Failure bringen.
        float targetSpeed = Mathf.Lerp(maxSpeed, minSpeed, stress);

        agent.speed = Mathf.Lerp(agent.speed, targetSpeed, Time.deltaTime * brakingForce);
    }
}