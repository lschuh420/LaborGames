using UnityEngine;

public class DamageRouter : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    public MechBossHealth mainHealth;
    public float damageMultiplier = 1.0f; // 0.5 für Panzerung, 1.5-2.0 für Schwachstellen

    [Header("Optional")]
    public MechLegHealth legHealth; // Falls es ein Bein ist

    public void TakeDamage(int damageAmount)
    {
        int finalDamage = Mathf.RoundToInt(damageAmount * damageMultiplier);
        Debug.Log($"<color=orange>[DamageRouter] Hit: {gameObject.name}. Base: {damageAmount}, Mult: {damageMultiplier}, Final: {finalDamage}</color>");
        
        // 1. Schaden an den Boss weitergeben
        if (mainHealth != null)
        {
            mainHealth.TakeDamage(finalDamage);
        }

        // 2. Falls es ein Bein ist, dort auch Schaden machen
        if (legHealth != null)
        {
            legHealth.TakeLegDamage(damageAmount); // Lokaler Schaden meist ohne Multiplier oder separat?
            // Laut Anforderung: "Jeglicher Schaden wird an das zentrale MechBossHealth-Skript weitergeleitet"
            // Bein-Zerstörung hat aber eigene 300 HP.
        }
    }
}
