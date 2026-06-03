using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Mech Custom")]
[TaskDescription("Kugelsichere State-Machine. Blockiert Behavior Tree Resets.")]
public class RobotCombatMaster : Action
{
    public SharedGameObject targetEntity;
    public float meleeRange = 10f;
    public float chargeTime = 1.5f;
    public float novaCooldownTime = 2.5f;

    public Transform eyesPivot;
    public LayerMask obstacleLayer;

    private NavMeshAgent agent;
    private MechBossHealth bossHealth; // Referenz für Stun/Limp Check
    private MechDefenseField defenseField;
    private MechWeapon[] weapons;
    private TurretController turret;

    // Unsere kugelsicheren Speicher-Variablen
    private int currentState = 0; // 0 = Jagen/Schießen, 1 = Aufladen, 2 = Nova explodiert (Cooldown)
    private float nextActionTime = 0f;
    private float originalAgentSpeed;
    private bool speedReduced = false;

    public override void OnAwake()
    {
        agent = Owner.GetComponent<NavMeshAgent>();
        bossHealth = Owner.GetComponent<MechBossHealth>();
        defenseField = Owner.GetComponentInChildren<MechDefenseField>();
        weapons = Owner.GetComponentsInChildren<MechWeapon>();
        turret = Owner.GetComponentInChildren<TurretController>();

        if (agent != null) originalAgentSpeed = agent.speed;
    }

    public override TaskStatus OnUpdate()
    {
        if (targetEntity.Value == null) return TaskStatus.Failure;
        float distance = Vector3.Distance(Owner.transform.position, targetEntity.Value.transform.position);

        // --------------------------------------------------------
        // CHECK: CRITICAL STATE (Limping)
        // --------------------------------------------------------
        if (bossHealth != null && bossHealth.IsLimping && !speedReduced)
        {
            if (agent != null)
            {
                agent.speed = originalAgentSpeed * 0.4f; // 60% langsamer
                speedReduced = true;
                Debug.Log("<color=red>[RobotMaster] Mech humpelt! Speed reduziert.</color>");
            }
        }

        // --------------------------------------------------------
        // ZUSTAND 1 & 2: WARTEN (Aufladen oder Cooldown läuft)
        // --------------------------------------------------------
        if (currentState == 1 || currentState == 2)
        {
            if (Time.time < nextActionTime)
            {
                // Zeit läuft noch ab...
                if (agent != null) agent.isStopped = true;
                if (turret != null) turret.target = null;
                return TaskStatus.Running;
            }
            else
            {
                // Timer ist fertig!
                if (currentState == 1)
                {
                    // Aufladen fertig -> NOVA ZÜNDEN!
                    if (defenseField != null) defenseField.TriggerField();

                    // Direkt in den Cooldown wechseln
                    currentState = 2;
                    nextActionTime = Time.time + novaCooldownTime;
                    return TaskStatus.Running;
                }
                else if (currentState == 2)
                {
                    // Cooldown fertig -> Wieder auf den Spieler losgehen
                    currentState = 0;
                }
            }
        }

        // --------------------------------------------------------
        // ZUSTAND 0: NORMALER KAMPF (Jagen und Ballern)
        // --------------------------------------------------------
        if (currentState == 0)
        {
            // Wenn Spieler in Reichweite -> AUFLADEN STARTEN! (Auch im Stun möglich!)
            if (distance <= meleeRange)
            {
                Debug.Log("<color=orange>[RobotMaster] Spieler zu nah! Lade Nova auf...</color>");
                currentState = 1; // Wechsel in den Auflade-Zustand
                nextActionTime = Time.time + chargeTime;

                if (agent != null) agent.isStopped = true;
                if (turret != null) turret.target = null;
                return TaskStatus.Running;
            }

            // STUN CHECK: Bewegung stoppen, aber Schießen/Zielen bleibt!
            bool isStunned = bossHealth != null && bossHealth.IsStunned;

            // Ansonsten: Jagen (nur wenn nicht gestunned)
            if (agent != null)
            {
                agent.isStopped = isStunned;
                if (!isStunned)
                {
                    agent.SetDestination(targetEntity.Value.transform.position);
                }
            }

            // Turm zielen lassen
            if (turret != null) turret.target = targetEntity.Value.transform;

            // Schießen (mit Sicht-Check)
            Vector3 targetPos = targetEntity.Value.transform.position + Vector3.up * 1.0f;
            Vector3 startPos = eyesPivot != null ? eyesPivot.position : Owner.transform.position + Vector3.up * 1.5f;

            if (!Physics.Linecast(startPos, targetPos, obstacleLayer))
            {
                if (weapons != null)
                {
                    foreach (MechWeapon weapon in weapons) weapon.FireAtTarget();
                }
            }
        }

        return TaskStatus.Running;
    }
}
