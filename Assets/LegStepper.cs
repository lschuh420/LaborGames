using UnityEngine;
using System.Collections;

public class LegStepper : MonoBehaviour
{
    [Header("Zwingende Zuweisung!")]
    [Tooltip("Ziehe das Target für dieses Bein hier rein")]
    public Transform target; // JETZT PUBLIC! Keine Geister-Suchen mehr.

    private Vector3 stepStart;
    private Vector3 stepTarget;
    private Quaternion stepStartRot;
    private Quaternion stepTargetRot;

    private float stepHeight;
    private float stepDuration;
    private bool isStepping = false;

    [Header("Step Quality")]
    [SerializeField] private float minStepDistance = 0.05f;
    [SerializeField] private float endLockTime = 0.04f;
    [SerializeField] private bool usePreLift = true;
    [SerializeField] private float preLiftAmount = 0.04f;
    [SerializeField] private float preLiftPortion = 0.15f;

    [Header("Curves (Absturzsicher)")]
    [SerializeField] private AnimationCurve horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve verticalArc = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

    public void StartStep(Vector3 targetPos, Quaternion targetRot, float height, float duration)
    {
        if (isStepping) return;

        // Wenn kein Target da ist, brich ab, bevor das Skript crasht
        if (target == null)
        {
            Debug.LogError($"[LegStepper] {gameObject.name} HAT KEIN ZIEL ZUGEWIESEN! Bewegung blockiert.");
            return;
        }

        stepStart = target.position;
        stepTarget = targetPos;
        stepStartRot = target.rotation;
        stepTargetRot = targetRot;

        stepHeight = Mathf.Max(0.01f, height);
        stepDuration = Mathf.Max(0.05f, duration);

        Vector3 planar = stepTarget - stepStart;
        planar.y = 0f;

        if (planar.magnitude < minStepDistance && Quaternion.Angle(stepStartRot, stepTargetRot) < 5f) return;

        StopAllCoroutines();
        StartCoroutine(PerformStep());
    }

    IEnumerator PerformStep()
    {
        isStepping = true;
        float elapsed = 0f;
        float preLiftTime = usePreLift ? stepDuration * Mathf.Clamp01(preLiftPortion) : 0f;

        // Phase 1: Pre-Lift
        if (usePreLift && preLiftTime > 0.0001f)
        {
            Vector3 liftStart = stepStart;
            Vector3 liftEnd = stepStart + Vector3.up * preLiftAmount;

            while (elapsed < preLiftTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / preLiftTime);
                float e = t * t * (3f - 2f * t);
                target.position = Vector3.Lerp(liftStart, liftEnd, e);
                target.rotation = stepStartRot;
                yield return null;
            }
            stepStart = liftEnd;
        }

        // Phase 2: Swing
        float swingElapsed = 0f;
        float swingDuration = Mathf.Max(0.01f, stepDuration - preLiftTime);

        while (swingElapsed < swingDuration)
        {
            swingElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(swingElapsed / swingDuration);

            float h = Mathf.Clamp01(horizontalEase.Evaluate(t));
            float v = Mathf.Clamp01(verticalArc.Evaluate(t));

            Vector3 horizontalPos = Vector3.Lerp(stepStart, stepTarget, h);
            Vector3 newPos = horizontalPos + Vector3.up * (v * stepHeight);
            Quaternion newRot = Quaternion.Slerp(stepStartRot, stepTargetRot, t);

            target.position = newPos;
            target.rotation = newRot;
            yield return null;
        }

        // Phase 3: Lock
        if (endLockTime > 0.001f)
        {
            Vector3 lockStart = target.position;
            Quaternion lockStartRot = target.rotation;
            float lockElapsed = 0f;

            while (lockElapsed < endLockTime)
            {
                lockElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(lockElapsed / endLockTime);
                float e = t * t * (3f - 2f * t);

                target.position = Vector3.Lerp(lockStart, stepTarget, e);
                target.rotation = Quaternion.Slerp(lockStartRot, stepTargetRot, e);
                yield return null;
            }
        }

        // Finale Position erzwingen
        target.position = stepTarget;
        target.rotation = stepTargetRot;
        isStepping = false;
    }

    public bool IsStepping() => isStepping;
}