using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Slots")]
    public GameObject[] equippedWeapons = new GameObject[2];
    public int activeSlot = 0;

    [Header("Weapon Holders")]
    public Transform rightHandHolder;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 40f;

    [Header("Shooting")]
    public float shootDistance = 100f;

    private Camera playerCamera;
    private float fireCooldown = 0f;
    private WeaponIK weaponIK;

    void Start()
    {
        playerCamera = Camera.main;
        weaponIK = GetComponent<WeaponIK>();
    }

    void Update()
    {
        // Count down cooldown
        if (fireCooldown > 0f)
            fireCooldown -= Time.deltaTime;

        // Switch weapons with Q or Tab
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab))
            SwitchWeapon();

        // Shoot with left click — only if cooldown is done
        if (Input.GetMouseButtonDown(0) && fireCooldown <= 0f)
            Shoot();
    }

    public void PickUpWeapon(GameObject weaponPrefab)
    {
        int slot = -1;
        for (int i = 0; i < equippedWeapons.Length; i++)
        {
            if (equippedWeapons[i] == null) { slot = i; break; }
        }
        if (slot == -1) slot = activeSlot;

        if (equippedWeapons[slot] != null)
            Destroy(equippedWeapons[slot]);

        equippedWeapons[slot] = Instantiate(weaponPrefab, rightHandHolder);

        // Disable collider so it doesn't re-trigger pickup
        Collider col = equippedWeapons[slot].GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Disable WeaponPickup script on the spawned gun
        WeaponPickup pickup = equippedWeapons[slot].GetComponent<WeaponPickup>();
        if (pickup != null) pickup.enabled = false;

        RefreshWeaponVisibility();
        Debug.Log("Picked up: " + weaponPrefab.name + " in slot " + slot);

        if (weaponIK != null)
        {
            Transform leftHandTarget = equippedWeapons[slot].transform.Find("LeftHandTarget");
            if (leftHandTarget == null)
                Debug.LogError("LeftHandTarget NOT FOUND on " + equippedWeapons[slot].name);
            else
                Debug.Log("LeftHandTarget found at: " + leftHandTarget.position);
            weaponIK.leftHandTarget = leftHandTarget;
            weaponIK.leftHandWeight = 1f;
        }
    }

    void SwitchWeapon()
    {
        activeSlot = (activeSlot + 1) % equippedWeapons.Length;
        RefreshWeaponVisibility();
        Debug.Log("Switched to slot: " + activeSlot);
    }

    void RefreshWeaponVisibility()
    {
        for (int i = 0; i < equippedWeapons.Length; i++)
        {
            if (equippedWeapons[i] != null)
                equippedWeapons[i].SetActive(i == activeSlot);
        }
    }

    void Shoot()
    {
        if (equippedWeapons[activeSlot] == null) return;

        // Get fire rate from WeaponData
        WeaponData data = equippedWeapons[activeSlot].GetComponent<WeaponData>();
        int damage = 50; // Unser neuer Standard-Wumms
        
        if (data != null)
        {
            damage = data.damage;
            Debug.Log($"<color=blue>[PlayerShooting] WeaponData gefunden auf '{equippedWeapons[activeSlot].name}'. Damage aus Script: {damage}</color>");
        }
        else
        {
            Debug.Log($"<color=cyan>[PlayerShooting] KEIN WeaponData auf '{equippedWeapons[activeSlot].name}' gefunden. Nutze Fallback: {damage}</color>");
        }

        float fireRate = data != null ? data.fireRate : 0.2f;
        fireCooldown = fireRate;

        if (bulletPrefab == null) return;

        // Aim bullet toward where camera is looking
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        RaycastHit hit;
        // Use layermask to ignore the player itself
        int layerMask = ~LayerMask.GetMask("Player");
        if (Physics.Raycast(ray, out hit, shootDistance, layerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(shootDistance);

        // Spawn bullet at hand position, aimed at target
        Vector3 direction = (targetPoint - rightHandHolder.position).normalized;
        GameObject bullet = Instantiate(
            bulletPrefab,
            rightHandHolder.position,
            Quaternion.LookRotation(direction)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
            bulletScript.speed = bulletSpeed;
        }

        // ====================================================================
        // --- NEU: SOUND-MELDUNG AN DEN ROBOTER SCHICKEN ---
        // ====================================================================
        MechSensor robotSensor = FindObjectOfType<MechSensor>();
        if (robotSensor != null)
        {
            // Schickt die aktuelle Position des Spielers an das Gehör des Roboters
            robotSensor.HearNoise(transform.position);
        }
        // ====================================================================
    }
}