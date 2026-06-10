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

    [Header("Audio (Surgical)")]
    public AudioSource audioSource; // EXPLICIT
    [Tooltip("Ein loopbarer Sound für das Fahren/Brummen")]
    public AudioClip moveClip;
    [SerializeField, Range(0f, 1f)] private float moveVolume = 0.3f;

    private NavMeshAgent agent;
    private StepManager stepManager;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stepManager = GetComponentInChildren<StepManager>();
        agent.speed = maxSpeed;

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        
        if (audioSource != null && moveClip != null)
        {
            audioSource.clip = moveClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        UpdateMovementLogic();
        UpdateAudio();
    }

    void UpdateMovementLogic()
    {
        if (!agent.hasPath) return;

        float stress = stepManager != null ? stepManager.currentSystemStress : 0f;
        if (stress < stressDeadzone) stress = 0f;

        float targetSpeed = Mathf.Lerp(maxSpeed, minSpeed, stress);
        agent.speed = Mathf.Lerp(agent.speed, targetSpeed, Time.deltaTime * brakingForce);
    }

    void UpdateAudio()
    {
        if (audioSource == null || moveClip == null) return;

        float currentSpeed = agent.velocity.magnitude;
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);

        audioSource.volume = (0.1f + speedFactor * 0.9f) * moveVolume;
        audioSource.pitch = 0.8f + (speedFactor * 0.4f);
    }
}
