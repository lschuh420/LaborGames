using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StepManager : MonoBehaviour
{
    public System.Action<float> OnStepLanded;

    public enum GaitPattern
    {
        Wave,     // Jedes Bein einzeln (Extrem langsam & wuchtig)
        Tripod,   // 3 Beine gleichzeitig (Schnell & agil)
        Tetrapod  // 2 Beine diagonal (Perfekt für "Bastion")
    }

    [System.Serializable]
    public class LegStepData
    {
        public string name;
        public Transform legRoot;
        public LegStepper legStepper;
        public Vector3 restPositionOffset;
        public Transform targetTransform;
        public bool isSteppingActive;
        public float lastStepTime;
        public float currentUrgency;
    }

    [Header("Hierarchy Links")]
    [SerializeField] private Transform legsContainer;
    [SerializeField] private Transform targetsContainer;

    [Header("Leg References")]
    [SerializeField] private List<LegStepData> legs = new List<LegStepData>();

    [Header("Robotic Gait Sequencer")]
    public GaitPattern selectedGait = GaitPattern.Tetrapod;
    private int currentGaitStep = 0;

    [Header("Gait Control & Timing")]
    [Tooltip("Maximale Beine, die gleichzeitig in der Luft sein dürfen")]
    [SerializeField] private int maxSimultaneousSteps = 2;
    [Tooltip("PLANARE Distanz: Ab wann greift das Bein nach vorne?")]
    [SerializeField] private float stepThreshold = 0.5f;
    [Tooltip("Zeitlicher Mindestabstand zwischen Starts (Staggering)")]
    [SerializeField] private float minStaggerDelay = 0.15f;

    [Header("Step Animation")]
    [SerializeField] private float stepHeight = 0.65f;
    [SerializeField] private float stepSpeed = 6.0f;
    [SerializeField] private float minStepDuration = 0.25f;
    [SerializeField] private float maxStepDuration = 0.8f;

    [Header("Panic & Stress")]
    [SerializeField] private float panicDistance = 3.0f;
    [Tooltip("Read-Only: Wird vom MechDriver ausgelesen")]
    public float currentSystemStress = 0f;

    [Header("Curve Prediction & Reach")]
    [SerializeField] private float velocitySmoothTime = 0.15f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField, Range(0.1f, 1.0f)] private float predictionSeconds = 0.2f;
    [Tooltip("Multiplikator NUR für Vorderbeine (Index 0 und 3). 1 = normal, 1.5 = greifen 50% weiter vor.")]
    [SerializeField, Range(1.0f, 3.0f)] private float frontLegReachMultiplier = 1.5f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float rayStartHeight = 3f;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float footGroundClearance = 0.05f;

    private Vector3 lastBodyPosition;
    private Quaternion lastBodyRotation;
    private Vector3 smoothedVel;
    private Vector3 velRef;
    private float smoothedYawSpeed;
    private float yawRef;

    private float startTime;
    private float lastStepStartTime;

    void Start()
    {
        InitializeLegData();

        if (targetsContainer != null) targetsContainer.SetParent(null);

        SnapAllLegsToGround();
        lastBodyPosition = transform.position;
        lastBodyRotation = transform.rotation;
        startTime = Time.time;
    }

    void Update()
    {
        UpdateMotionSmoothing();
        UpdateLegUrgency();
        EvaluatePanicSteps();
        ProcessStepQueue();

        lastBodyPosition = transform.position;
        lastBodyRotation = transform.rotation;
    }

    void InitializeLegData()
    {
        if (legsContainer == null) legsContainer = transform.Find("Mech_Body_Logical/Legs_Container");
        if (targetsContainer == null) targetsContainer = GameObject.Find("IK_Targets_Container")?.transform;
    }

    void SnapAllLegsToGround()
    {
        for (int i = 0; i < legs.Count; i++)
        {
            Vector3 idealRest = GetIdealRestPosition(i);
            GetGroundInfo(idealRest, i, out Vector3 restPos, out Quaternion restRot);

            legs[i].targetTransform.position = restPos;
            legs[i].targetTransform.rotation = restRot;

            if (legs[i].legStepper != null)
                legs[i].legStepper.StartStep(restPos, restRot, 0f, 0f);
        }
    }

    void UpdateMotionSmoothing()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 rawVel = (transform.position - lastBodyPosition) / dt;
        smoothedVel = Vector3.SmoothDamp(smoothedVel, rawVel, ref velRef, velocitySmoothTime);

        float yawDelta = Mathf.DeltaAngle(lastBodyRotation.eulerAngles.y, transform.eulerAngles.y);
        smoothedYawSpeed = Mathf.SmoothDampAngle(smoothedYawSpeed, yawDelta / dt, ref yawRef, rotationSmoothTime);
    }

    float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    void UpdateLegUrgency()
    {
        float maxDist = 0f;
        for (int i = 0; i < legs.Count; i++)
        {
            if (legs[i].isSteppingActive)
            {
                legs[i].currentUrgency = 0f;
                continue;
            }

            Vector3 idealRest = GetIdealRestPosition(i);
            float dist = GetPlanarDistance(legs[i].targetTransform.position, idealRest);

            legs[i].currentUrgency = dist;
            if (dist > maxDist) maxDist = dist;
        }

        currentSystemStress = Mathf.InverseLerp(stepThreshold, panicDistance, maxDist);
    }

    void EvaluatePanicSteps()
    {
        for (int i = 0; i < legs.Count; i++)
        {
            if (legs[i].isSteppingActive) continue;
            Vector3 idealRest = GetIdealRestPosition(i);

            if (GetPlanarDistance(legs[i].targetTransform.position, idealRest) > panicDistance)
            {
                CalculateNextStepTarget(i, out Vector3 nextPos, out Quaternion nextRot);
                TryExecuteStep(i, nextPos, nextRot);
            }
        }
    }

    int[][] GetCurrentGaitArray()
    {
        switch (selectedGait)
        {
            case GaitPattern.Tripod:
                return new int[][] {
                    new int[] { 0, 4, 2 },
                    new int[] { 3, 1, 5 }
                };
            case GaitPattern.Wave:
                return new int[][] {
                    new int[] { 2 }, new int[] { 1 }, new int[] { 0 },
                    new int[] { 5 }, new int[] { 4 }, new int[] { 3 }
                };
            case GaitPattern.Tetrapod:
            default:
                return new int[][] {
                    new int[] { 0, 5 },
                    new int[] { 1, 3 },
                    new int[] { 2, 4 }
                };
        }
    }

    void ProcessStepQueue()
    {
        if (Time.time - startTime < 0.5f) return;
        if (GetActiveStepCount() >= maxSimultaneousSteps) return;
        if (Time.time - lastStepStartTime < minStaggerDelay) return;

        int[][] currentPattern = GetCurrentGaitArray();
        if (currentGaitStep >= currentPattern.Length) currentGaitStep = 0;

        int[] legsInCurrentStep = currentPattern[currentGaitStep];
        bool groupReadyToStep = false;

        foreach (int legIndex in legsInCurrentStep)
        {
            if (legs[legIndex].isSteppingActive) continue;

            if (legs[legIndex].currentUrgency > stepThreshold)
            {
                groupReadyToStep = true;
                break;
            }
        }

        if (groupReadyToStep)
        {
            foreach (int legIndex in legsInCurrentStep)
            {
                if (!legs[legIndex].isSteppingActive)
                {
                    CalculateNextStepTarget(legIndex, out Vector3 nextPos, out Quaternion nextRot);
                    TryExecuteStep(legIndex, nextPos, nextRot);
                }
            }

            lastStepStartTime = Time.time;
            currentGaitStep = (currentGaitStep + 1) % currentPattern.Length;
        }
    }

    bool TryExecuteStep(int legIndex, Vector3 stepPos, Quaternion stepRot)
    {
        LegStepData leg = legs[legIndex];

        float distance = Vector3.Distance(leg.targetTransform.position, stepPos);
        float dynamicDuration = Mathf.Clamp(distance / stepSpeed, minStepDuration, maxStepDuration);

        leg.legStepper.StartStep(stepPos, stepRot, stepHeight, dynamicDuration);
        leg.isSteppingActive = true;

        StartCoroutine(WaitForStepCompletion(legIndex, dynamicDuration));
        return true;
    }

    void CalculateNextStepTarget(int legIndex, out Vector3 outPos, out Quaternion outRot)
    {
        Vector3 planarVel = new Vector3(smoothedVel.x, 0f, smoothedVel.z);

        // --- DIE PROFI-LÖSUNG: Asymmetrische Prädiktion ---
        float currentPrediction = predictionSeconds;

        // Index 0 = FL (Vorne Links), Index 3 = FR (Vorne Rechts)
        if (legIndex == 0 || legIndex == 3)
        {
            currentPrediction *= frontLegReachMultiplier;
        }
        // --------------------------------------------------

        Vector3 futureBodyPos = transform.position + (planarVel * currentPrediction);

        float futureYawOffset = smoothedYawSpeed * currentPrediction;
        Quaternion futureBodyRot = transform.rotation * Quaternion.Euler(0f, futureYawOffset, 0f);

        Vector3 idealFutureRest = futureBodyPos + (futureBodyRot * legs[legIndex].restPositionOffset);

        GetGroundInfo(idealFutureRest, legIndex, out outPos, out outRot);
    }

    Vector3 GetIdealRestPosition(int legIndex)
    {
        return transform.position + transform.rotation * legs[legIndex].restPositionOffset;
    }

    void GetGroundInfo(Vector3 p, int legIndex, out Vector3 outPos, out Quaternion outRot)
    {
        if (Physics.Raycast(p + Vector3.up * rayStartHeight, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            outPos = hit.point + Vector3.up * footGroundClearance;
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            outRot = Quaternion.LookRotation(forwardOnPlane, hit.normal);
        }
        else
        {
            outPos = p;
            outPos.y = legs[legIndex].targetTransform.position.y;
            outRot = transform.rotation;
        }
    }

    IEnumerator WaitForStepCompletion(int index, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (index < legs.Count)
        {
            legs[index].isSteppingActive = false;
            OnStepLanded?.Invoke(1.0f);
        }
    }

    int GetActiveStepCount() { int c = 0; foreach (var l in legs) if (l.isSteppingActive) c++; return c; }
}