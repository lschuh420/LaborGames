using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Erzeugt automatisch eine schwebende Lebensleiste über dem Roboter.
/// Kann einfach an das Roboter-Prefab oder ein Objekt mit MechBossHealth gehängt werden.
/// </summary>
public class RobotFloatingHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MechBossHealth bossHealth;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5.5f, 0); // Etwas höher
    [SerializeField] private Vector2 size = new Vector2(3f, 0.12f); // Schmaler (0.12 statt 0.3)
    [SerializeField] private Color healthColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color backBarColor = new Color(1f, 1f, 1f, 0.8f); // Weißlich für Effekt
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.6f);

    [Header("Animation")]
    [SerializeField] private float catchUpSpeed = 2f;
    [SerializeField] private float catchUpDelay = 0.5f;

    private RectTransform frontRect;
    private RectTransform backRect;
    private GameObject canvasObj;
    private Transform mainCameraTransform;
    private float delayTimer;
    private float currentTargetFill = 1f;

    private void Start()
    {
        if (bossHealth == null)
            bossHealth = GetComponentInParent<MechBossHealth>();

        if (bossHealth == null)
        {
            Debug.LogError("[RobotFloatingHealthBar] MechBossHealth NICHT gefunden!");
            enabled = false;
            return;
        }

        mainCameraTransform = Camera.main != null ? Camera.main.transform : null;

        CreateHealthBar();
        
        bossHealth.OnHealthChanged += HandleHealthChanged;
        
        // Initialer Stand
        float initialFill = bossHealth.maxHealth > 0 ? (float)bossHealth.currentHealth / bossHealth.maxHealth : 1f;
        UpdateVisuals(initialFill, true);
        Debug.Log($"[RobotHealthBar] Initialisiert mit {bossHealth.currentHealth}/{bossHealth.maxHealth} (Fill: {initialFill})");
    }

    private void CreateHealthBar()
    {
        canvasObj = new GameObject("HealthBar_WorldCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;

        // 1. Hintergrund
        CreateUIElement("Background", canvasObj.transform, backgroundColor, Vector2.zero, Vector2.one);

        // 2. Weiße Leiste (Back)
        GameObject backObj = CreateUIElement("BackFill", canvasObj.transform, backBarColor, new Vector2(0.01f, 0.1f), new Vector2(0.99f, 0.9f));
        backRect = backObj.GetComponent<RectTransform>();

        // 3. Rote Leiste (Front)
        GameObject frontObj = CreateUIElement("FrontFill", canvasObj.transform, healthColor, new Vector2(0.01f, 0.1f), new Vector2(0.99f, 0.9f));
        frontRect = frontObj.GetComponent<RectTransform>();
    }

    private GameObject CreateUIElement(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        return obj;
    }

    private void LateUpdate()
    {
        if (canvasObj != null && mainCameraTransform != null)
        {
            canvasObj.transform.LookAt(canvasObj.transform.position + mainCameraTransform.forward);
        }

        // Weiß zieht nach
        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else if (backRect != null && frontRect != null)
        {
            if (backRect.anchorMax.x > frontRect.anchorMax.x)
            {
                float newX = Mathf.Lerp(backRect.anchorMax.x, frontRect.anchorMax.x, Time.deltaTime * catchUpSpeed);
                backRect.anchorMax = new Vector2(newX, backRect.anchorMax.y);
            }
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        float targetFill = Mathf.Clamp01((float)current / max);
        Debug.Log($"[RobotHealthBar] Update erhalten: {current}/{max} -> Fill: {targetFill}");
        UpdateVisuals(targetFill, false);
    }

    private void UpdateVisuals(float targetFill, bool instant)
    {
        if (frontRect == null) return;

        // Wenn Schaden (Fill sinkt)
        if (targetFill < frontRect.anchorMax.x && !instant)
        {
            frontRect.anchorMax = new Vector2(targetFill, frontRect.anchorMax.y);
            delayTimer = catchUpDelay;
        }
        else
        {
            frontRect.anchorMax = new Vector2(targetFill, frontRect.anchorMax.y);
            if (instant && backRect != null)
                backRect.anchorMax = new Vector2(targetFill, backRect.anchorMax.y);
        }
    }

    private void OnDestroy()
    {
        if (bossHealth != null) bossHealth.OnHealthChanged -= HandleHealthChanged;
    }
}
