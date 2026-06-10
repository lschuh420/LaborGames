using UnityEngine;

// Jedes Skript, das dieses Interface erbt, MUSS eine TakeDamage-Methode haben!
public interface IDamageable
{
    void TakeDamage(int damageAmount);
}