using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BuildingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuildingSystem buildingSystem;
    [SerializeField] private TMP_FontAsset fontAsset; // Drag LiberationSans SDF here if auto-load fails

    [Header("UI Panels")]
    private GameObject mainPanel;
    private GameObject categoryPanel;
    private GameObject itemsPanel;
    private GameObject buildModeIndicator;
    private TMP_Text selectedPieceText;
    private TMP_Text instructionsText;

    [Header("Runtime Loaded Pieces")]
    private Dictionary<BuildingCategory, List<BuildingPieceData>> piecesByCategory;

    private bool menuOpen = false;
    private BuildingCategory? selectedCategory = null;
    private Canvas canvas;
    private TMP_FontAsset tmpFont;

    // Simple data holder for runtime pieces
    public class BuildingPieceData
    {
        public string name;
        public GameObject prefab;
        public BuildingCategory category;
    }

    void Start()
    {
        // Load TMP font - use serialized field first, then try auto-load
        if (fontAsset != null)
        {
            tmpFont = fontAsset;
        }
        else
        {
            tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        Debug.Log($"TMP Font loaded: {(tmpFont != null ? tmpFont.name : "NULL - text will be broken")}");

        LoadBuildingPieces();
        CreateUI();

        if (buildingSystem == null)
            buildingSystem = FindFirstObjectByType<BuildingSystem>();

        if (buildingSystem != null)
        {
            buildingSystem.OnBuildModeChanged += OnBuildModeChanged;
            buildingSystem.OnPieceSelected += OnPieceSelected;
        }
    }

    void OnDestroy()
    {
        if (buildingSystem != null)
        {
            buildingSystem.OnBuildModeChanged -= OnBuildModeChanged;
            buildingSystem.OnPieceSelected -= OnPieceSelected;
        }
    }

    void LoadBuildingPieces()
    {
        piecesByCategory = new Dictionary<BuildingCategory, List<BuildingPieceData>>();

        foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            piecesByCategory[cat] = new List<BuildingPieceData>();
        }

        // Load prefabs from Creepy Cat package
        string basePath = "_Creepy_Cat/_3D Scifi Kit Vol 3/Prefabs";

        // Load Floors
        LoadPrefabsFromPath($"{basePath}/Builds", "Floor", BuildingCategory.Floors);

        // Load Walls
        LoadPrefabsFromPath($"{basePath}/Builds", "Wall", BuildingCategory.Walls);

        // Load Roofs
        LoadPrefabsFromPath($"{basePath}/Builds", "Roof", BuildingCategory.Roofs);

        // Load Props
        LoadPrefabsFromPath($"{basePath}/Props/_Update 1.00-First build/_Props", "", BuildingCategory.Props, 15);
        LoadPrefabsFromPath($"{basePath}/Props/_Machines & Computers", "", BuildingCategory.Machines, 10);

        Debug.Log($"Loaded building pieces - Floors: {piecesByCategory[BuildingCategory.Floors].Count}, " +
                  $"Walls: {piecesByCategory[BuildingCategory.Walls].Count}, " +
                  $"Roofs: {piecesByCategory[BuildingCategory.Roofs].Count}, " +
                  $"Props: {piecesByCategory[BuildingCategory.Props].Count}, " +
                  $"Machines: {piecesByCategory[BuildingCategory.Machines].Count}");
    }

    void LoadPrefabsFromPath(string path, string filter, BuildingCategory category, int limit = 20)
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>(path);

        // If Resources.LoadAll doesn't work, try loading from AssetDatabase in editor
        #if UNITY_EDITOR
        if (prefabs.Length == 0)
        {
            string fullPath = $"Assets/{path}";
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { fullPath });

            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= limit) break;

                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                if (string.IsNullOrEmpty(filter) || fileName.Contains(filter))
                {
                    GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null)
                    {
                        piecesByCategory[category].Add(new BuildingPieceData
                        {
                            name = CleanPrefabName(fileName),
                            prefab = prefab,
                            category = category
                        });
                        count++;
                    }
                }
            }
        }
        #endif
    }

    string CleanPrefabName(string name)
    {
        // Remove common prefixes like "P_"
        if (name.StartsWith("P_"))
            name = name.Substring(2);

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        return name;
    }

    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("BuildingCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Main build menu panel (centered)
        mainPanel = CreatePanel(canvasObj.transform, "MainBuildPanel", Vector2.zero, new Vector2(600, 500), true);
        mainPanel.SetActive(false);

        // Title
        CreateText(mainPanel.transform, "Title", "BUILD MENU", new Vector2(0, 220), 48,
            new Color(0.8f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);

        // Category buttons panel
        categoryPanel = CreatePanel(mainPanel.transform, "CategoryPanel", new Vector2(-150, 0), new Vector2(200, 400), false);
        CreateCategoryButtons();

        // Items panel (right side)
        itemsPanel = CreatePanel(mainPanel.transform, "ItemsPanel", new Vector2(100, 0), new Vector2(300, 400), false);

        // Instructions at bottom
        CreateText(mainPanel.transform, "MenuInstructions", "Click category, Select item, Press B to close",
            new Vector2(0, -220), 24, new Color(0.6f, 0.6f, 0.6f), FontStyles.Italic, TextAlignmentOptions.Center);

        // Build mode indicator (top right, always visible when building)
        buildModeIndicator = CreatePanel(canvasObj.transform, "BuildModeIndicator", new Vector2(-20, -20), new Vector2(300, 100), false);
        RectTransform indRect = buildModeIndicator.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(1, 1);
        indRect.anchorMax = new Vector2(1, 1);
        indRect.pivot = new Vector2(1, 1);

        selectedPieceText = CreateText(buildModeIndicator.transform, "SelectedPiece", "No piece selected",
            new Vector2(150, -25), 28, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);

        instructionsText = CreateText(buildModeIndicator.transform, "Instructions",
            "LMB: Place | R/T: Rotate | RMB: Cancel\nB: Open Menu",
            new Vector2(150, -65), 18, new Color(0.7f, 0.7f, 0.7f), FontStyles.Normal, TextAlignmentOptions.Center);

        buildModeIndicator.SetActive(false);

        // Bottom hint (always visible)
        GameObject hintPanel = CreatePanel(canvasObj.transform, "HintPanel", new Vector2(0, 20), new Vector2(200, 40), false);
        RectTransform hintRect = hintPanel.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0);
        hintRect.anchorMax = new Vector2(0.5f, 0);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        CreateText(hintPanel.transform, "HintText", "Press B to Build",
            new Vector2(100, 20), 22, new Color(0.9f, 0.9f, 0.9f), FontStyles.Normal, TextAlignmentOptions.Center);
    }

    void CreateCategoryButtons()
    {
        float yOffset = 160f;
        float buttonHeight = 60f;
        float spacing = 8f;

        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            CreateCategoryButton(categoryPanel.transform, category.ToString(), category, new Vector2(0, yOffset));
            yOffset -= buttonHeight + spacing;
        }
    }

    void CreateCategoryButton(Transform parent, string label, BuildingCategory category, Vector2 position)
    {
        GameObject btnObj = new GameObject($"Btn_{category}");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.7f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
        colors.selectedColor = new Color(0.3f, 0.5f, 0.7f);
        btn.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 55);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (tmpFont != null) tmp.font = tmpFont;
        tmp.text = $"{label} ({piecesByCategory[category].Count})";
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() => ShowCategoryItems(category));
    }

    void ShowCategoryItems(BuildingCategory category)
    {
        selectedCategory = category;

        // Clear existing items
        foreach (Transform child in itemsPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Create scroll view content
        float yOffset = 180f;
        float buttonHeight = 50f;
        float spacing = 5f;

        var pieces = piecesByCategory[category];

        foreach (var piece in pieces)
        {
            CreateItemButton(itemsPanel.transform, piece, new Vector2(0, yOffset));
            yOffset -= buttonHeight + spacing;
        }
    }

    void CreateItemButton(Transform parent, BuildingPieceData piece, Vector2 position)
    {
        GameObject btnObj = new GameObject($"Item_{piece.name}");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.2f, 0.25f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.4f, 0.5f);
        colors.pressedColor = new Color(0.2f, 0.35f, 0.45f);
        btn.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(290, 45);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (tmpFont != null) tmp.font = tmpFont;
        tmp.text = piece.name;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(10, 0, 0, 0);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() => SelectBuildingPiece(piece));
    }

    void SelectBuildingPiece(BuildingPieceData piece)
    {
        if (buildingSystem == null) return;

        // Create a runtime BuildingPiece
        BuildingPiece bp = ScriptableObject.CreateInstance<BuildingPiece>();
        bp.pieceName = piece.name;
        bp.prefab = piece.prefab;
        bp.category = piece.category;

        buildingSystem.SelectPiece(bp);
        CloseMenu();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Toggle build menu with B
        if (keyboard.bKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
        mainPanel.SetActive(menuOpen);

        // Handle cursor
        if (menuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void CloseMenu()
    {
        menuOpen = false;
        mainPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnBuildModeChanged(bool inBuildMode)
    {
        buildModeIndicator.SetActive(inBuildMode);
    }

    void OnPieceSelected(BuildingPiece piece)
    {
        if (selectedPieceText != null)
        {
            selectedPieceText.text = piece.pieceName;
        }
    }

    // UI Helper methods
    GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, bool centered)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

        RectTransform rect = panel.GetComponent<RectTransform>();

        if (centered)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return panel;
    }

    TMP_Text CreateText(Transform parent, string name, string content, Vector2 position,
        float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

        if (tmpFont != null)
            tmp.font = tmpFont;

        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 50);

        return tmp;
    }
}
