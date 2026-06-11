# Robot Leg Destruction & Movement Scaling Implementation Plan

> **For Gemini:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a realistic, "Arc Raiders" style behavior where destroying a robot's leg causes a temporary collapse (stagger/stun) and permanently reduces its movement speed based on the number of remaining legs.

**Architecture:**
- **Stun Logic:** Update `MechBossHealth` to trigger a stun/collapse event every time a leg is destroyed.
- **Speed Scaling:** Modify `MechDriver` to listen for leg destruction and scale `maxSpeed` down based on the `destroyedLegsCount`.
- **UI/Feedback:** Ensure the health bar and existing systems handle these transitions smoothly.

**Tech Stack:** C#, Unity, Action Events.

---

### Task 1: Update MechBossHealth for Staggering

**Files:**
- Modify: `Assets/Roboter/DamageSystem/MechBossHealth.cs`

**Step 1: Modify TriggerMechStun logic**
The existing `ReportLegDestroyed` only stuns on the first leg. We want it to trigger a stagger/stun on *every* leg destruction.

```csharp
// Around line 70 in MechBossHealth.cs
public void ReportLegDestroyed()
{
    destroyedLegsCount++;
    Debug.Log($"<color=orange>[MechBoss] Bein zerstört! Insgesamt: {destroyedLegsCount}</color>");

    // JEDES Bein löst jetzt einen kurzen Stun/Stagger aus
    IsStunned = true;
    
    // Stun Sound abspielen
    if (audioSource != null && stunSound != null)
    {
        audioSource.PlayOneShot(stunSound, 0.8f);
    }

    // Event für andere Systeme (z.B. Animation/AI)
    OnMechStun?.Invoke(2); // 2 Sekunden Stun für jeden Beinfreier
    
    if (destroyedLegsCount >= 2)
    {
        IsLimping = true;
        OnMechLimp?.Invoke();
    }

    // Reset nach Zeit
    CancelInvoke(nameof(ResetStun)); // Falls schon einer läuft
    Invoke(nameof(ResetStun), 2f);
}
```

**Step 2: Commit**
```bash
git add Assets/Roboter/DamageSystem/MechBossHealth.cs
git commit -m "feat: stagger robot on every leg destruction"
```

### Task 2: Implement Speed Scaling in MechDriver

**Files:**
- Modify: `Assets/Roboter/MechDriver.cs`

**Step 1: Add speed reduction logic**
We need to track destroyed legs and reduce `maxSpeed`.

```csharp
// MechDriver.cs additions

[Header("Leg Destruction Scaling")]
[SerializeField] private float speedReductionPerLeg = 0.5f; // 3.5 -> 3.0 -> 2.5 ...
[SerializeField] private float criticalSpeedThreshold = 3; // Ab 3 zerstörten Beinen wird es sehr langsam

private int destroyedLegs = 0;
private float baseMaxSpeed;

void Start() {
    baseMaxSpeed = maxSpeed;
    // ... existing start code
    
    // Listen to leg destruction
    // Note: We need a way to get the MechBossHealth reference
    MechBossHealth health = GetComponentInParent<MechBossHealth>();
    if (health != null) {
        // We'll need a new event or just check the count in Update
    }
}

// Better approach: Update maxSpeed when legs are destroyed
public void UpdateSpeedFactor(int destroyedCount) {
    destroyedLegs = destroyedCount;
    
    // 6 Beine total (Annahme). Bei 3 zerstörten Beinen (Arc Raiders Style)
    // wird er extrem langsam.
    float factor = 1f - (destroyedCount * 0.15f); // 15% weniger pro Bein
    
    if (destroyedCount >= 3) {
        factor *= 0.5f; // Extra Malus ab 3 Beinen
    }
    
    maxSpeed = baseMaxSpeed * Mathf.Max(0.1f, factor);
    Debug.Log($"[MechDriver] Legs destroyed: {destroyedCount}. New MaxSpeed: {maxSpeed}");
}
```

**Step 2: Connect MechBossHealth to MechDriver**
Update `MechBossHealth` to notify `MechDriver`.

**Step 3: Commit**
```bash
git add Assets/Roboter/MechDriver.cs Assets/Roboter/DamageSystem/MechBossHealth.cs
git commit -m "feat: scale movement speed based on destroyed legs"
```

### Task 3: Movement Disability (3+ Legs)

**Step 1: Stop movement if too many legs are gone**
In `MechDriver.UpdateMovementLogic`, if `destroyedLegs >= 4`, make it struggle significantly (maybe stuttering rotation or very low speed).

**Step 2: Commit**
```bash
git commit -m "feat: implement mobility crawl for crippled robot"
```
