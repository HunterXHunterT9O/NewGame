using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TMPro;
using EdgeOfUniverse.UI;
using EdgeOfUniverse.VFX;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// One-click RTS scene setup. Creates everything from scratch.
    /// </summary>
    public class RTSQuickStart : EditorWindow
    {
        private float groundSize = 100f;
        private int unitCount = 6;
        private int obstacleCount = 12;
        private bool addHUD = true;
        private bool addObstacles = true;
        private bool addVFX = true;

        [MenuItem("Tools/Edge of Universe/Quick Start RTS Scene")]
        public static void ShowWindow()
        {
            GetWindow<RTSQuickStart>("RTS Quick Start");
        }

        private void OnGUI()
        {
            GUILayout.Label("RTS Quick Start", EditorStyles.boldLabel);
            GUILayout.Label("Create a complete RTS scene from scratch", EditorStyles.miniLabel);
            GUILayout.Space(15);

            groundSize = EditorGUILayout.Slider("Ground Size", groundSize, 50f, 200f);
            unitCount = EditorGUILayout.IntSlider("Unit Count", unitCount, 1, 12);
            addObstacles = EditorGUILayout.Toggle("Add Obstacles", addObstacles);
            if (addObstacles)
            {
                obstacleCount = EditorGUILayout.IntSlider("Obstacle Count", obstacleCount, 5, 30);
            }
            addHUD = EditorGUILayout.Toggle("Add Mission HUD", addHUD);
            addVFX = EditorGUILayout.Toggle("Add VFX System", addVFX);

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "This will create:\n" +
                "• Ground plane with NavMesh\n" +
                "• RTS Camera (WASD, zoom, rotate)\n" +
                "• Unit Selection System\n" +
                "• Selectable units with pathfinding\n" +
                (addObstacles ? "• Obstacles for cover\n" : "") +
                (addHUD ? "• Mission HUD\n" : "") +
                (addVFX ? "• VFX System (particles, effects)" : ""),
                MessageType.Info);

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Create Fresh RTS Scene", GUILayout.Height(45)))
            {
                CreateFreshScene();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            if (GUILayout.Button("Setup in Current Scene"))
            {
                SetupInCurrentScene();
            }
        }

        private void CreateFreshScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupInCurrentScene();

            // Save scene
            string path = EditorUtility.SaveFilePanelInProject(
                "Save RTS Scene",
                "RTSMission",
                "unity",
                "Save your new RTS scene");

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(newScene, path);
                Debug.Log($"[RTS Quick Start] Scene saved to {path}");
            }
        }

        private void SetupInCurrentScene()
        {
            // Clear existing RTS objects
            ClearExistingSetup();

            // Setup in order
            CreateLighting();
            GameObject ground = CreateGround();
            CreateCamera();
            CreateSelectionSystem();

            if (addObstacles)
            {
                CreateObstacles();
            }

            // Bake NavMesh
            BakeNavMesh(ground);

            // Spawn units AFTER navmesh is baked
            SpawnUnits();

            if (addHUD)
            {
                CreateHUD();
            }

            if (addVFX)
            {
                SetupVFX();
            }

            Debug.Log("[RTS Quick Start] Scene setup complete!\n\n" +
                "Controls:\n" +
                "• WASD/Arrows - Pan camera\n" +
                "• Mouse wheel - Zoom\n" +
                "• Middle mouse - Rotate camera\n" +
                "• Left click - Select unit\n" +
                "• Shift+click - Add to selection\n" +
                "• Left drag - Box select\n" +
                "• Right click - Move units\n" +
                "• Space - Focus on selection\n" +
                "• Ctrl+A - Select all");
        }

        private void ClearExistingSetup()
        {
            // Remove existing RTS objects
            string[] objectsToRemove = {
                "Ground", "RTSCameraRig", "UnitSelectionSystem",
                "Obstacles", "Units", "MissionHUD_Canvas",
                "Directional Light", "Sun", "VFXManager"
            };

            foreach (string name in objectsToRemove)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }

            // Remove any cameras
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                DestroyImmediate(cam.gameObject);
            }

            // Remove test units
            foreach (var unit in FindObjectsByType<SelectableUnit>(FindObjectsSortMode.None))
            {
                DestroyImmediate(unit.gameObject);
            }
        }

        private void CreateLighting()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.95f, 0.9f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Undo.RegisterCreatedObjectUndo(lightObj, "Create Light");
        }

        private GameObject CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(groundSize / 10f, 1f, groundSize / 10f);
            ground.isStatic = true;
            ground.layer = LayerMask.NameToLayer("Default");

            // Material
            var renderer = ground.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.25f, 0.28f, 0.25f);
            renderer.material = mat;

            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            return ground;
        }

        private void CreateCamera()
        {
            GameObject rigObj = new GameObject("RTSCameraRig");
            RTSCameraController controller = rigObj.AddComponent<RTSCameraController>();
            rigObj.transform.position = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(rigObj, "Create Camera");
        }

        private void CreateSelectionSystem()
        {
            GameObject sysObj = new GameObject("UnitSelectionSystem");
            sysObj.AddComponent<UnitSelectionSystem>();

            Undo.RegisterCreatedObjectUndo(sysObj, "Create Selection System");
        }

        private void BakeNavMesh(GameObject ground)
        {
            NavMeshSurface surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = ground.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            Debug.Log("[RTS Quick Start] NavMesh baked.");
        }

        private void CreateObstacles()
        {
            GameObject container = new GameObject("Obstacles");
            Undo.RegisterCreatedObjectUndo(container, "Create Obstacles");

            float spawnRadius = groundSize * 0.4f;

            for (int i = 0; i < obstacleCount; i++)
            {
                // Random position, avoiding center
                Vector2 pos2D = Random.insideUnitCircle * spawnRadius;
                if (pos2D.magnitude < 10f)
                {
                    pos2D = pos2D.normalized * 10f;
                }

                Vector3 position = new Vector3(pos2D.x, 0, pos2D.y);

                // Create obstacle
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i}";
                obstacle.transform.SetParent(container.transform);
                obstacle.isStatic = true;

                float sizeX = Random.Range(2f, 5f);
                float sizeZ = Random.Range(2f, 5f);
                float height = Random.Range(2f, 4f);

                obstacle.transform.localScale = new Vector3(sizeX, height, sizeZ);
                obstacle.transform.position = position + Vector3.up * (height / 2f);
                obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Material
                var renderer = obstacle.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                float grey = Random.Range(0.3f, 0.45f);
                mat.color = new Color(grey, grey * 1.05f, grey * 1.1f);
                renderer.material = mat;

                // NavMesh carving
                var navObstacle = obstacle.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
            }
        }

        private void SpawnUnits()
        {
            GameObject container = new GameObject("Units");
            Undo.RegisterCreatedObjectUndo(container, "Create Units");

            for (int i = 0; i < unitCount; i++)
            {
                float angle = (i / (float)unitCount) * Mathf.PI * 2f;
                float radius = 5f + Random.value * 3f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                // Find valid NavMesh position
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }

                CreateUnit(container.transform, pos, i);
            }

            // Refresh unit list
            var selectionSystem = FindAnyObjectByType<UnitSelectionSystem>();
            if (selectionSystem != null)
            {
                selectionSystem.RefreshUnitList();
            }
        }

        private void CreateUnit(Transform parent, Vector3 position, int index)
        {
            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = $"Unit_{index}";
            unit.transform.SetParent(parent);
            unit.transform.position = position + Vector3.up * 1f;

            // Material
            var renderer = unit.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.4f, 0.7f);
            renderer.material = mat;

            // Components
            var selectable = unit.AddComponent<SelectableUnit>();

            NavMeshAgent agent = unit.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = 5f;
            agent.angularSpeed = 120f;
            agent.stoppingDistance = 0.5f;

            unit.AddComponent<UnitMovement>();
        }

        private void CreateHUD()
        {
            string uiAssetPath = "Assets/UI/Generated";

            // Create Canvas
            GameObject canvasObj = new GameObject("MissionHUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Add MissionHUD component
            MissionHUD hud = canvasObj.AddComponent<MissionHUD>();

            // Create HUD elements
            CreateThreatMeter(canvasObj.transform, hud, uiAssetPath);
            CreateSquadPanel(canvasObj.transform, hud, uiAssetPath);
            CreateMissionInfo(canvasObj.transform, hud, uiAssetPath);
            CreateResourceBars(canvasObj.transform, hud, uiAssetPath);
            CreateAlertPanel(canvasObj.transform, hud, uiAssetPath);

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD");
            Debug.Log("[RTS Quick Start] Mission HUD created.");
        }

        private void SetupVFX()
        {
            string vfxPath = "Assets/VFX/Prefabs";

            // Check if VFX prefabs exist, if not generate them
            if (!AssetDatabase.IsValidFolder(vfxPath))
            {
                Debug.Log("[RTS Quick Start] VFX prefabs not found. Please run Tools > Edge of Universe > Generate VFX Prefabs first.");
                return;
            }

            // Create VFX Manager
            GameObject managerObj = new GameObject("VFXManager");
            VFXManager manager = managerObj.AddComponent<VFXManager>();

            // Assign prefabs
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("moveCommandPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_MoveCommand.prefab");
            so.FindProperty("selectionBurstPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_SelectionBurst.prefab");
            so.FindProperty("dustTrailPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_DustTrail.prefab");
            so.FindProperty("muzzleFlashPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_MuzzleFlash.prefab");
            so.FindProperty("bulletImpactPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_BulletImpact.prefab");
            so.FindProperty("explosionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_Explosion.prefab");
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(managerObj, "Create VFX Manager");
            Debug.Log("[RTS Quick Start] VFX Manager created.");
        }

        private void CreateThreatMeter(Transform parent, MissionHUD hud, string uiAssetPath)
        {
            GameObject container = CreateUIElement("ThreatMeter", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(300, 60);

            // Frame
            GameObject frame = CreateUIElement("Frame", container.transform);
            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Frame.png");
            frameImg.type = Image.Type.Sliced;
            SetStretch(frame.GetComponent<RectTransform>());

            // Fill
            GameObject fill = CreateUIElement("Fill", container.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Clear.png");
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = new Vector2(4, 4);
            fillRt.offsetMax = new Vector2(-4, -4);

            // Label
            GameObject label = CreateUIElement("Label", container.transform);
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "CLEAR";
            labelText.fontSize = 16;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.22f, 0.55f, 0.29f);
            SetStretch(label.GetComponent<RectTransform>());

            // Assign to HUD
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("threatFill").objectReferenceValue = fillImg;
            hudSO.FindProperty("threatFrame").objectReferenceValue = frameImg;
            hudSO.FindProperty("threatLabel").objectReferenceValue = labelText;

            SerializedProperty spritesArray = hudSO.FindProperty("threatSprites");
            spritesArray.arraySize = 5;
            spritesArray.GetArrayElementAtIndex(0).objectReferenceValue = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Clear.png");
            spritesArray.GetArrayElementAtIndex(1).objectReferenceValue = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Detected.png");
            spritesArray.GetArrayElementAtIndex(2).objectReferenceValue = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Alerted.png");
            spritesArray.GetArrayElementAtIndex(3).objectReferenceValue = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Swarm.png");
            spritesArray.GetArrayElementAtIndex(4).objectReferenceValue = LoadSprite(uiAssetPath, "ThreatMeter/Threat_Critical.png");

            hudSO.ApplyModifiedProperties();
        }

        private void CreateSquadPanel(Transform parent, MissionHUD hud, string uiAssetPath)
        {
            GameObject container = CreateUIElement("SquadPanel", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(500, 100);

            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite(uiAssetPath, "Panels/Panel_Main.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.9f);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Create prefab
            GameObject framePrefab = CreateSquadFramePrefab(uiAssetPath);

            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("squadContainer").objectReferenceValue = container.transform;
            hudSO.FindProperty("squadFramePrefab").objectReferenceValue = framePrefab;
            hudSO.ApplyModifiedProperties();
        }

        private GameObject CreateSquadFramePrefab(string uiAssetPath)
        {
            GameObject frame = new GameObject("SquadFramePrefab");
            RectTransform rt = frame.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 70);

            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Normal.png");
            frameImg.type = Image.Type.Sliced;

            // Health bar
            GameObject healthBg = CreateUIElement("HealthBarBg", frame.transform);
            Image healthBgImg = healthBg.AddComponent<Image>();
            healthBgImg.sprite = LoadSprite(uiAssetPath, "ProgressBars/ProgressBar_Frame.png");
            healthBgImg.type = Image.Type.Sliced;
            RectTransform healthBgRt = healthBg.GetComponent<RectTransform>();
            healthBgRt.anchorMin = new Vector2(0, 0);
            healthBgRt.anchorMax = new Vector2(1, 0);
            healthBgRt.pivot = new Vector2(0.5f, 0);
            healthBgRt.anchoredPosition = new Vector2(0, -8);
            healthBgRt.sizeDelta = new Vector2(-10, 8);

            GameObject healthFill = CreateUIElement("HealthBarFill", healthBg.transform);
            Image healthFillImg = healthFill.AddComponent<Image>();
            healthFillImg.sprite = LoadSprite(uiAssetPath, "ProgressBars/ProgressBar_Health.png");
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            SetStretch(healthFill.GetComponent<RectTransform>(), 2);

            SquadFrameUI squadUI = frame.AddComponent<SquadFrameUI>();

            SerializedObject so = new SerializedObject(squadUI);
            so.FindProperty("frameImage").objectReferenceValue = frameImg;
            so.FindProperty("normalFrame").objectReferenceValue = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Normal.png");
            so.FindProperty("selectedFrame").objectReferenceValue = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Selected.png");
            so.FindProperty("woundedFrame").objectReferenceValue = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Wounded.png");
            so.FindProperty("criticalFrame").objectReferenceValue = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Critical.png");
            so.FindProperty("deadFrame").objectReferenceValue = LoadSprite(uiAssetPath, "SquadFrames/SquadFrame_Dead.png");
            so.FindProperty("healthBar").objectReferenceValue = healthFillImg;
            so.FindProperty("healthBarBackground").objectReferenceValue = healthBgImg;
            so.ApplyModifiedProperties();

            // Save prefab
            string prefabPath = "Assets/UI/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI"))
                    AssetDatabase.CreateFolder("Assets", "UI");
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(frame, prefabPath + "/SquadFrame.prefab");
            DestroyImmediate(frame);
            return prefab;
        }

        private void CreateMissionInfo(Transform parent, MissionHUD hud, string uiAssetPath)
        {
            GameObject container = CreateUIElement("MissionInfo", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(280, 120);

            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite(uiAssetPath, "Panels/Panel_Secondary.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.9f);

            // Title
            GameObject title = CreateUIElement("Title", container.transform);
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "MISSION";
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.color = new Color(0.31f, 0.71f, 0.78f);
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -10);
            titleRt.sizeDelta = new Vector2(-30, 25);

            // Timer
            GameObject timer = CreateUIElement("Timer", container.transform);
            TextMeshProUGUI timerText = timer.AddComponent<TextMeshProUGUI>();
            timerText.text = "00:00";
            timerText.fontSize = 24;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.TopRight;
            timerText.color = Color.white;
            RectTransform timerRt = timer.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(1, 1);
            timerRt.anchorMax = new Vector2(1, 1);
            timerRt.pivot = new Vector2(1, 1);
            timerRt.anchoredPosition = new Vector2(-15, -8);
            timerRt.sizeDelta = new Vector2(80, 30);

            // Objective
            GameObject objective = CreateUIElement("Objective", container.transform);
            TextMeshProUGUI objText = objective.AddComponent<TextMeshProUGUI>();
            objText.text = "• Locate the target\n• Eliminate hostiles\n• Extract safely";
            objText.fontSize = 14;
            objText.alignment = TextAlignmentOptions.TopLeft;
            objText.color = new Color(0.8f, 0.8f, 0.8f);
            RectTransform objRt = objective.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0, 0);
            objRt.anchorMax = new Vector2(1, 1);
            objRt.offsetMin = new Vector2(15, 15);
            objRt.offsetMax = new Vector2(-15, -40);

            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("missionTitle").objectReferenceValue = titleText;
            hudSO.FindProperty("missionTimer").objectReferenceValue = timerText;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objText;
            hudSO.FindProperty("missionPanel").objectReferenceValue = bgImg;
            hudSO.ApplyModifiedProperties();
        }

        private void CreateResourceBars(Transform parent, MissionHUD hud, string uiAssetPath)
        {
            GameObject container = CreateUIElement("ResourceBars", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 20);
            rt.sizeDelta = new Vector2(220, 80);

            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite(uiAssetPath, "Panels/Panel_Tooltip.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.85f);

            // Ammo bar
            var (ammoBar, ammoText) = CreateResourceBar(container.transform, "Ammo",
                LoadSprite(uiAssetPath, "ProgressBars/ProgressBar_Ammo.png"),
                LoadSprite(uiAssetPath, "Icons/Icon_Ammo.png"),
                new Vector2(0, 25));

            // Supply bar
            var (supplyBar, supplyText) = CreateResourceBar(container.transform, "Supply",
                LoadSprite(uiAssetPath, "ProgressBars/ProgressBar_Resource.png"),
                LoadSprite(uiAssetPath, "Icons/Icon_Medical.png"),
                new Vector2(0, -15));

            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("ammoBar").objectReferenceValue = ammoBar;
            hudSO.FindProperty("ammoText").objectReferenceValue = ammoText;
            hudSO.FindProperty("supplyBar").objectReferenceValue = supplyBar;
            hudSO.FindProperty("supplyText").objectReferenceValue = supplyText;
            hudSO.ApplyModifiedProperties();
        }

        private (Image, TextMeshProUGUI) CreateResourceBar(Transform parent, string name, Sprite fillSprite, Sprite iconSprite, Vector2 position)
        {
            GameObject container = CreateUIElement(name, parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(180, 25);

            // Icon
            GameObject icon = CreateUIElement("Icon", container.transform);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(20, 20);

            // Fill
            GameObject fill = CreateUIElement("Fill", container.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.75f;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0.5f);
            fillRt.anchorMax = new Vector2(1, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = new Vector2(27, -6);
            fillRt.offsetMax = new Vector2(-47, 6);

            // Text
            GameObject text = CreateUIElement("Text", container.transform);
            TextMeshProUGUI textComp = text.AddComponent<TextMeshProUGUI>();
            textComp.text = "75/100";
            textComp.fontSize = 12;
            textComp.alignment = TextAlignmentOptions.MidlineRight;
            textComp.color = Color.white;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(1, 0.5f);
            textRt.anchorMax = new Vector2(1, 0.5f);
            textRt.pivot = new Vector2(1, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(40, 20);

            return (fillImg, textComp);
        }

        private void CreateAlertPanel(Transform parent, MissionHUD hud, string uiAssetPath)
        {
            GameObject container = CreateUIElement("AlertPanel", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 150);
            rt.sizeDelta = new Vector2(400, 60);

            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite(uiAssetPath, "Panels/Panel_Alert.png");
            bgImg.type = Image.Type.Sliced;

            // Icon
            GameObject icon = CreateUIElement("Icon", container.transform);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.sprite = LoadSprite(uiAssetPath, "Icons/Icon_Warning.png");
            iconImg.preserveAspect = true;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(15, 0);
            iconRt.sizeDelta = new Vector2(40, 40);

            // Text
            GameObject text = CreateUIElement("Text", container.transform);
            TextMeshProUGUI textComp = text.AddComponent<TextMeshProUGUI>();
            textComp.text = "Alert message";
            textComp.fontSize = 18;
            textComp.fontStyle = FontStyles.Bold;
            textComp.alignment = TextAlignmentOptions.MidlineLeft;
            textComp.color = Color.white;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = new Vector2(65, 10);
            textRt.offsetMax = new Vector2(-15, -10);

            container.SetActive(false);

            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("alertContainer").objectReferenceValue = container;
            hudSO.FindProperty("alertIcon").objectReferenceValue = iconImg;
            hudSO.FindProperty("alertText").objectReferenceValue = textComp;
            hudSO.ApplyModifiedProperties();
        }

        #region UI Helpers

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void SetStretch(RectTransform rt, float padding = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private Sprite LoadSprite(string basePath, string relativePath)
        {
            string fullPath = $"{basePath}/{relativePath}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            if (sprite == null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
                if (tex != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.SaveAndReimport();
                    }
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
                }
            }

            return sprite;
        }

        #endregion
    }
}
