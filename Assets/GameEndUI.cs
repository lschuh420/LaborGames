using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Baut die komplette End-Screen-UI zur Laufzeit auf (kein manuelles Editor-Setup nötig,
// gleiche Philosophie wie LowHealthEffect).
//
// - Spieler stirbt  (PlayerHealth.OnPlayerDeath) -> "GAME OVER" + Play-Again-Button
// - Roboter zerstört (MechBossHealth.OnMechDeath) -> "#1 VICTORY ROYALE" + Play-Again-Button
//
// Einfach an ein leeres GameObject in der Szene hängen. Fertig.
public class GameEndUI : MonoBehaviour
{
    [Header("Behaviour")]
    [Tooltip("Spiel beim End-Screen pausieren (Time.timeScale = 0)")]
    public bool pauseGameOnEnd = true;
    [Tooltip("Cursor beim End-Screen sichtbar machen")]
    public bool showCursorOnEnd = true;

    [Header("Audio (optional)")]
    public AudioClip gameOverSound;
    public AudioClip victorySound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    private Canvas canvas;
    private GameObject gameOverPanel;
    private GameObject victoryPanel;
    private AudioSource audioSource;
    private bool gameEnded = false;

    private void Awake()
    {
        BuildUI();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += ShowGameOver;
        MechBossHealth.OnMechDeath += ShowVictory;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= ShowGameOver;
        MechBossHealth.OnMechDeath -= ShowVictory;
    }

    // ----------------------------------------------------------------------
    // Anzeige
    // ----------------------------------------------------------------------
    private void ShowGameOver()
    {
        if (gameEnded) return;
        gameEnded = true;

        gameOverPanel.SetActive(true);
        PlaySound(gameOverSound);
        EnterEndState();
    }

    private void ShowVictory()
    {
        if (gameEnded) return;
        gameEnded = true;

        victoryPanel.SetActive(true);
        PlaySound(victorySound);

        // Bewusst KEINE Pause: Der Spieler soll sich weiter bewegen und den
        // explodierenden Roboter sehen. Der Cursor wird aber sichtbar gemacht,
        // damit der Restart-Button oben rechts anklickbar ist (WASD-Bewegung
        // funktioniert weiterhin, auch bei entsperrtem Cursor).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnterEndState()
    {
        if (showCursorOnEnd)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseGameOnEnd)
            Time.timeScale = 0f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, soundVolume);
    }

    // Vom Play-Again-Button aufgerufen
    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ----------------------------------------------------------------------
    // UI-Aufbau (alles per Code)
    // ----------------------------------------------------------------------
    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("GameEndCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // über allem anderen

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem sicherstellen (sonst sind Buttons nicht klickbar)
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Panels
        gameOverPanel = BuildGameOverPanel(canvasGO.transform);
        victoryPanel = BuildVictoryPanel(canvasGO.transform);

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    private GameObject BuildGameOverPanel(Transform parent)
    {
        GameObject panel = CreateFullscreenPanel("GameOverPanel", parent, new Color(0.05f, 0f, 0f, 0.9f));

        CreateText(panel.transform, "Title", "GAME OVER",
            new Color(0.85f, 0.1f, 0.1f), 130, FontStyle.Bold,
            new Vector2(0.5f, 0.62f), new Vector2(900, 200));

        CreateText(panel.transform, "Subtitle", "Du wurdest vernichtet.",
            new Color(0.9f, 0.9f, 0.9f), 42, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(900, 80));

        CreatePlayAgainButton(panel.transform, new Color(0.7f, 0.12f, 0.12f));

        return panel;
    }

    private GameObject BuildVictoryPanel(Transform parent)
    {
        // Kein blaues Overlay: transparenter Hintergrund, nur die Schrift bleibt sichtbar,
        // damit der Spieler weiter spielen und den Roboter explodieren sehen kann.
        GameObject panel = CreateFullscreenPanel("VictoryPanel", parent, new Color(0f, 0f, 0f, 0f));
        // Hintergrund darf keine Klicks abfangen (sonst wäre Maus/Spiel blockiert).
        Image bg = panel.GetComponent<Image>();
        if (bg != null) bg.raycastTarget = false;

        CreateText(panel.transform, "Crown", "#1", // kleine Krone/Rang
            new Color(1f, 0.85f, 0.2f), 90, FontStyle.Bold,
            new Vector2(0.5f, 0.72f), new Vector2(600, 130));

        CreateText(panel.transform, "Title", "VICTORY ROYALE",
            new Color(1f, 0.88f, 0.25f), 120, FontStyle.Bold,
            new Vector2(0.5f, 0.58f), new Vector2(1400, 200));

        CreateText(panel.transform, "Subtitle", "Der Roboter ist nur noch Schrott!",
            new Color(1f, 1f, 1f), 40, FontStyle.Normal,
            new Vector2(0.5f, 0.46f), new Vector2(1200, 80));

        // Kleiner Restart-Button oben rechts, damit der Rest der Sicht frei bleibt.
        CreateRestartButton(panel.transform, new Color(0.2f, 0.55f, 0.9f),
            new Vector2(1f, 1f), new Vector2(-150, -60), new Vector2(240, 70), 34);

        return panel;
    }

    private GameObject CreateFullscreenPanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return panel;
    }

    private void CreateText(Transform parent, string name, string content, Color color,
        int fontSize, FontStyle style, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.color = color;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.font = GetBuiltinFont();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private void CreatePlayAgainButton(Transform parent, Color buttonColor)
    {
        GameObject btnGO = new GameObject("PlayAgainButton");
        btnGO.transform.SetParent(parent, false);

        Image img = btnGO.AddComponent<Image>();
        img.color = buttonColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(RestartGame);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.28f);
        rt.anchorMax = new Vector2(0.5f, 0.28f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420, 100);

        // Button-Beschriftung
        GameObject label = new GameObject("Label");
        label.transform.SetParent(btnGO.transform, false);
        Text txt = label.AddComponent<Text>();
        txt.text = "PLAY AGAIN";
        txt.color = Color.white;
        txt.fontSize = 46;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetBuiltinFont();

        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    // Flexibel platzierbarer Restart-Button (für die Ecke oben rechts beim Victory-Screen).
    private void CreateRestartButton(Transform parent, Color buttonColor,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(parent, false);

        Image img = btnGO.AddComponent<Image>();
        img.color = buttonColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(RestartGame);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        GameObject label = new GameObject("Label");
        label.transform.SetParent(btnGO.transform, false);
        Text txt = label.AddComponent<Text>();
        txt.text = "RESTART";
        txt.color = Color.white;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetBuiltinFont();

        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    private Font GetBuiltinFont()
    {
        Font f = null;
        // Unity 2022+ Built-in Font
        f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
