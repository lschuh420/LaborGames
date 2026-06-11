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
    private float baseMaxSpeed;
    private int destroyedLegsCount = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stepManager = GetComponentInChildren<StepManager>();
        baseMaxSpeed = maxSpeed;
        agent.speed = maxSpeed;

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        
        if (audioSource != null && moveClip != null)
        {
            audioSource.clip = moveClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Auf Bein-Zerstörung reagieren
        MechBossHealth.OnLegDestroyed += HandleLegDestroyed;
    }

    private void OnDestroy()
    {
        MechBossHealth.OnLegDestroyed -= HandleLegDestroyed;
    }

    private void HandleLegDestroyed(int count)
    {
        destroyedLegsCount = count;
        UpdateMaxSpeed();
    }

    private void UpdateMaxSpeed()
    {
        // 1. Basis-Reduktion: 15% pro Bein
        float factor = 1f - (destroyedLegsCount * 0.15f);

        // 2. Kritischer Malus: Ab 3 Beinen wird es EXTREM mühsam (Arc Raiders Style)
        if (destroyedLegsCount >= 3)
        {
            // Massive Reduktion: Er schleppt sich nur noch
            factor *= 0.15f; 
            Debug.Log("<color=red>[MechDriver] KRITISCHER BEINSCHADEN! Der Mech kann sich kaum noch halten.</color>");
        }

        // 3. Absolutes Minimum (damit er nicht ganz stehen bleibt, außer bei 6 Beinen?)
        if (destroyedLegsCount >= 4) factor = 0.08f;
        if (destroyedLegsCount >= 5) factor = 0.03f; 
        if (destroyedLegsCount >= 6) factor = 0f;    // Komplett immobil

        maxSpeed = baseMaxSpeed * Mathf.Max(0f, factor);
        Debug.Log($"[MechDriver] Neue Höchstgeschwindigkeit: {maxSpeed} (Beine weg: {destroyedLegsCount})");
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

        // Wenn er gestunnt ist (von MechBossHealth gesteuert), sollte er eigentlich stehen bleiben.
        // Das macht der Behavior Tree meistens schon, aber wir können hier sicherheitshalber drosseln.
        
        float currentTargetMax = maxSpeed;
        
        float targetSpeed = Mathf.Lerp(currentTargetMax, minSpeed, stress);
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
