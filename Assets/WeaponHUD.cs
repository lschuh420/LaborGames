using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponHUD : MonoBehaviour
{
    [Header("Slot Images")]
    public Image slot1Image;
    public Image slot2Image;

    [Header("Weapon Name Labels")]
    public TextMeshProUGUI weaponName1;
    public TextMeshProUGUI weaponName2;

    [Header("Slot Number Labels")]
    public TextMeshProUGUI slotNumber1;
    public TextMeshProUGUI slotNumber2;

    [Header("Ammo")]
    [Tooltip("Shows the active weapon's ammo, e.g. 12/∞.")]
    public TextMeshProUGUI ammoText;
    public Color ammoNormalColor = Color.white;
    [Tooltip("Color used when the magazine is empty or reloading.")]
    public Color ammoLowColor = new Color(1f, 0.3f, 0.2f, 1f);

    [Header("Reload Indicator")]
    [Tooltip("A radial Filled Image. Its fill goes 0 -> 1 as the reload completes.")]
    public Image reloadCircle;

    [Header("Colors")]
    public Color activeColor = new Color(1f, 0.6f, 0f, 1f);
    public Color inactiveColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    public Color emptyColor = new Color(0.08f, 0.08f, 0.08f, 0.6f);

    private PlayerShooting playerShooting;
    private int lastActiveSlot = -1;

    void Start()
    {
        playerShooting = FindObjectOfType<PlayerShooting>();
        UpdateHUD();
    }

    void Update()
    {
        // Only update when slot changes for performance
        if (playerShooting != null && playerShooting.activeSlot != lastActiveSlot)
        {
            lastActiveSlot = playerShooting.activeSlot;
            UpdateHUD();
        }

        // Always update weapon names in case pickup happened
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (playerShooting == null) return;
        UpdateSlot(0, slot1Image, weaponName1, slotNumber1);
        UpdateSlot(1, slot2Image, weaponName2, slotNumber2);
        UpdateAmmo();
        UpdateReloadCircle();
    }

    void UpdateReloadCircle()
    {
        if (reloadCircle == null) return;

        bool reloading = playerShooting.IsReloading;

        // Only show the circle while reloading
        reloadCircle.gameObject.SetActive(reloading);

        if (reloading)
            reloadCircle.fillAmount = playerShooting.ReloadProgress; // 0 -> 1
    }

    void UpdateAmmo()
    {
        if (ammoText == null) return;

        WeaponData data = playerShooting.ActiveWeaponData;
        if (data == null)
        {
            ammoText.text = "";
            return;
        }

        // Hide the number while reloading — the circle is the only indicator then.
        if (playerShooting.IsReloading)
        {
            ammoText.text = "";
            return;
        }

        // ∞ is the infinity symbol (∞)
        ammoText.text = data.currentAmmo + "/∞";
        ammoText.color = data.currentAmmo > 0 ? ammoNormalColor : ammoLowColor;
    }

    void UpdateSlot(int slot, Image slotImage, TextMeshProUGUI nameLabel, TextMeshProUGUI numberLabel)
    {
        bool isActive = playerShooting.activeSlot == slot;
        bool hasWeapon = playerShooting.equippedWeapons[slot] != null;

        // Slot background color
        if (isActive && hasWeapon)
            slotImage.color = activeColor;
        else if (hasWeapon)
            slotImage.color = inactiveColor;
        else
            slotImage.color = emptyColor;

        // Weapon name
        if (hasWeapon)
        {
            WeaponData data = playerShooting.equippedWeapons[slot]
                              .GetComponent<WeaponData>();
            nameLabel.text = data != null ? data.weaponName : "Gun";
            nameLabel.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            nameLabel.text = "EMPTY";
            nameLabel.color = new Color(0.4f, 0.4f, 0.4f);
        }

        // Slot number color
        if (numberLabel != null)
            numberLabel.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }
}