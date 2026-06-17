using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Low-Health-Screeneffekt (URP): unter dem Schwellwert verliert das Bild
// Sättigung und bekommt eine rote Vignette. Erstellt sein eigenes globales
// Volume zur Laufzeit -> kein manuelles Setup im Editor nötig.
[RequireComponent(typeof(Volume))]
public class LowHealthEffect : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Trigger")]
    [Tooltip("Effekt beginnt, sobald HP <= diesem Wert sind")]
    public int healthThreshold = 20;

    [Header("Look at full effect (0 HP)")]
    [Tooltip("Sättigung bei 0 HP (-100 = komplett grau)")]
    public float minSaturation = -80f;
    [Tooltip("Stärke der roten Vignette bei 0 HP")]
    public float maxVignette = 0.5f;
    public Color vignetteColor = new Color(0.6f, 0f, 0f);

    [Header("Smoothing")]
    public float fadeSpeed = 4f;

    private Volume volume;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private float currentIntensity; // 0 = gesund, 1 = fast tot

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        // Eigenes globales Volume + Profil zur Laufzeit aufbauen
        volume = GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f; // über anderen Volumes
        volume.weight = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;

        vignette = profile.Add<Vignette>(true);
        vignette.color.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.color.value = vignetteColor;
        vignette.intensity.value = 0f;
    }

    private void Update()
    {
        if (playerHealth == null) return;

        // Zielintensität: 0 bei >= Schwellwert, 1 bei 0 HP
        float target = 0f;
        if (playerHealth.currentHealth < healthThreshold && healthThreshold > 0)
            target = 1f - Mathf.Clamp01((float)playerHealth.currentHealth / healthThreshold);

        currentIntensity = Mathf.MoveTowards(currentIntensity, target, fadeSpeed * Time.deltaTime);

        colorAdjustments.saturation.value = Mathf.Lerp(0f, minSaturation, currentIntensity);
        vignette.intensity.value = Mathf.Lerp(0f, maxVignette, currentIntensity);
    }
}
