using UnityEngine;
using BehaviorDesigner.Runtime;

public class MechSensor : MonoBehaviour
{
    [Header("Behavior Designer Link")]
    public BehaviorTree behaviorTree;

    [Header("Visual Feedback (Licht & Scanner)")]
    public Light statusLight;
    public MeshRenderer scannerConeRenderer;
    public float scannerAlpha = 0.3f;

    public Color colorGreen = Color.green;
    public Color colorYellow = Color.yellow;
    public Color colorRed = Color.red;

    [Header("Scanner Beam Dynamics")]
    public Transform scannerPivot;
    public float maxScannerLength = 25f;

    [Header("Audio (Surgical)")]
    public AudioClip alertSound;
    [SerializeField, Range(0f, 1f)] private float alertVolume = 0.7f;
    private AudioSource audioSource;

    [Header("Senses Setup")]
    [Tooltip("Der Punkt, von dem aus der Mech schaut. Muss vor dem Körper liegen!")]
    public Transform eyesPivot;
    public float sightRadius = 25f;
    public float fieldOfView = 90f;
    public LayerMask playerLayer;
    [Tooltip("Layer, die die Sicht blockieren (Wände, Hindernisse). DARF NICHT den Robot-Layer enthalten!")]
    public LayerMask obstacleLayer;

    [Header("Memory & Delays")]
    [Tooltip("Wie viele Sekunden bleibt er nach Sichtverlust noch auf ROT?")]
    public float loseSightDelay = 8f;
    private float timeSinceLastSeen = 0f;

    [Header("State Output (Nur zur Übersicht)")]
    public int currentAlertState = 0; // 0=Grün, 1=Gelb, 2=Rot
    public Transform currentTarget = null;
    public Vector3 lastKnownPosition;

    void Start()
    {
        if (behaviorTree == null) behaviorTree = GetComponent<BehaviorTree>();
        
        // Sicherheits-Check für eyesPivot
        if (eyesPivot == null)
        {
            Debug.LogWarning($"[MechSensor] Kein EyesPivot auf {name} zugewiesen! Benutze transform.position.");
            eyesPivot = transform;
        }

        // AudioSource im Sound-Container suchen
        audioSource = GetComponentInChildren<AudioSource>();
    }

    void Update()
    {
        SyncStateFromTree();
        LookForPlayer();
        UpdateLightColor();
        UpdateScannerLength();
    }

    void SyncStateFromTree()
    {
        if (behaviorTree == null) return;
        SharedInt alertVar = behaviorTree.GetVariable("AlertState") as SharedInt;
        if (alertVar != null) currentAlertState = alertVar.Value;
    }

    void UpdateLightColor()
    {
        Color targetColor = colorGreen;
        if (currentAlertState == 1) targetColor = colorYellow;
        else if (currentAlertState == 2) targetColor = colorRed;

        if (statusLight != null) statusLight.color = targetColor;

        if (scannerConeRenderer != null)
        {
            Color coneColor = new Color(targetColor.r, targetColor.g, targetColor.b, scannerAlpha);
            scannerConeRenderer.material.SetColor("_BaseColor", coneColor);
        }
    }

    void UpdateScannerLength()
    {
        if (scannerPivot == null || eyesPivot == null) return;
        float currentLength = maxScannerLength;
        LayerMask visualHitMask = obstacleLayer | playerLayer;

        if (Physics.Raycast(eyesPivot.position, eyesPivot.forward, out RaycastHit hit, maxScannerLength, visualHitMask))
        {
            currentLength = hit.distance;
            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0) currentLength += 0.4f;
        }

        Vector3 newScale = scannerPivot.localScale;
        newScale.z = currentLength;
        scannerPivot.localScale = newScale;
    }

    void LookForPlayer()
    {
        if (eyesPivot == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, sightRadius, playerLayer);
        bool playerSeenThisFrame = false;

        if (hits.Length > 0)
        {
            Transform potentialTarget = hits[0].transform;
            Vector3 targetCenter = hits[0].bounds.center;
            Vector3 directionToTarget = (targetCenter - eyesPivot.position).normalized;
            Vector3 flatForward = Vector3.ProjectOnPlane(eyesPivot.forward, Vector3.up).normalized;

            if (Vector3.Angle(flatForward, directionToTarget) < fieldOfView / 2f)
            {
                float distanceToTarget = Vector3.Distance(eyesPivot.position, targetCenter);

                bool hitSomething = Physics.Raycast(eyesPivot.position, directionToTarget, out RaycastHit rayHit, distanceToTarget, obstacleLayer);
                
                if (!hitSomething)
                {
                    if (currentAlertState != 2)
                    {
                        Debug.Log($"<color=red>[MechSensor] Spieler GESEHEN! Wechsel zu ROT.</color>");
                        
                        // Alarm-Sound abspielen (nur beim Wechsel zu ROT)
                        if (audioSource != null && alertSound != null)
                        {
                            audioSource.PlayOneShot(alertSound, alertVolume);
                        }

                        SetStateRed(potentialTarget);
                    }
                    playerSeenThisFrame = true;
                    timeSinceLastSeen = 0f;
                }
                else
                {
                    Debug.DrawLine(eyesPivot.position, rayHit.point, Color.red);
                }
            }
        }

        if (!playerSeenThisFrame && currentAlertState == 2)
        {
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen >= loseSightDelay)
            {
                Debug.Log($"<color=yellow>[MechSensor] Sichtverlust zu lange! Wechsel zu GELB.</color>");
                Vector3 lostPos = currentTarget != null ? currentTarget.position : transform.position;
                SetStateYellow(lostPos);
            }
        }
    }

    public void HearNoise(Vector3 noisePosition)
    {
        if (currentAlertState < 2)
        {
            Debug.Log($"<color=orange>[MechSensor] Geräusch gehört! Wechsel zu GELB.</color>");
            SetStateYellow(noisePosition);
        }
    }

    void SetStateRed(Transform target)
    {
        currentAlertState = 2;
        currentTarget = target;
        lastKnownPosition = target.position;
        UpdateBlackboard();
    }

    void SetStateYellow(Vector3 investigatePos)
    {
        currentAlertState = 1;
        currentTarget = null;
        lastKnownPosition = investigatePos;
        UpdateBlackboard();
    }

    void UpdateBlackboard()
    {
        if (behaviorTree == null) return;

        SharedInt alertVar = behaviorTree.GetVariable("AlertState") as SharedInt;
        if (alertVar != null) alertVar.Value = currentAlertState;

        SharedGameObject targetVar = behaviorTree.GetVariable("TargetEntity") as SharedGameObject;
        if (targetVar != null) targetVar.Value = currentTarget != null ? currentTarget.gameObject : null;

        SharedVector3 posVar = behaviorTree.GetVariable("InvestigationPos") as SharedVector3;
        if (posVar != null) posVar.Value = lastKnownPosition;
    }
}
