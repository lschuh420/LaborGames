using UnityEngine;
using BehaviorDesigner.Runtime; // Zwingend nötig für die Kommunikation mit dem Tool!

public class MechSensor : MonoBehaviour
{
    [Header("Behavior Designer Link")]
    [Tooltip("Zieht sich das Skript automatisch, wenn leer")]
    public BehaviorTree behaviorTree;

    [Header("Senses Setup")]
    public Transform eyesPivot;
    public float sightRadius = 25f;
    public float fieldOfView = 90f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("State Output (Nur zur Übersicht)")]
    public int currentAlertState = 0; // 0=Grün, 1=Gelb, 2=Rot
    public Transform currentTarget = null;
    public Vector3 lastKnownPosition;

    void Start()
    {
        // Holt sich die Behavior Tree Komponente automatisch vom Robot_Root
        if (behaviorTree == null)
        {
            behaviorTree = GetComponent<BehaviorTree>();
        }
    }

    void Update()
    {
        LookForPlayer();
    }

    void LookForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRadius, playerLayer);
        bool playerSeenThisFrame = false;

        if (hits.Length > 0)
        {
            Transform potentialTarget = hits[0].transform;

            // NEU: Ziel ist die Mitte des Player-Colliders, nicht der Fuß
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
                }
            }
        }

        if (!playerSeenThisFrame && currentAlertState == 2)
        {
            Vector3 lostPos = currentTarget != null ? currentTarget.position : transform.position;
            SetStateYellow(lostPos);
        }
    }

    public void HearNoise(Vector3 noisePosition)
    {
        // Reagiert nur auf Geräusche, wenn er nicht eh schon im Kampf ist
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

        if (behaviorTree != null)
        {
            SharedFloat timerVar = behaviorTree.GetVariable("SearchTimer") as SharedFloat;
            if (timerVar != null) timerVar.Value = 20f;
        }

        UpdateBlackboard();
    }

    void UpdateBlackboard()
    {
        if (behaviorTree == null) return;

        // 1. Wir holen uns die echte Variable aus dem Baum
        SharedInt alertVar = behaviorTree.GetVariable("AlertState") as SharedInt;
        // 2. Wir ändern ".Value" direkt. DAS ist das Klopfen, das den Abort sofort auslöst!
        if (alertVar != null) alertVar.Value = currentAlertState;

        SharedGameObject targetVar = behaviorTree.GetVariable("TargetEntity") as SharedGameObject;
        if (targetVar != null) targetVar.Value = currentTarget != null ? currentTarget.gameObject : null;

        SharedVector3 posVar = behaviorTree.GetVariable("InvestigationPos") as SharedVector3;
        if (posVar != null) posVar.Value = lastKnownPosition;
    }
}