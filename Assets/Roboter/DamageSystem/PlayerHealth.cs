using UnityEngine;

// Das ": MonoBehaviour, IDamageable" ist die absolute Magie hier!
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // Hier wird das Interface erfüllt
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"<color=red>AUA! Spieler frisst {damageAmount} Schaden. Aktuelle HP: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("<color=darkred>WASTED! Spieler ist tot!</color>");
        // Hier kommt später der Game-Over-Screen oder Ragdoll rein
    }
}