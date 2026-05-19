using UnityEngine;
using BehaviorDesigner.Runtime;

public class MechSensor : MonoBehaviour
{
    [Header("Behavior Designer Link")]
    public BehaviorTree behaviorTree;

    [Header("Visual Feedback (Licht & Scanner)")]
    [Tooltip("Ziehe hier dein Spot Light rein (falls du noch eins auf dem Boden haben willst)")]
    public Light statusLight;

    [Tooltip("Ziehe hier deinen neuen 3D-Kegel (Hologramm-Strahl) rein")]
    public MeshRenderer scannerConeRenderer;
    [Tooltip("Wie durchsichtig soll der Strahl sein? (0 = unsichtbar, 1 = massiv)")]
    public float scannerAlpha = 0.3f;

    public Color colorGreen = Color.green;
    public Color colorYellow = Color.yellow;
    public Color colorRed = Color.red;

    [Header("Scanner Beam Dynamics")]
    [Tooltip("Ziehe hier das LEERE ScannerPivot-Objekt rein")]
    public Transform scannerPivot;
    [Tooltip("Wie lang ist der Strahl maximal, wenn keine Wand da ist?")]
    public float maxScannerLength = 25f;

    [Header("Senses Setup")]
    public Transform eyesPivot;
    public float sightRadius = 25f;
    public float fieldOfView = 90f;
    public LayerMask playerLayer;
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
        if (behaviorTree == null)
        {
            behaviorTree = GetComponent<BehaviorTree>();
        }
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
        if (alertVar != null)
        {
            currentAlertState = alertVar.Value;
        }
    }

    void UpdateLightColor()
    {
        // 1. Zielfarbe bestimmen
        Color targetColor = colorGreen;
        if (currentAlertState == 1) targetColor = colorYellow;
        else if (currentAlertState == 2) targetColor = colorRed;

        // 2. Das normale Spot Light aktualisieren (falls zugewiesen)
        if (statusLight != null)
        {
            statusLight.color = targetColor;
        }

        // 3. Den neuen Hologramm-Kegel aktualisieren (falls zugewiesen)
        if (scannerConeRenderer != null)
        {
            // Wir mischen die Farbe mit deiner gewünschten Durchsichtigkeit (Alpha)
            Color coneColor = new Color(targetColor.r, targetColor.g, targetColor.b, scannerAlpha);

            // In URP heißt die Hauptfarbe im Material standardmäßig "_BaseColor"
            scannerConeRenderer.material.SetColor("_BaseColor", coneColor);
        }
    }
    void UpdateScannerLength()
    {
        if (scannerPivot == null) return;
        float currentLength = maxScannerLength;

        LayerMask visualHitMask = obstacleLayer | playerLayer;

        if (Physics.Raycast(eyesPivot.position, eyesPivot.forward, out RaycastHit hit, maxScannerLength, visualHitMask))
        {
            currentLength = hit.distance;

            // NEU: Wenn der getroffene Layer zum Player gehört, mogeln wir!
            // Wir schieben den Laser optisch ein paar Zentimeter tiefer in die Hitbox.
            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
            {
                currentLength += 0.4f; // <-- Hier kannst du jonglieren (z. B. 0.3f oder 0.5f), bis es perfekt aussieht!
            }
        }

        Vector3 newScale = scannerPivot.localScale;
        newScale.z = currentLength;
        scannerPivot.localScale = newScale;
    }
    void LookForPlayer()
    {
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

                if (!Physics.Raycast(eyesPivot.position, directionToTarget, distanceToTarget, obstacleLayer))
                {
                    SetStateRed(potentialTarget);
                    playerSeenThisFrame = true;
                    timeSinceLastSeen = 0f;
                }
            }
        }

        if (!playerSeenThisFrame && currentAlertState == 2)
        {
            timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen >= loseSightDelay)
            {
                Vector3 lostPos = currentTarget != null ? currentTarget.position : transform.position;
                SetStateYellow(lostPos);
            }
        }
    }

    public void HearNoise(Vector3 noisePosition)
    {
        if (currentAlertState < 2)
        {
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