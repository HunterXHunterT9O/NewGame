using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using TMPro;
using EdgeOfUniverse.UI;
using EdgeOfUniverse.RTS;
using EdgeOfUniverse.Editor.UIGenerator;

namespace EdgeOfUniverse.Editor
{
    public class MissionControlSceneSetup : EditorWindow
    {
        private int planetCount = 8;
        private float mapRadius = 250f;
        private bool addDropship = true;
        private bool addAudio = true;
        private bool addPostProcessing = true;

        private static readonly string TexturePath = "Assets/Exo-planets - NovaShade/Sources Files/Textures/";
        private static readonly string SpherePath = "Assets/Exo-planets - NovaShade/Sources Files/Spheres models/Sphere_Smooth_ForCloseUp.FBX";
        private static readonly string SkyboxMatPath = "Assets/Exo-planets - NovaShade/Sources Files/Materials/Stars_background.mat";
        private static readonly string RingsPath = "Assets/Exo-planets - NovaShade/Sources Files/Spheres models/";
        private static readonly string DropshipPrefabPath = "Assets/_Creepy_Cat/_3D Scifi Kit Vol 3/Prefabs/Props/_Update 1.04-Mars Rover & DropShip/_Spaceship/P_Drop_Ship_Mark_01.prefab";
        private static readonly string AudioPath = "Assets/Exo-planets - NovaShade/SpaceAmbient-AudioLoop.wav";
        private static readonly string URPShaderCheckPath = "Assets/Exo-planets - NovaShade/Sources Files/Materials/Shaders";

        [MenuItem("Tools/Edge of Universe/Mission Control Scene")]
        public static void ShowWindow()
        {
            GetWindow<MissionControlSceneSetup>("Mission Control Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Mission Control Scene", EditorStyles.boldLabel);
            GUILayout.Label("Create a galactic mission control strategic view", EditorStyles.miniLabel);
            GUILayout.Space(15);

            // URP shader check
            bool shadersExtracted = AssetDatabase.IsValidFolder(URPShaderCheckPath);
            if (!shadersExtracted)
            {
                EditorGUILayout.HelpBox(
                    "URP shaders not extracted!\n\n" +
                    "Double-click in Unity Editor:\n" +
                    "Assets/Exo-planets - NovaShade/-- Files to EXTRACT here --/\n" +
                    "Files for URP - CLICK TO EXTRACT.unitypackage\n\n" +
                    "Planets will use fallback URP/Lit materials without extracted shaders.",
                    MessageType.Warning);
                GUILayout.Space(10);
            }

            planetCount = EditorGUILayout.IntSlider("Planet Count", planetCount, 1, 8);
            mapRadius = EditorGUILayout.Slider("Map Radius", mapRadius, 150f, 500f);
            addDropship = EditorGUILayout.Toggle("Add Dropship", addDropship);
            addAudio = EditorGUILayout.Toggle("Add Ambient Audio", addAudio);
            addPostProcessing = EditorGUILayout.Toggle("Add Post Processing", addPostProcessing);

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "This will create:\n" +
                $"• {planetCount} planets in orbital layout\n" +
                "• Star field skybox\n" +
                "• Strategic camera (WASD, zoom, orbit)\n" +
                (addDropship ? "• Dropship with travel system\n" : "") +
                "• Full mission control UI overlay\n" +
                (addAudio ? "• Ambient space audio\n" : "") +
                (addPostProcessing ? "• Post-processing effects\n" : "") +
                "• Mission control manager",
                MessageType.Info);

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Create Fresh Mission Control Scene", GUILayout.Height(45)))
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

        [MenuItem("Tools/Edge of Universe/Create Mission Control Scene Now")]
        public static void CreateFromCommandLine()
        {
            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var setup = CreateInstance<MissionControlSceneSetup>();
            setup.SetupInCurrentScene();

            string path = "Assets/Scenes/MissionControl.unity";
            EditorSceneManager.SaveScene(newScene, path);
            Debug.Log($"[Mission Control] Scene saved to {path}");
        }

