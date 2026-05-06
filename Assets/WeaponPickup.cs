using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public GameObject weaponPrefab;
    private bool pickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other.gameObject);
    }

    // This catches CharacterController collisions
    private void OnTriggerStay(Collider other)
    {
        TryPickup(other.gameObject);
    }

    private void TryPickup(GameObject other)
    {
        if (pickedUp) return;

        PlayerShooting shooting = other.GetComponentInParent<PlayerShooting>();

        if (shooting == null)
            shooting = other.GetComponent<PlayerShooting>();

        if (shooting != null)
        {
            pickedUp = true;
            shooting.PickUpWeapon(weaponPrefab);
            Destroy(gameObject);
        }
    }
}