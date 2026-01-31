using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingUI_Toolkit : MonoBehaviour
{
    [SerializeField] private PanelSettings panelSettings;

    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement buildMenuPanel;
    private VisualElement categoryContainer;
    private VisualElement itemsContainer;
    private VisualElement buildModePanel;
    private Label hintLabel;
    private Label selectedPieceLabel;
    private bool menuOpen = false;
    private BuildingSystem buildingSystem;

    // Building pieces data
    private Dictionary<BuildingCategory, List<BuildingPieceData>> piecesByCategory;

    public class BuildingPieceData
    {
        public string name;
        public GameObject prefab;
        public BuildingCategory category;
    }

    void Start()
    {
        LoadBuildingPieces();
        SetupUIDocument();
        CreateUI();

        // Hook up building system events
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

    void OnBuildModeChanged(bool inBuildMode)
    {
        buildModePanel.style.display = inBuildMode ? DisplayStyle.Flex : DisplayStyle.None;
        hintLabel.style.display = inBuildMode ? DisplayStyle.None : DisplayStyle.Flex;
    }

    void OnPieceSelected(BuildingPiece piece)
    {
        selectedPieceLabel.text = piece.pieceName;
    }

    void LoadBuildingPieces()
    {
        piecesByCategory = new Dictionary<BuildingCategory, List<BuildingPieceData>>();

        foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            piecesByCategory[cat] = new List<BuildingPieceData>();
        }

        #if UNITY_EDITOR
        string basePath = "Assets/_Creepy_Cat/_3D Scifi Kit Vol 3/Prefabs";

        LoadPrefabsFromPath($"{basePath}/Builds", "Floor", BuildingCategory.Floors, 15);
        LoadPrefabsFromPath($"{basePath}/Builds", "Wall", BuildingCategory.Walls, 15);
        LoadPrefabsFromPath($"{basePath}/Builds", "Roof", BuildingCategory.Roofs, 15);
        LoadPrefabsFromPath($"{basePath}/Props/_Update 1.00-First build/_Props", "", BuildingCategory.Props, 15);
        LoadPrefabsFromPath($"{basePath}/Props/_Machines & Computers", "", BuildingCategory.Machines, 10);

        Debug.Log($"Loaded: Floors({piecesByCategory[BuildingCategory.Floors].Count}), " +
                  $"Walls({piecesByCategory[BuildingCategory.Walls].Count}), " +
                  $"Roofs({piecesByCategory[BuildingCategory.Roofs].Count}), " +
                  $"Props({piecesByCategory[BuildingCategory.Props].Count}), " +
                  $"Machines({piecesByCategory[BuildingCategory.Machines].Count})");
        #endif
    }

    void LoadPrefabsFromPath(string path, string filter, BuildingCategory category, int limit)
    {
        #if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { path });

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
                        name = CleanName(fileName),
                        prefab = prefab,
                        category = category
                    });
                    count++;
                }
            }
        }
        #endif
    }

    string CleanName(string name)
    {
        if (name.StartsWith("P_")) name = name.Substring(2);
        return name.Replace("_", " ");
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.bKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
        buildMenuPanel.style.display = menuOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // Only show hint if not in build mode and menu is closed
        bool inBuildMode = buildingSystem != null && buildingSystem.IsInBuildMode;
        if (!menuOpen)
        {
            hintLabel.style.display = inBuildMode ? DisplayStyle.None : DisplayStyle.Flex;
            buildModePanel.style.display = inBuildMode ? DisplayStyle.Flex : DisplayStyle.None;
        }
        else
        {
            hintLabel.style.display = DisplayStyle.None;
            buildModePanel.style.display = DisplayStyle.None;
        }

        // Handle cursor
        if (menuOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None; // Keep unlocked for testing
            UnityEngine.Cursor.visible = true;
        }
    }

    void SetupUIDocument()
    {
        // Add UIDocument component
        uiDocument = gameObject.AddComponent<UIDocument>();

        // Try to load PanelSettings from Resources if not assigned
        if (panelSettings == null)
        {
            panelSettings = Resources.Load<PanelSettings>("UI/BuildingPanelSettings");
        }

        // Create at runtime if still null
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            Debug.Log("Created PanelSettings at runtime");
        }

        uiDocument.panelSettings = panelSettings;
        root = uiDocument.rootVisualElement;
    }

    void CreateUI()
    {
        // Hint label at bottom
        hintLabel = new Label("Press B to Build");
        hintLabel.style.position = Position.Absolute;
        hintLabel.style.bottom = 40;
        hintLabel.style.left = new Length(50, LengthUnit.Percent);
        hintLabel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
        hintLabel.style.fontSize = 24;
        hintLabel.style.color = Color.white;
        hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        hintLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        hintLabel.style.paddingLeft = 20;
        hintLabel.style.paddingRight = 20;
        hintLabel.style.paddingTop = 10;
        hintLabel.style.paddingBottom = 10;
        hintLabel.style.borderTopLeftRadius = 8;
        hintLabel.style.borderTopRightRadius = 8;
        hintLabel.style.borderBottomLeftRadius = 8;
        hintLabel.style.borderBottomRightRadius = 8;
        root.Add(hintLabel);

        // Build menu panel
        buildMenuPanel = new VisualElement();
        buildMenuPanel.style.position = Position.Absolute;
        buildMenuPanel.style.left = new Length(50, LengthUnit.Percent);
        buildMenuPanel.style.top = new Length(50, LengthUnit.Percent);
        buildMenuPanel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        buildMenuPanel.style.width = 650;
        buildMenuPanel.style.height = 500;
        buildMenuPanel.style.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 0.95f);
        buildMenuPanel.style.borderTopLeftRadius = 12;
        buildMenuPanel.style.borderTopRightRadius = 12;
        buildMenuPanel.style.borderBottomLeftRadius = 12;
        buildMenuPanel.style.borderBottomRightRadius = 12;
        buildMenuPanel.style.paddingTop = 20;
        buildMenuPanel.style.paddingBottom = 50;
        buildMenuPanel.style.paddingLeft = 20;
        buildMenuPanel.style.paddingRight = 20;
        buildMenuPanel.style.display = DisplayStyle.None;
        root.Add(buildMenuPanel);

        // Title
        var title = new Label("BUILD MENU");
        title.style.fontSize = 32;
        title.style.color = new Color(0.8f, 0.9f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 15;
        buildMenuPanel.Add(title);

        // Content row (categories left, items right)
        var contentRow = new VisualElement();
        contentRow.style.flexDirection = FlexDirection.Row;
        contentRow.style.flexGrow = 1;
        buildMenuPanel.Add(contentRow);

        // Category container (left side)
        categoryContainer = new VisualElement();
        categoryContainer.style.width = 200;
        categoryContainer.style.marginRight = 15;
        contentRow.Add(categoryContainer);

        // Create category buttons
        foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            CreateCategoryButton(cat);
        }

        // Items container (right side)
        itemsContainer = new VisualElement();
        itemsContainer.style.flexGrow = 1;
        itemsContainer.style.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.8f);
        itemsContainer.style.borderTopLeftRadius = 8;
        itemsContainer.style.borderTopRightRadius = 8;
        itemsContainer.style.borderBottomLeftRadius = 8;
        itemsContainer.style.borderBottomRightRadius = 8;
        itemsContainer.style.paddingTop = 10;
        itemsContainer.style.paddingBottom = 10;
        itemsContainer.style.paddingLeft = 10;
        itemsContainer.style.paddingRight = 10;
        contentRow.Add(itemsContainer);

        // Placeholder text
        var placeholder = new Label("Select a category");
        placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
        placeholder.style.fontSize = 18;
        placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
        placeholder.style.flexGrow = 1;
        placeholder.name = "placeholder";
        itemsContainer.Add(placeholder);

        // Instructions
        var instructions = new Label("Press B to close");
        instructions.style.fontSize = 16;
        instructions.style.color = new Color(0.6f, 0.6f, 0.6f);
        instructions.style.unityTextAlign = TextAnchor.MiddleCenter;
        instructions.style.position = Position.Absolute;
        instructions.style.bottom = 15;
        instructions.style.left = 0;
        instructions.style.right = 0;
        buildMenuPanel.Add(instructions);

        // Build mode panel (top-right, shows when building)
        buildModePanel = new VisualElement();
        buildModePanel.style.position = Position.Absolute;
        buildModePanel.style.top = 20;
        buildModePanel.style.right = 20;
        buildModePanel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        buildModePanel.style.paddingTop = 15;
        buildModePanel.style.paddingBottom = 15;
        buildModePanel.style.paddingLeft = 20;
        buildModePanel.style.paddingRight = 20;
        buildModePanel.style.borderTopLeftRadius = 8;
        buildModePanel.style.borderTopRightRadius = 8;
        buildModePanel.style.borderBottomLeftRadius = 8;
        buildModePanel.style.borderBottomRightRadius = 8;
        buildModePanel.style.display = DisplayStyle.None;
        root.Add(buildModePanel);

        // Selected piece label
        selectedPieceLabel = new Label("No piece");
        selectedPieceLabel.style.fontSize = 22;
        selectedPieceLabel.style.color = Color.white;
        selectedPieceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        selectedPieceLabel.style.marginBottom = 10;
        buildModePanel.Add(selectedPieceLabel);

        // Controls info
        var controlsLabel = new Label("LMB: Place  |  R/T: Rotate\nRMB: Cancel  |  B: Menu");
        controlsLabel.style.fontSize = 14;
        controlsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        buildModePanel.Add(controlsLabel);
    }

    void CreateCategoryButton(BuildingCategory category)
    {
        int count = piecesByCategory[category].Count;

        var btn = new Button(() => ShowCategoryItems(category));
        btn.text = $"{category} ({count})";
        btn.style.height = 50;
        btn.style.marginBottom = 8;
        btn.style.fontSize = 18;
        btn.style.backgroundColor = new Color(0.2f, 0.25f, 0.3f);
        btn.style.color = Color.white;
        btn.style.borderTopLeftRadius = 6;
        btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6;
        btn.style.borderBottomRightRadius = 6;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;

        categoryContainer.Add(btn);
    }

    void ShowCategoryItems(BuildingCategory category)
    {
        // Clear items container
        itemsContainer.Clear();

        var pieces = piecesByCategory[category];

        if (pieces.Count == 0)
        {
            var empty = new Label("No items found");
            empty.style.color = new Color(0.5f, 0.5f, 0.5f);
            empty.style.fontSize = 16;
            itemsContainer.Add(empty);
            return;
        }

        // Create scroll view for items
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1;
        itemsContainer.Add(scrollView);

        foreach (var piece in pieces)
        {
            CreateItemButton(scrollView, piece);
        }
    }

    void CreateItemButton(VisualElement parent, BuildingPieceData piece)
    {
        var btn = new Button(() => SelectPiece(piece));
        btn.text = piece.name;
        btn.style.height = 40;
        btn.style.marginBottom = 5;
        btn.style.fontSize = 16;
        btn.style.backgroundColor = new Color(0.15f, 0.18f, 0.22f);
        btn.style.color = Color.white;
        btn.style.borderTopLeftRadius = 4;
        btn.style.borderTopRightRadius = 4;
        btn.style.borderBottomLeftRadius = 4;
        btn.style.borderBottomRightRadius = 4;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
        btn.style.unityTextAlign = TextAnchor.MiddleLeft;
        btn.style.paddingLeft = 10;

        parent.Add(btn);
    }

    void SelectPiece(BuildingPieceData piece)
    {
        Debug.Log($"Selected: {piece.name}");

        // Get BuildingSystem and select piece
        var buildingSystem = FindFirstObjectByType<BuildingSystem>();
        if (buildingSystem != null)
        {
            var bp = ScriptableObject.CreateInstance<BuildingPiece>();
            bp.pieceName = piece.name;
            bp.prefab = piece.prefab;
            buildingSystem.SelectPiece(bp);
        }

        // Close menu
        ToggleMenu();
    }
}
