using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusUI : MonoBehaviour
{
    private FirstPersonController player;

    private TMP_Text stateText;
    private TMP_Text speedText;
    private TMP_Text altitudeText;
    private TMP_Text groundedText;
    private TMP_Text controlsText;
    private Image stateIndicator;
    private bool uiReady = false;

    private TMP_FontAsset defaultFont;

    void Start()
    {
        // Load TMP font from Resources folder
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        player = FindFirstObjectByType<FirstPersonController>();
        CreateUI();

        if (player != null)
        {
            player.OnStateChanged += OnPlayerStateChanged;
            OnPlayerStateChanged(player.CurrentState);
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnStateChanged -= OnPlayerStateChanged;
        }
    }

    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("StatusCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Status Panel (top-left)
        GameObject statusPanel = CreatePanel(canvasObj.transform, "StatusPanel",
            new Vector2(20, -20), new Vector2(280, 180), true);

        // Title
        CreateTMPText(statusPanel.transform, "Title", "PLAYER STATUS",
            new Vector2(140, -15), 24, new Color(0.7f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Top);

        // State indicator dot
        GameObject indicatorObj = new GameObject("Indicator");
        indicatorObj.transform.SetParent(statusPanel.transform, false);
        stateIndicator = indicatorObj.AddComponent<Image>();
        stateIndicator.color = Color.green;
        stateIndicator.raycastTarget = false;
        RectTransform indRect = indicatorObj.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0, 1);
        indRect.anchorMax = new Vector2(0, 1);
        indRect.pivot = new Vector2(0, 1);
        indRect.anchoredPosition = new Vector2(15, -55);
        indRect.sizeDelta = new Vector2(18, 18);

        // State text
        stateText = CreateTMPText(statusPanel.transform, "StateText", "GROUNDED",
            new Vector2(40, -52), 28, Color.white, FontStyles.Bold, TextAlignmentOptions.TopLeft);

        // Stats
        speedText = CreateTMPText(statusPanel.transform, "Speed", "Speed: 0.0 m/s",
            new Vector2(15, -90), 20, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal, TextAlignmentOptions.TopLeft);

        altitudeText = CreateTMPText(statusPanel.transform, "Altitude", "Altitude: 0.0 m",
            new Vector2(15, -115), 20, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal, TextAlignmentOptions.TopLeft);

        groundedText = CreateTMPText(statusPanel.transform, "Grounded", "Ground Contact: Yes",
            new Vector2(15, -140), 20, new Color(0.5f, 1f, 0.5f), FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Controls Panel (bottom-left)
        GameObject controlsPanel = CreatePanel(canvasObj.transform, "ControlsPanel",
            new Vector2(20, 20), new Vector2(340, 120), false);

        CreateTMPText(controlsPanel.transform, "ControlsTitle", "CONTROLS",
            new Vector2(170, -12), 20, new Color(0.7f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Top);

        controlsText = CreateTMPText(controlsPanel.transform, "ControlsText",
            "WASD - Move  |  Mouse - Look\nSpace - Jump  |  Shift - Sprint",
            new Vector2(15, -40), 18, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Crosshair (center)
        GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(canvasObj.transform, false);
        Image crossImg = crosshair.AddComponent<Image>();
        crossImg.color = new Color(1, 1, 1, 0.8f);
        crossImg.raycastTarget = false;
        RectTransform crossRect = crosshair.GetComponent<RectTransform>();
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.pivot = new Vector2(0.5f, 0.5f);
        crossRect.anchoredPosition = Vector2.zero;
        crossRect.sizeDelta = new Vector2(8, 8);

        uiReady = true;
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, bool topLeft)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);
        bg.raycastTarget = false;

        RectTransform rect = panel.GetComponent<RectTransform>();

        if (topLeft)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
        }
        else
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return panel;
    }

    TMP_Text CreateTMPText(Transform parent, string name, string content, Vector2 position,
        float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

        // Set font first
        if (defaultFont != null)
        {
            tmp.font = defaultFont;
        }

        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300, 80);

        return tmp;
    }

    void Update()
    {
        if (!uiReady || player == null) return;

        if (speedText != null)
            speedText.text = $"Speed: {player.CurrentSpeed:F1} m/s";
        if (altitudeText != null)
            altitudeText.text = $"Altitude: {player.Altitude:F1} m";
        if (groundedText != null)
        {
            groundedText.text = $"Ground Contact: {(player.IsGrounded ? "Yes" : "No")}";
            groundedText.color = player.IsGrounded
                ? new Color(0.5f, 1f, 0.5f)
                : new Color(0.8f, 0.8f, 0.8f);
        }
    }

    void OnPlayerStateChanged(FirstPersonController.PlayerState newState)
    {
        if (!uiReady || stateText == null || stateIndicator == null || controlsText == null) return;

        switch (newState)
        {
            case FirstPersonController.PlayerState.Grounded:
                stateText.text = "GROUNDED";
                stateIndicator.color = new Color(0.3f, 1f, 0.3f);
                controlsText.text = "WASD - Move  |  Mouse - Look\nHold Space - Hover  |  Shift - Sprint";
                break;

            case FirstPersonController.PlayerState.Floating:
                stateText.text = "FLOATING";
                stateIndicator.color = new Color(0.3f, 0.7f, 1f);
                controlsText.text = "WASD - Move  |  Mouse - Look\nSpace - Hover  |  Ctrl/Q - Down\nDouble-tap C - Land";
                break;

            case FirstPersonController.PlayerState.Landing:
                stateText.text = "LANDING";
                stateIndicator.color = new Color(1f, 0.8f, 0.3f);
                controlsText.text = "Descending to ground...";
                break;
        }
    }
}