        private void CreateFreshScene()
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupInCurrentScene();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Mission Control Scene",
                "MissionControl",
                "unity",
                "Save your new Mission Control scene");

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(newScene, path);
                Debug.Log($"[Mission Control] Scene saved to {path}");
            }
        }

        private void SetupInCurrentScene()
        {
            ClearExistingSetup();

            CreateSkybox();
            GameObject sun = CreateSun();
            PlanetInteractable[] planets = CreatePlanets(sun);
            MissionControlCamera cam = CreateCamera();

            DropshipController dropship = null;
            if (addDropship)
            {
                dropship = CreateDropship(planets);
            }

            var uiResult = CreateUI();
            MissionControlUI ui = uiResult.ui;
            MiningResultsUI resultsUI = uiResult.resultsUI;

            if (addAudio)
            {
                CreateAudio();
            }

            MissionControlManager manager = CreateManager(planets, cam, dropship);

            // Wire mining results UI to MissionControlUI
            if (ui != null && resultsUI != null)
            {
                SerializedObject uiWireSO = new SerializedObject(ui);
                uiWireSO.FindProperty("miningResultsUI").objectReferenceValue = resultsUI;
                uiWireSO.ApplyModifiedProperties();
            }

            if (addPostProcessing)
            {
                CreatePostProcessing();
            }

            Debug.Log("[Mission Control] Scene setup complete!\n\n" +
                "Controls:\n" +
                "• WASD/Arrows - Pan camera\n" +
                "• Mouse wheel - Zoom\n" +
                "• Right-drag - Orbit\n" +
                "• Left click - Select planet\n" +
                "• Deploy button - Send dropship");
        }

        private void ClearExistingSetup()
        {
            string[] objectsToRemove = {
                "MissionControlManager", "MissionControlCameraRig", "Planets",
                "Dropship", "MissionControl_Canvas", "Sun", "SunLight",
                "Directional Light", "AmbientAudio", "PostProcessing"
            };

            foreach (string name in objectsToRemove)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null) DestroyImmediate(obj);
            }

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                DestroyImmediate(cam.gameObject);
            }
        }

        private void CreateSkybox()
        {
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMatPath);
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
            }
            else
            {
                // Fallback: dark skybox
                RenderSettings.skybox = null;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
            RenderSettings.fog = false;
        }

        private GameObject CreateSun()
        {
            // Directional light
            GameObject lightObj = new GameObject("SunLight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.95f, 0.85f);
            lightObj.transform.rotation = Quaternion.Euler(35f, -45f, 0f);
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Sun Light");

            // Point light for LightSource.Sun reference
            GameObject sunObj = new GameObject("Sun");
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.intensity = 2f;
            sunLight.range = 200f;
            sunLight.color = new Color(1f, 0.9f, 0.7f);
            sunObj.transform.position = new Vector3(80f, 40f, -60f);
            Undo.RegisterCreatedObjectUndo(sunObj, "Create Sun Point");

            return sunObj;
        }

        private PlanetInteractable[] CreatePlanets(GameObject sun)
        {
            GameObject container = new GameObject("Planets");
            Undo.RegisterCreatedObjectUndo(container, "Create Planets");

            PlanetConfig[] configs = GetPlanetConfigs();
            int count = Mathf.Min(planetCount, configs.Length);
            PlanetInteractable[] planetComponents = new PlanetInteractable[count];

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = mapRadius * (0.6f + (i / (float)count) * 0.4f);
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    UnityEngine.Random.Range(-2f, 2f),
                    Mathf.Sin(angle) * radius
                );

                planetComponents[i] = CreatePlanet(container.transform, configs[i], position, sun);
            }

            return planetComponents;
        }

        private PlanetInteractable CreatePlanet(Transform parent, PlanetConfig config, Vector3 position, GameObject sun)
        {
            // Try to load the existing URP prefab (has correct mesh, materials, hierarchy)
            string prefabPath = $"Assets/Exo-planets - NovaShade/Prefabs/URP/{config.primaryTexture}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject root;

            if (prefab != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                root.name = config.name;
                root.transform.SetParent(parent);
                root.transform.position = position;
                // Prefabs have Planet child at scale ~30 (~60 units diameter)
                // Scale root to ~0.5 so planets are ~30 units diameter
                root.transform.localScale = Vector3.one * 0.5f;

                // Wire Sun reference on the existing LightSource
                LightSource lightSource = root.GetComponent<LightSource>();
                if (lightSource != null)
                {
                    SerializedObject lightSO = new SerializedObject(lightSource);
                    lightSO.FindProperty("Sun").objectReferenceValue = sun;
                    lightSO.ApplyModifiedProperties();
                }

                Debug.Log($"[Mission Control] Loaded URP prefab for {config.name}");
            }
            else
            {
                // Fallback: build from primitive spheres
                root = new GameObject(config.name);
                root.transform.SetParent(parent);
                root.transform.position = position;

                LightSource lightSource = root.AddComponent<LightSource>();

                GameObject planetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                planetObj.name = "Planet";
                planetObj.transform.SetParent(root.transform);
                planetObj.transform.localPosition = Vector3.zero;
                planetObj.transform.localScale = Vector3.one * config.scale;

                Renderer planetRenderer = planetObj.GetComponent<Renderer>();
                if (planetRenderer != null)
                {
                    Material mat = CreatePlanetMaterial(config);
                    planetRenderer.material = mat;
                }

                // Atmosphere
                GameObject atmosphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                atmosphere.name = "Atmosphere";
                atmosphere.transform.SetParent(root.transform);
                atmosphere.transform.localPosition = Vector3.zero;
                atmosphere.transform.localScale = Vector3.one * config.scale * 1.05f;

                var atmoCollider = atmosphere.GetComponent<Collider>();
                if (atmoCollider != null) DestroyImmediate(atmoCollider);

                Renderer atmoRenderer = atmosphere.GetComponent<Renderer>();
                Material atmoMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                atmoMat.SetColor("_BaseColor", config.atmosphereColor);
                atmoMat.SetFloat("_Surface", 1);
                atmoMat.SetFloat("_Blend", 0);
                atmoMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                atmoMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                atmoMat.SetInt("_ZWrite", 0);
                atmoMat.renderQueue = 3000;
                atmoRenderer.material = atmoMat;
                atmoRenderer.shadowCastingMode = ShadowCastingMode.Off;
                atmoRenderer.receiveShadows = false;

                // Rings
                CreateRings(root.transform, config);

                SerializedObject lightSO = new SerializedObject(lightSource);
                lightSO.FindProperty("Sun").objectReferenceValue = sun;
                lightSO.ApplyModifiedProperties();

                Debug.LogWarning($"[Mission Control] URP prefab not found for {config.primaryTexture}, using fallback spheres.");
            }

            // Add collider for interaction (if not already present)
            if (root.GetComponent<Collider>() == null)
            {
                SphereCollider collider = root.AddComponent<SphereCollider>();
                // Prefab planet mesh is ~30 units, fallback sphere is ~config.scale
                collider.radius = 16f;
            }

            // Add PlanetInteractable component
            PlanetInteractable interactable = root.AddComponent<PlanetInteractable>();

            // Create PlanetData ScriptableObject
            PlanetData data = CreatePlanetData(config);

            // Assign PlanetData
            SerializedObject interactableSO = new SerializedObject(interactable);
            interactableSO.FindProperty("planetData").objectReferenceValue = data;
            interactableSO.FindProperty("ringRadius").floatValue = 20f;
            interactableSO.ApplyModifiedProperties();

            return interactable;
        }

        private Material CreatePlanetMaterial(PlanetConfig config)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            // Load albedo texture
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}{config.primaryTexture}_Albedo.tga");
            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
            }
            else
            {
                mat.SetColor("_BaseColor", config.atmosphereColor);
            }

            // Load normal map
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}{config.primaryTexture}_Normal.tga");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Load metallic/smoothness
            Texture2D metalSmooth = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}{config.primaryTexture}_MetalSmoothness.tga");
            if (metalSmooth != null)
            {
                mat.SetTexture("_MetallicGlossMap", metalSmooth);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            // Secondary texture (water, lava, etc.)
            if (!string.IsNullOrEmpty(config.secondaryTexture))
            {
                Texture2D secondary = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}{config.secondaryTexture}");
                if (secondary != null)
                {
                    mat.SetTexture("_DetailAlbedoMap", secondary);
                    mat.EnableKeyword("_DETAIL_MULX2");
                }
            }

            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }

        private GameObject CreateRings(Transform parent, PlanetConfig config)
        {
            string ringFileName = null;
            switch (config.ringType)
            {
                case RingType.StarRings: ringFileName = "StarRings.fbx"; break;
                case RingType.StarRings3: ringFileName = "StarRings3.fbx"; break;
                case RingType.StarRings4: ringFileName = "StarRings4.fbx"; break;
                default: break;
            }

            GameObject ringsObj;

            if (ringFileName != null)
            {
                GameObject ringsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RingsPath}{ringFileName}");
                if (ringsPrefab != null)
                {
                    ringsObj = (GameObject)PrefabUtility.InstantiatePrefab(ringsPrefab);
                    ringsObj.name = "Rings";
                    ringsObj.transform.SetParent(parent);
                    ringsObj.transform.localPosition = Vector3.zero;
                    ringsObj.transform.localScale = Vector3.one * config.scale;
                    return ringsObj;
                }
            }

            // Create empty Rings placeholder (required by LightSource)
            ringsObj = new GameObject("Rings");
            ringsObj.transform.SetParent(parent);
            ringsObj.transform.localPosition = Vector3.zero;

            // Add a renderer so LightSource doesn't error
            MeshFilter mf = ringsObj.AddComponent<MeshFilter>();
            MeshRenderer mr = ringsObj.AddComponent<MeshRenderer>();
            mr.enabled = false;

            return ringsObj;
        }

        private PlanetData CreatePlanetData(PlanetConfig config)
        {
            string dataPath = "Assets/Data/Planets";
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                    AssetDatabase.CreateFolder("Assets", "Data");
                AssetDatabase.CreateFolder("Assets/Data", "Planets");
            }

            string assetPath = $"{dataPath}/{config.name.Replace(" ", "_")}.asset";

            // Check if it already exists
            PlanetData existing = AssetDatabase.LoadAssetAtPath<PlanetData>(assetPath);
            if (existing != null) return existing;

            PlanetData data = ScriptableObject.CreateInstance<PlanetData>();
            data.planetName = config.name;
            data.description = config.description;
            data.missionBriefing = config.missionBriefing;
            data.dangerLevel = config.dangerLevel;
            data.missionStatus = MissionStatus.Available;
            data.resources = config.resources;
            data.primaryTexture = config.primaryTexture;
            data.secondaryTexture = config.secondaryTexture;
            data.cloudDensity = config.cloudDensity;
            data.ringType = config.ringType;
            data.atmosphereColor = config.atmosphereColor;
            data.planetScale = config.scale;

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private MissionControlCamera CreateCamera()
        {
            GameObject rigObj = new GameObject("MissionControlCameraRig");
            rigObj.transform.position = new Vector3(0f, 120f, 0f);

            MissionControlCamera cam = rigObj.AddComponent<MissionControlCamera>();
            Undo.RegisterCreatedObjectUndo(rigObj, "Create Camera");
            return cam;
        }

        private DropshipController CreateDropship(PlanetInteractable[] planets)
        {
            GameObject dropshipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DropshipPrefabPath);

            GameObject dropshipRoot = new GameObject("Dropship");

            if (dropshipPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(dropshipPrefab);
                model.transform.SetParent(dropshipRoot.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 0.3f;

                // Assign animation controller if available
                Animator anim = model.GetComponent<Animator>();
                if (anim != null)
                {
                    RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/_Creepy_Cat/_3D Scifi Kit Vol 3/Animations/P_Drop_Ship_Mark_01.controller");
                    if (controller != null)
                    {
                        anim.runtimeAnimatorController = controller;
                    }
                }
            }
            else
            {
                // Fallback: capsule
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "DropshipModel";
                fallback.transform.SetParent(dropshipRoot.transform);
                fallback.transform.localPosition = Vector3.zero;
                fallback.transform.localScale = new Vector3(0.5f, 0.3f, 1.5f);

                var renderer = fallback.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.3f, 0.3f, 0.35f);
                renderer.material = mat;

                Debug.LogWarning("[Mission Control] Dropship prefab not found. Using fallback.");
            }

            // Position at first planet
            if (planets.Length > 0)
            {
                dropshipRoot.transform.position = planets[0].transform.position + Vector3.up * 20f;
            }

            DropshipController controller2 = dropshipRoot.AddComponent<DropshipController>();
            Undo.RegisterCreatedObjectUndo(dropshipRoot, "Create Dropship");
            return controller2;
        }

        private (MissionControlUI ui, MiningResultsUI resultsUI) CreateUI()
        {
            // Generate Skia textures if not present
            if (!MissionControlUIGenerator.TexturesExist())
            {
                MissionControlUIGenerator.GenerateAll();
            }

            string texPath = "Assets/UI/Generated/MissionControl";

            // Create Canvas
            GameObject canvasObj = new GameObject("MissionControl_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 0)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            MissionControlUI ui = canvasObj.AddComponent<MissionControlUI>();
            SerializedObject uiSO = new SerializedObject(ui);

            // Load Skia sprites (with 9-slice borders for panels)
            Sprite panelSprite = LoadSlicedSprite($"{texPath}/MC_Panel_9Slice.png", 20);
            Sprite headerPanelSprite = LoadSlicedSprite($"{texPath}/MC_Panel_Header_9Slice.png", 20);
            Sprite topBarSprite = LoadSprite($"{texPath}/MC_Bar_TopBar.png");
            Sprite bottomBarSprite = LoadSprite($"{texPath}/MC_Bar_BottomBar.png");
            Sprite confirmPanelSprite = LoadSlicedSprite($"{texPath}/MC_Panel_Confirm.png", 20);
            Sprite deploySprite = LoadSprite($"{texPath}/MC_Button_Deploy.png");
            Sprite deployHoverSprite = LoadSprite($"{texPath}/MC_Button_Deploy_Hover.png");
            Sprite deployDisabledSprite = LoadSprite($"{texPath}/MC_Button_Deploy_Disabled.png");
            Sprite confirmBtnSprite = LoadSprite($"{texPath}/MC_Button_Confirm.png");
            Sprite cancelBtnSprite = LoadSprite($"{texPath}/MC_Button_Cancel.png");
            Sprite barFillSprite = LoadSprite($"{texPath}/MC_BarFill.png");
            Sprite barFrameSprite = LoadSprite($"{texPath}/MC_BarFrame.png");
            Sprite dangerBarFillSprite = LoadSprite($"{texPath}/MC_DangerBarFill.png");
            Sprite travelBarFillSprite = LoadSprite($"{texPath}/MC_TravelBarFill.png");
            Sprite travelBarFrameSprite = LoadSprite($"{texPath}/MC_TravelBarFrame.png");
            Sprite diamondSprite = LoadSprite($"{texPath}/MC_Diamond.png");
            Sprite diamondLitSprite = LoadSprite($"{texPath}/MC_Diamond_Lit.png");
            Sprite separatorSprite = LoadSprite($"{texPath}/MC_Separator.png");

            // ======== TOP BAR ========
            GameObject topBar = CreateUIElement("TopBar", canvasObj.transform);
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0, 1);
            topBarRT.anchorMax = new Vector2(1, 1);
            topBarRT.pivot = new Vector2(0.5f, 1);
            topBarRT.anchoredPosition = Vector2.zero;
            topBarRT.sizeDelta = new Vector2(0, 55);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.sprite = topBarSprite;
            topBarImg.type = Image.Type.Tiled;
            topBarImg.color = Color.white;

            // Location label with bracket prefix
            TextMeshProUGUI locationText = CreateText("LocationText", topBar.transform,
                "// LOCATION: ARKAS HAVEN", 15, FontStyles.Bold,
                new Color(0.31f, 0.71f, 0.78f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(400, 30));
            uiSO.FindProperty("locationText").objectReferenceValue = locationText;

            // Mission count
            TextMeshProUGUI missionCountText = CreateText("MissionCount", topBar.transform,
                "MISSIONS: 0/8", 13, FontStyles.Normal,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(180, 30));
            missionCountText.alignment = TextAlignmentOptions.Center;
            uiSO.FindProperty("missionCountText").objectReferenceValue = missionCountText;

            // Resource summary
            TextMeshProUGUI resourceSummaryText = CreateText("ResourceSummary", topBar.transform,
                "FLEET CARGO: 0", 13, FontStyles.Normal,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-20, 0), new Vector2(280, 30));
            resourceSummaryText.alignment = TextAlignmentOptions.MidlineRight;
            uiSO.FindProperty("resourceSummaryText").objectReferenceValue = resourceSummaryText;

            // Fleet cargo row (below top bar)
            GameObject fleetCargoRow = CreateUIElement("FleetCargoRow", canvasObj.transform);
            RectTransform fcrRT = fleetCargoRow.GetComponent<RectTransform>();
            fcrRT.anchorMin = new Vector2(0, 1);
            fcrRT.anchorMax = new Vector2(1, 1);
            fcrRT.pivot = new Vector2(0.5f, 1);
            fcrRT.anchoredPosition = new Vector2(0, -55);
            fcrRT.sizeDelta = new Vector2(0, 26);

            Image fcrBg = fleetCargoRow.AddComponent<Image>();
            fcrBg.color = new Color(0.07f, 0.08f, 0.11f, 0.85f);
            fcrBg.raycastTarget = false;

            // Fleet resource labels
            Color fleetLabelColor = new Color(0.45f, 0.50f, 0.55f);
            Color fleetValueColor = new Color(0.31f, 0.71f, 0.78f);
            float fleetX = 20f;

            CreateText("FleetMinLabel", fleetCargoRow.transform, "MIN:", 10, FontStyles.Bold,
                fleetLabelColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX, 0), new Vector2(35, 22));
            TextMeshProUGUI fleetMineralsText = CreateText("FleetMinVal", fleetCargoRow.transform, "0", 11, FontStyles.Bold,
                fleetValueColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX + 35, 0), new Vector2(45, 22));
            uiSO.FindProperty("fleetMineralsText").objectReferenceValue = fleetMineralsText;

            fleetX += 100;
            CreateText("FleetFuelLabel", fleetCargoRow.transform, "FUEL:", 10, FontStyles.Bold,
                fleetLabelColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX, 0), new Vector2(40, 22));
            TextMeshProUGUI fleetFuelText = CreateText("FleetFuelVal", fleetCargoRow.transform, "0", 11, FontStyles.Bold,
                fleetValueColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX + 40, 0), new Vector2(45, 22));
            uiSO.FindProperty("fleetFuelText").objectReferenceValue = fleetFuelText;

            fleetX += 105;
            CreateText("FleetBioLabel", fleetCargoRow.transform, "BIO:", 10, FontStyles.Bold,
                fleetLabelColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX, 0), new Vector2(35, 22));
            TextMeshProUGUI fleetBioMatterText = CreateText("FleetBioVal", fleetCargoRow.transform, "0", 11, FontStyles.Bold,
                fleetValueColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX + 35, 0), new Vector2(45, 22));
            uiSO.FindProperty("fleetBioMatterText").objectReferenceValue = fleetBioMatterText;

            fleetX += 100;
            CreateText("FleetTechLabel", fleetCargoRow.transform, "TECH:", 10, FontStyles.Bold,
                fleetLabelColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX, 0), new Vector2(42, 22));
            TextMeshProUGUI fleetTechSalvageText = CreateText("FleetTechVal", fleetCargoRow.transform, "0", 11, FontStyles.Bold,
                fleetValueColor,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(fleetX + 42, 0), new Vector2(45, 22));
            uiSO.FindProperty("fleetTechSalvageText").objectReferenceValue = fleetTechSalvageText;

            // ======== PLANET INFO PANEL (right side, wider) ========
            GameObject planetInfoPanel = CreateUIElement("PlanetInfoPanel", canvasObj.transform);
            RectTransform piRT = planetInfoPanel.GetComponent<RectTransform>();
            piRT.anchorMin = new Vector2(1, 0);
            piRT.anchorMax = new Vector2(1, 1);
            piRT.pivot = new Vector2(1, 0.5f);
            piRT.anchoredPosition = new Vector2(-12, 0);
            piRT.sizeDelta = new Vector2(380, -120);
            piRT.offsetMin = new Vector2(piRT.offsetMin.x, 62); // above bottom bar
            piRT.offsetMax = new Vector2(-12, -82); // below top bar + fleet cargo row

            Image piImg = planetInfoPanel.AddComponent<Image>();
            piImg.sprite = headerPanelSprite;
            piImg.type = Image.Type.Sliced;
            piImg.color = Color.white;

            CanvasGroup planetInfoGroup = planetInfoPanel.AddComponent<CanvasGroup>();
            planetInfoGroup.alpha = 0f;
            planetInfoGroup.interactable = false;
            planetInfoGroup.blocksRaycasts = false;

            uiSO.FindProperty("planetInfoPanel").objectReferenceValue = piRT;
            uiSO.FindProperty("planetInfoGroup").objectReferenceValue = planetInfoGroup;

            // -- Planet name (in header area)
            TextMeshProUGUI planetNameText = CreateText("PlanetName", planetInfoPanel.transform,
                "", 22, FontStyles.Bold,
                new Color(0.31f, 0.71f, 0.78f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -8), new Vector2(-30, 28));
            planetNameText.alignment = TextAlignmentOptions.Center;
            uiSO.FindProperty("planetNameText").objectReferenceValue = planetNameText;

            // -- Section: THREAT ASSESSMENT
            CreateText("ThreatLabel", planetInfoPanel.transform,
                "THREAT ASSESSMENT", 11, FontStyles.Bold,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(18, -42), new Vector2(-36, 16));

            // Separator under threat label
            CreateSpriteElement("ThreatSep", planetInfoPanel.transform, separatorSprite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -56), new Vector2(-30, 3));

            // -- Danger diamonds row
            GameObject dangerRow = CreateUIElement("DangerRow", planetInfoPanel.transform);
            RectTransform dangerRowRT = dangerRow.GetComponent<RectTransform>();
            dangerRowRT.anchorMin = new Vector2(0, 1);
            dangerRowRT.anchorMax = new Vector2(1, 1);
            dangerRowRT.pivot = new Vector2(0, 1);
            dangerRowRT.anchoredPosition = new Vector2(18, -64);
            dangerRowRT.sizeDelta = new Vector2(-36, 28);

            HorizontalLayoutGroup dangerLayout = dangerRow.AddComponent<HorizontalLayoutGroup>();
            dangerLayout.spacing = 6;
            dangerLayout.childAlignment = TextAnchor.MiddleLeft;
            dangerLayout.childControlWidth = false;
            dangerLayout.childControlHeight = false;

            CreateText("DangerLabel", dangerRow.transform,
                "DANGER:", 12, FontStyles.Bold,
                new Color(0.7f, 0.7f, 0.7f),
                Vector2.zero, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(65, 24));

            SerializedProperty diamondsArray = uiSO.FindProperty("dangerDiamonds");
            diamondsArray.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                GameObject diamond = CreateUIElement($"Diamond_{i}", dangerRow.transform);
                RectTransform drt = diamond.GetComponent<RectTransform>();
                drt.sizeDelta = new Vector2(24, 24);
                Image dimg = diamond.AddComponent<Image>();
                dimg.sprite = diamondSprite;
                dimg.color = Color.white;
                dimg.preserveAspect = true;
                diamondsArray.GetArrayElementAtIndex(i).objectReferenceValue = dimg;
            }

            // Danger bar
            GameObject dangerBarBg = CreateUIElement("DangerBarBg", planetInfoPanel.transform);
            RectTransform dbgRT = dangerBarBg.GetComponent<RectTransform>();
            dbgRT.anchorMin = new Vector2(0, 1);
            dbgRT.anchorMax = new Vector2(1, 1);
            dbgRT.pivot = new Vector2(0.5f, 1);
            dbgRT.anchoredPosition = new Vector2(0, -96);
            dbgRT.sizeDelta = new Vector2(-36, 10);
            Image dbgImg = dangerBarBg.AddComponent<Image>();
            dbgImg.color = new Color(0.06f, 0.07f, 0.09f, 0.9f);

            GameObject dangerBarFill = CreateUIElement("DangerBarFill", dangerBarBg.transform);
            Image dangerBarImg = dangerBarFill.AddComponent<Image>();
            dangerBarImg.sprite = dangerBarFillSprite;
            dangerBarImg.type = Image.Type.Filled;
            dangerBarImg.fillMethod = Image.FillMethod.Horizontal;
            dangerBarImg.fillAmount = 0f;
            dangerBarImg.color = Color.white;
            SetStretch(dangerBarFill.GetComponent<RectTransform>());

            uiSO.FindProperty("dangerBar").objectReferenceValue = dangerBarImg;

            // -- Section: RESOURCES
            CreateSpriteElement("ResSep", planetInfoPanel.transform, separatorSprite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -114), new Vector2(-30, 3));

            CreateText("ResourcesLabel", planetInfoPanel.transform,
                "RESOURCES", 11, FontStyles.Bold,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(18, -120), new Vector2(-36, 16));

            float barY = -142f;
            CreateSkiaResourceBar(planetInfoPanel.transform, "Minerals", barY, uiSO, "mineralsBar", "mineralsText",
                new Color(0.78f, 0.65f, 0.35f), barFillSprite, barFrameSprite);
            CreateSkiaResourceBar(planetInfoPanel.transform, "Fuel", barY - 28f, uiSO, "fuelBar", "fuelText",
                new Color(0.35f, 0.65f, 0.85f), barFillSprite, barFrameSprite);
            CreateSkiaResourceBar(planetInfoPanel.transform, "BioMatter", barY - 56f, uiSO, "bioMatterBar", "bioMatterText",
                new Color(0.35f, 0.75f, 0.35f), barFillSprite, barFrameSprite);
            CreateSkiaResourceBar(planetInfoPanel.transform, "TechSalvage", barY - 84f, uiSO, "techSalvageBar", "techSalvageText",
                new Color(0.65f, 0.40f, 0.78f), barFillSprite, barFrameSprite);

            // -- Section: MISSION BRIEF
            CreateSpriteElement("BriefSep", planetInfoPanel.transform, separatorSprite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -240), new Vector2(-30, 3));

            CreateText("BriefLabel", planetInfoPanel.transform,
                "MISSION BRIEF", 11, FontStyles.Bold,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(18, -246), new Vector2(-36, 16));

            TextMeshProUGUI missionBriefText = CreateText("MissionBrief", planetInfoPanel.transform,
                "", 12, FontStyles.Italic,
                new Color(0.6f, 0.62f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(18, -265), new Vector2(-36, 70));
            missionBriefText.alignment = TextAlignmentOptions.TopLeft;
            missionBriefText.textWrappingMode = TextWrappingModes.Normal;
            uiSO.FindProperty("missionBriefText").objectReferenceValue = missionBriefText;

            // -- Deploy button (Skia textured)
            GameObject deployBtnObj = CreateUIElement("DeployButton", planetInfoPanel.transform);
            RectTransform deployRT = deployBtnObj.GetComponent<RectTransform>();
            deployRT.anchorMin = new Vector2(0, 0);
            deployRT.anchorMax = new Vector2(1, 0);
            deployRT.pivot = new Vector2(0.5f, 0);
            deployRT.anchoredPosition = new Vector2(0, 18);
            deployRT.sizeDelta = new Vector2(-36, 50);

            Image deployImg = deployBtnObj.AddComponent<Image>();
            deployImg.sprite = deploySprite;
            deployImg.color = Color.white;

            Button deployBtn = deployBtnObj.AddComponent<Button>();
            SpriteState deployStates = new SpriteState();
            deployStates.highlightedSprite = deployHoverSprite;
            deployStates.pressedSprite = deploySprite;
            deployStates.disabledSprite = deployDisabledSprite;
            deployBtn.spriteState = deployStates;
            deployBtn.transition = Selectable.Transition.SpriteSwap;

            TextMeshProUGUI deployBtnText = CreateText("Text", deployBtnObj.transform,
                "DEPLOY DROPSHIP", 15, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            SetStretch(deployBtnText.GetComponent<RectTransform>());
            deployBtnText.alignment = TextAlignmentOptions.Center;

            uiSO.FindProperty("deployButton").objectReferenceValue = deployBtn;
            uiSO.FindProperty("deployButtonText").objectReferenceValue = deployBtnText;

            // ======== CONFIRMATION OVERLAY ========
            GameObject confirmPanel = CreateUIElement("ConfirmPanel", canvasObj.transform);
            RectTransform cpRT = confirmPanel.GetComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.5f, 0.5f);
            cpRT.anchorMax = new Vector2(0.5f, 0.5f);
            cpRT.pivot = new Vector2(0.5f, 0.5f);
            cpRT.anchoredPosition = Vector2.zero;
            cpRT.sizeDelta = new Vector2(400, 200);

            Image cpImg = confirmPanel.AddComponent<Image>();
            cpImg.sprite = confirmPanelSprite;
            cpImg.type = Image.Type.Sliced;
            cpImg.color = Color.white;

            CanvasGroup confirmGroup = confirmPanel.AddComponent<CanvasGroup>();
            confirmGroup.alpha = 0f;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;
            confirmPanel.SetActive(false);

            // Confirm header text
            CreateText("ConfirmHeader", confirmPanel.transform,
                "DEPLOYMENT CONFIRMATION", 13, FontStyles.Bold,
                new Color(0.71f, 0.18f, 0.18f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(350, 24));

            TextMeshProUGUI confirmText = CreateText("ConfirmText", confirmPanel.transform,
                "Deploy dropship to\nTarget Planet?", 16, FontStyles.Normal, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 10), new Vector2(340, 60));
            confirmText.alignment = TextAlignmentOptions.Center;

            // Confirm/Cancel buttons with Skia sprites
            GameObject confirmYesObj = CreateSkiaButton("ConfirmYes", confirmPanel.transform,
                "CONFIRM", confirmBtnSprite,
                new Vector2(0.3f, 0), new Vector2(0.3f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 22), new Vector2(140, 45));

            GameObject confirmNoObj = CreateSkiaButton("ConfirmNo", confirmPanel.transform,
                "CANCEL", cancelBtnSprite,
                new Vector2(0.7f, 0), new Vector2(0.7f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 22), new Vector2(140, 45));

            uiSO.FindProperty("confirmPanel").objectReferenceValue = cpRT;
            uiSO.FindProperty("confirmGroup").objectReferenceValue = confirmGroup;
            uiSO.FindProperty("confirmYesButton").objectReferenceValue = confirmYesObj.GetComponent<Button>();
            uiSO.FindProperty("confirmNoButton").objectReferenceValue = confirmNoObj.GetComponent<Button>();
            uiSO.FindProperty("confirmText").objectReferenceValue = confirmText;

            // ======== BOTTOM BAR ========
            GameObject bottomBar = CreateUIElement("BottomBar", canvasObj.transform);
            RectTransform bbRT = bottomBar.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0, 0);
            bbRT.anchorMax = new Vector2(1, 0);
            bbRT.pivot = new Vector2(0.5f, 0);
            bbRT.anchoredPosition = Vector2.zero;
            bbRT.sizeDelta = new Vector2(0, 50);
            Image bbImg = bottomBar.AddComponent<Image>();
            bbImg.sprite = bottomBarSprite;
            bbImg.type = Image.Type.Tiled;
            bbImg.color = Color.white;

            // Dropship status
            TextMeshProUGUI dropshipStatusText = CreateText("DropshipStatus", bottomBar.transform,
                "DROPSHIP: STANDBY", 14, FontStyles.Bold,
                new Color(0.31f, 0.71f, 0.78f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(260, 30));
            uiSO.FindProperty("dropshipStatusText").objectReferenceValue = dropshipStatusText;

            // Travel progress bar with Skia frame
            GameObject progressBg = CreateUIElement("ProgressBarBg", bottomBar.transform);
            RectTransform progBgRT = progressBg.GetComponent<RectTransform>();
            progBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            progBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            progBgRT.anchoredPosition = new Vector2(50, 0);
            progBgRT.sizeDelta = new Vector2(450, 18);
            Image progBgImg = progressBg.AddComponent<Image>();
            progBgImg.sprite = travelBarFrameSprite;
            progBgImg.color = Color.white;

            GameObject progressFill = CreateUIElement("ProgressFill", progressBg.transform);
            Image progressFillImg = progressFill.AddComponent<Image>();
            progressFillImg.sprite = travelBarFillSprite;
            progressFillImg.type = Image.Type.Filled;
            progressFillImg.fillMethod = Image.FillMethod.Horizontal;
            progressFillImg.fillAmount = 0f;
            progressFillImg.color = Color.white;
            SetStretch(progressFill.GetComponent<RectTransform>());

            uiSO.FindProperty("travelProgressBar").objectReferenceValue = progressFillImg;

            TextMeshProUGUI travelProgressText = CreateText("ProgressText", bottomBar.transform,
                "0%", 14, FontStyles.Bold,
                new Color(0.31f, 0.71f, 0.78f),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-20, 0), new Vector2(80, 30));
            travelProgressText.alignment = TextAlignmentOptions.MidlineRight;
            uiSO.FindProperty("travelProgressText").objectReferenceValue = travelProgressText;

            // ======== SCANLINE OVERLAY ========
            GameObject scanlineObj = CreateUIElement("ScanlineOverlay", canvasObj.transform);
            SetStretch(scanlineObj.GetComponent<RectTransform>());

            RawImage scanline = scanlineObj.AddComponent<RawImage>();
            Texture2D scanTex = LoadTexture($"{texPath}/MC_Scanline.png");
            if (scanTex == null) scanTex = CreateScanlineTexture();
            scanTex.wrapMode = TextureWrapMode.Repeat;
            scanTex.filterMode = FilterMode.Point;
            scanline.texture = scanTex;
            scanline.color = new Color(1, 1, 1, 0.04f);
            scanline.uvRect = new Rect(0, 0, 480, 4.21875f); // tile across screen
            scanline.raycastTarget = false;

            uiSO.FindProperty("scanlineOverlay").objectReferenceValue = scanline;

            // ======== MINING RESULTS OVERLAY ========
            Sprite resultsPanelSprite = LoadSlicedSprite($"{texPath}/MC_Panel_Results_9Slice.png", 20);
            Sprite continueBtnSprite = LoadSprite($"{texPath}/MC_Button_Continue.png");

            GameObject resultsObj = CreateUIElement("MiningResultsPanel", canvasObj.transform);
            RectTransform resultsRT = resultsObj.GetComponent<RectTransform>();
            resultsRT.anchorMin = new Vector2(0.5f, 0.5f);
            resultsRT.anchorMax = new Vector2(0.5f, 0.5f);
            resultsRT.pivot = new Vector2(0.5f, 0.5f);
            resultsRT.anchoredPosition = Vector2.zero;
            resultsRT.sizeDelta = new Vector2(480, 400);

            Image resultsImg = resultsObj.AddComponent<Image>();
            resultsImg.sprite = resultsPanelSprite;
            resultsImg.type = Image.Type.Sliced;
            resultsImg.color = Color.white;

            CanvasGroup resultsGroup = resultsObj.AddComponent<CanvasGroup>();
            resultsGroup.alpha = 0f;
            resultsGroup.interactable = false;
            resultsGroup.blocksRaycasts = false;
            resultsObj.SetActive(false);

            MiningResultsUI miningResultsUI = resultsObj.AddComponent<MiningResultsUI>();
            SerializedObject mrSO = new SerializedObject(miningResultsUI);
            mrSO.FindProperty("resultsPanel").objectReferenceValue = resultsRT;
            mrSO.FindProperty("resultsGroup").objectReferenceValue = resultsGroup;

            // Title
            TextMeshProUGUI resultsTitleText = CreateText("ResultsTitle", resultsObj.transform,
                "MINING OPERATION COMPLETE", 14, FontStyles.Bold,
                new Color(0.31f, 0.71f, 0.78f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -12), new Vector2(440, 22));
            resultsTitleText.alignment = TextAlignmentOptions.Center;
            mrSO.FindProperty("titleText").objectReferenceValue = resultsTitleText;

            // Planet name
            TextMeshProUGUI resultsPlanetText = CreateText("ResultsPlanet", resultsObj.transform,
                "", 20, FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -38), new Vector2(440, 28));
            resultsPlanetText.alignment = TextAlignmentOptions.Center;
            mrSO.FindProperty("planetNameText").objectReferenceValue = resultsPlanetText;

            // Danger text
            TextMeshProUGUI resultsDangerText = CreateText("ResultsDanger", resultsObj.transform,
                "", 13, FontStyles.Bold,
                new Color(0.78f, 0.55f, 0.16f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -66), new Vector2(440, 20));
            resultsDangerText.alignment = TextAlignmentOptions.Center;
            mrSO.FindProperty("dangerText").objectReferenceValue = resultsDangerText;

            // Separator
            CreateSpriteElement("ResultsSep", resultsObj.transform, separatorSprite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -88), new Vector2(-40, 3));

            // Section header "EXTRACTED RESOURCES"
            CreateText("ExtractedHeader", resultsObj.transform,
                "EXTRACTED RESOURCES", 11, FontStyles.Bold,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(24, -96), new Vector2(-48, 16));

            // Resource result rows
            Color resLabelColor = new Color(0.6f, 0.62f, 0.65f);
            Color resValueColor = new Color(0.31f, 0.71f, 0.78f);
            float resRowY = -118f;

            CreateText("ResMinLabel", resultsObj.transform, "MINERALS", 12, FontStyles.Normal,
                resLabelColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, resRowY), new Vector2(120, 22));
            TextMeshProUGUI resMineralsVal = CreateText("ResMinVal", resultsObj.transform, "+0", 14, FontStyles.Bold,
                resValueColor,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, resRowY), new Vector2(80, 22));
            resMineralsVal.alignment = TextAlignmentOptions.MidlineRight;
            mrSO.FindProperty("mineralsValueText").objectReferenceValue = resMineralsVal;

            resRowY -= 28f;
            CreateText("ResFuelLabel", resultsObj.transform, "FUEL", 12, FontStyles.Normal,
                resLabelColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, resRowY), new Vector2(120, 22));
            TextMeshProUGUI resFuelVal = CreateText("ResFuelVal", resultsObj.transform, "+0", 14, FontStyles.Bold,
                resValueColor,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, resRowY), new Vector2(80, 22));
            resFuelVal.alignment = TextAlignmentOptions.MidlineRight;
            mrSO.FindProperty("fuelValueText").objectReferenceValue = resFuelVal;

            resRowY -= 28f;
            CreateText("ResBioLabel", resultsObj.transform, "BIOMATTER", 12, FontStyles.Normal,
                resLabelColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, resRowY), new Vector2(120, 22));
            TextMeshProUGUI resBioVal = CreateText("ResBioVal", resultsObj.transform, "+0", 14, FontStyles.Bold,
                resValueColor,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, resRowY), new Vector2(80, 22));
            resBioVal.alignment = TextAlignmentOptions.MidlineRight;
            mrSO.FindProperty("bioMatterValueText").objectReferenceValue = resBioVal;

            resRowY -= 28f;
            CreateText("ResTechLabel", resultsObj.transform, "TECH SALVAGE", 12, FontStyles.Normal,
                resLabelColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, resRowY), new Vector2(120, 22));
            TextMeshProUGUI resTechVal = CreateText("ResTechVal", resultsObj.transform, "+0", 14, FontStyles.Bold,
                resValueColor,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, resRowY), new Vector2(80, 22));
            resTechVal.alignment = TextAlignmentOptions.MidlineRight;
            mrSO.FindProperty("techSalvageValueText").objectReferenceValue = resTechVal;

            // Loss row (hidden by default)
            resRowY -= 36f;
            CreateSpriteElement("LossSep", resultsObj.transform, separatorSprite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, resRowY + 8), new Vector2(-40, 3));

            GameObject lossRow = CreateUIElement("LossRow", resultsObj.transform);
            RectTransform lossRowRT = lossRow.GetComponent<RectTransform>();
            lossRowRT.anchorMin = new Vector2(0, 1);
            lossRowRT.anchorMax = new Vector2(1, 1);
            lossRowRT.pivot = new Vector2(0.5f, 1);
            lossRowRT.anchoredPosition = new Vector2(0, resRowY - 4);
            lossRowRT.sizeDelta = new Vector2(-48, 24);
            lossRow.SetActive(false);

            TextMeshProUGUI lossText = CreateText("LossText", lossRow.transform,
                "HOSTILE INTERFERENCE: -0 UNITS LOST", 12, FontStyles.Bold,
                new Color(1f, 0.12f, 0.12f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            SetStretch(lossText.GetComponent<RectTransform>());
            lossText.alignment = TextAlignmentOptions.Center;

            mrSO.FindProperty("lossRow").objectReferenceValue = lossRow;
            mrSO.FindProperty("lossText").objectReferenceValue = lossText;

            // Continue button
            GameObject continueObj = CreateSkiaButton("ContinueButton", resultsObj.transform,
                "CONTINUE", continueBtnSprite ?? deploySprite,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 24), new Vector2(200, 50));
            mrSO.FindProperty("continueButton").objectReferenceValue = continueObj.GetComponent<Button>();

            mrSO.ApplyModifiedProperties();

            uiSO.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI");
            Debug.Log("[Mission Control] Skia-powered UI created with mining results panel.");
            return (ui, miningResultsUI);
        }

        private void CreateSkiaResourceBar(Transform parent, string label, float yPos,
            SerializedObject uiSO, string barProp, string textProp, Color barColor,
            Sprite fillSprite, Sprite frameSprite)
        {
            GameObject row = CreateUIElement($"{label}Row", parent);
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0, 1);
            rowRT.anchoredPosition = new Vector2(18, yPos);
            rowRT.sizeDelta = new Vector2(-36, 22);

            // Label
            CreateText($"{label}Label", row.transform, label.ToUpper(), 11, FontStyles.Normal,
                new Color(0.55f, 0.6f, 0.65f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                Vector2.zero, new Vector2(85, 20));

            // Bar frame
            GameObject barBg = CreateUIElement($"{label}BarBg", row.transform);
            RectTransform barBgRT = barBg.GetComponent<RectTransform>();
            barBgRT.anchorMin = new Vector2(0, 0.5f);
            barBgRT.anchorMax = new Vector2(1, 0.5f);
            barBgRT.pivot = new Vector2(0.5f, 0.5f);
            barBgRT.offsetMin = new Vector2(88, -7);
            barBgRT.offsetMax = new Vector2(-42, 7);

            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.sprite = frameSprite;
            barBgImg.color = Color.white;

            // Bar fill
            GameObject barFill = CreateUIElement($"{label}Fill", barBg.transform);
            Image barFillImg = barFill.AddComponent<Image>();
            barFillImg.sprite = fillSprite;
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillAmount = 0f;
            barFillImg.color = barColor;
            SetStretch(barFill.GetComponent<RectTransform>());

            uiSO.FindProperty(barProp).objectReferenceValue = barFillImg;

            // Value text
            TextMeshProUGUI valueText = CreateText($"{label}Value", row.transform,
                "0", 12, FontStyles.Bold, Color.white,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                Vector2.zero, new Vector2(35, 20));
            valueText.alignment = TextAlignmentOptions.MidlineRight;

            uiSO.FindProperty(textProp).objectReferenceValue = valueText;
        }

        private GameObject CreateSkiaButton(string name, Transform parent, string text, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;

            TextMeshProUGUI btnText = CreateText("Text", btnObj.transform,
                text, 13, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            SetStretch(btnText.GetComponent<RectTransform>());
            btnText.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }

        private void CreateSpriteElement(string name, Transform parent, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private Sprite LoadSprite(string assetPath)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning($"[MC UI] Texture not found: {assetPath}");
                return null;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite LoadSlicedSprite(string assetPath, int border)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning($"[MC UI] Texture not found: {assetPath}");
                return null;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private Texture2D LoadTexture(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private void CreateAudio()
        {
            GameObject audioObj = new GameObject("AmbientAudio");
            AudioSource source = audioObj.AddComponent<AudioSource>();

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (clip != null)
            {
                source.clip = clip;
                source.loop = true;
                source.volume = 0.3f;
                source.playOnAwake = true;
            }
            else
            {
                Debug.LogWarning("[Mission Control] Ambient audio not found at: " + AudioPath);
            }

            Undo.RegisterCreatedObjectUndo(audioObj, "Create Audio");
        }

        private MissionControlManager CreateManager(PlanetInteractable[] planets, MissionControlCamera cam, DropshipController dropship)
        {
            GameObject managerObj = new GameObject("MissionControlManager");
            MissionControlManager manager = managerObj.AddComponent<MissionControlManager>();

            // Add FleetInventory
            FleetInventory fleetInventory = managerObj.AddComponent<FleetInventory>();

            // Add MiningOperation
            MiningOperation miningOperation = managerObj.AddComponent<MiningOperation>();

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("missionCamera").objectReferenceValue = cam;

            if (dropship != null)
                so.FindProperty("dropship").objectReferenceValue = dropship;

            so.FindProperty("miningOperation").objectReferenceValue = miningOperation;
            so.FindProperty("fleetInventory").objectReferenceValue = fleetInventory;

            SerializedProperty planetsArray = so.FindProperty("planets");
            planetsArray.arraySize = planets.Length;
            for (int i = 0; i < planets.Length; i++)
            {
                planetsArray.GetArrayElementAtIndex(i).objectReferenceValue = planets[i];
            }

            so.FindProperty("currentPlanetIndex").intValue = 0;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(managerObj, "Create Manager");
            return manager;
        }

        private void CreatePostProcessing()
        {
            GameObject ppObj = new GameObject("PostProcessing");

            // Try to add Volume component for URP post-processing
            var volume = ppObj.AddComponent<Volume>();
            volume.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Add Bloom
            var bloom = profile.Add(typeof(UnityEngine.Rendering.Universal.Bloom)) as UnityEngine.Rendering.Universal.Bloom;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.3f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;

            // Add Film Grain
            var filmGrain = profile.Add(typeof(UnityEngine.Rendering.Universal.FilmGrain)) as UnityEngine.Rendering.Universal.FilmGrain;
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = 0.15f;

            // Add Chromatic Aberration
            var chromaticAberration = profile.Add(typeof(UnityEngine.Rendering.Universal.ChromaticAberration)) as UnityEngine.Rendering.Universal.ChromaticAberration;
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0.05f;

            // Save the profile
            string ppPath = "Assets/Data";
            if (!AssetDatabase.IsValidFolder(ppPath))
                AssetDatabase.CreateFolder("Assets", "Data");

            AssetDatabase.CreateAsset(profile, $"{ppPath}/MissionControlPostProcess.asset");
            volume.profile = profile;

            Undo.RegisterCreatedObjectUndo(ppObj, "Create Post Processing");
        }

        #region Planet Configurations

        private struct PlanetConfig
        {
            public string name;
            public string description;
            public string missionBriefing;
            public int dangerLevel;
            public string primaryTexture;
            public string secondaryTexture;
            public RingType ringType;
            public CloudDensity cloudDensity;
            public Color atmosphereColor;
            public float scale;
            public ResourceDeposit[] resources;
        }

        private PlanetConfig[] GetPlanetConfigs()
        {
            return new PlanetConfig[]
            {
                new PlanetConfig
                {
                    name = "Arkas Haven",
                    description = "A temperate world serving as the fleet's home base.",
                    missionBriefing = "Maintain base operations. All resource types available in moderate quantities.",
                    dangerLevel = 1,
                    primaryTexture = "Arkas",
                    secondaryTexture = "",
                    ringType = RingType.None,
                    cloudDensity = CloudDensity.Light,
                    atmosphereColor = new Color(0.3f, 0.6f, 1f, 0.12f),
                    scale = 2f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 50 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 50 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 50 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 50 }
                    }
                },
                new PlanetConfig
                {
                    name = "Vendara Prime",
                    description = "Lush oceanic planet with abundant biological resources.",
                    missionBriefing = "Extract biological samples from the planet's vast kelp forests. Light resistance expected.",
                    dangerLevel = 2,
                    primaryTexture = "Vendara",
                    secondaryTexture = "Vendara_water.tga",
                    ringType = RingType.None,
                    cloudDensity = CloudDensity.Average,
                    atmosphereColor = new Color(0.2f, 0.7f, 0.5f, 0.15f),
                    scale = 2.2f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 85 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 30 },
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 20 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 15 }
                    }
                },
                new PlanetConfig
                {
                    name = "Titawin Reach",
                    description = "Rocky mining world surrounded by asteroid rings.",
                    missionBriefing = "Establish mining operations in the mineral-rich canyon systems. Watch for unstable terrain.",
                    dangerLevel = 2,
                    primaryTexture = "Titawin",
                    secondaryTexture = "",
                    ringType = RingType.StarRings4,
                    cloudDensity = CloudDensity.None,
                    atmosphereColor = new Color(0.8f, 0.6f, 0.3f, 0.1f),
                    scale = 1.8f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 75 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 35 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 25 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 10 }
                    }
                },
                new PlanetConfig
                {
                    name = "Hoth Expanse",
                    description = "Frozen gas giant with dense cloud coverage and fuel deposits.",
                    missionBriefing = "Extract hydrogen fuel from the upper atmosphere. Severe storms complicate operations.",
                    dangerLevel = 3,
                    primaryTexture = "Hoth",
                    secondaryTexture = "",
                    ringType = RingType.StarRings3,
                    cloudDensity = CloudDensity.Heavy,
                    atmosphereColor = new Color(0.6f, 0.7f, 0.9f, 0.18f),
                    scale = 2.8f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 65 },
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 20 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 10 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 15 }
                    }
                },
                new PlanetConfig
                {
                    name = "Gataca Station",
                    description = "Derelict orbital station rich in salvageable technology.",
                    missionBriefing = "Infiltrate the abandoned station and recover tech salvage. Automated defenses still active.",
                    dangerLevel = 3,
                    primaryTexture = "Gataca",
                    secondaryTexture = "",
                    ringType = RingType.StarRings,
                    cloudDensity = CloudDensity.None,
                    atmosphereColor = new Color(0.5f, 0.4f, 0.7f, 0.1f),
                    scale = 1.6f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 85 },
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 30 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 20 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 5 }
                    }
                },
                new PlanetConfig
                {
                    name = "Bistar Forge",
                    description = "Volcanic world with extreme mineral concentrations.",
                    missionBriefing = "Mine rare minerals from active volcanic sites. Extreme heat and hostile fauna present.",
                    dangerLevel = 4,
                    primaryTexture = "Bistar",
                    secondaryTexture = "Bistar_Lava.tga",
                    ringType = RingType.None,
                    cloudDensity = CloudDensity.Light,
                    atmosphereColor = new Color(1f, 0.4f, 0.1f, 0.15f),
                    scale = 2.0f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 90 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 40 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 20 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 5 }
                    }
                },
                new PlanetConfig
                {
                    name = "Xagobah Deep",
                    description = "Dense swamp world with volatile fuel pockets.",
                    missionBriefing = "Tap into subterranean fuel reserves. Toxic atmosphere and aggressive predators.",
                    dangerLevel = 4,
                    primaryTexture = "Xagobah",
                    secondaryTexture = "Xagobah_water.tga",
                    ringType = RingType.None,
                    cloudDensity = CloudDensity.Average,
                    atmosphereColor = new Color(0.3f, 0.5f, 0.2f, 0.18f),
                    scale = 2.4f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 80 },
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 50 },
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 25 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 15 }
                    }
                },
                new PlanetConfig
                {
                    name = "Teratoma Hive",
                    description = "Infested world overrun by alien organisms.",
                    missionBriefing = "Retrieve biological specimens from the hive core. Maximum threat level. Expect overwhelming resistance.",
                    dangerLevel = 5,
                    primaryTexture = "Teratoma",
                    secondaryTexture = "Teratoma_Water.tga",
                    ringType = RingType.None,
                    cloudDensity = CloudDensity.Heavy,
                    atmosphereColor = new Color(0.6f, 0.1f, 0.1f, 0.2f),
                    scale = 2.6f,
                    resources = new ResourceDeposit[]
                    {
                        new ResourceDeposit { type = ResourceType.BioMatter, amount = 100 },
                        new ResourceDeposit { type = ResourceType.Fuel, amount = 30 },
                        new ResourceDeposit { type = ResourceType.TechSalvage, amount = 40 },
                        new ResourceDeposit { type = ResourceType.Minerals, amount = 15 }
                    }
                }
            };
        }

        #endregion

        #region UI Helpers

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent,
            string text, float fontSize, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return tmp;
        }

        private void SetStretch(RectTransform rt, float padding = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private Texture2D CreateScanlineTexture()
        {
            Texture2D tex = new Texture2D(1, 128, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < 128; y++)
            {
                float alpha = (y % 4 < 2) ? 0.08f : 0f;
                tex.SetPixel(0, y, new Color(1, 1, 1, alpha));
            }

            tex.Apply();
            return tex;
        }

        #endregion
    }
}
