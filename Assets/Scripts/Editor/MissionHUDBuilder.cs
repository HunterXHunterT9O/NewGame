using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EdgeOfUniverse.UI;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Editor utility to create the Mission HUD using generated UI assets.
    /// </summary>
    public class MissionHUDBuilder : EditorWindow
    {
        private string uiAssetPath = "Assets/UI/Generated";

        [MenuItem("Tools/Edge of Universe/Build Mission HUD")]
        public static void ShowWindow()
        {
            GetWindow<MissionHUDBuilder>("Mission HUD Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Mission HUD Builder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            uiAssetPath = EditorGUILayout.TextField("UI Assets Path", uiAssetPath);

            GUILayout.Space(20);

            if (GUILayout.Button("Create Mission HUD in Scene", GUILayout.Height(40)))
            {
                CreateMissionHUD();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will create a Canvas with the Mission HUD setup.\n\n" +
                "Components:\n" +
                "- Threat Meter (top center)\n" +
                "- Squad Panel (bottom left)\n" +
                "- Mission Info (top right)\n" +
                "- Resource Bars (bottom right)\n" +
                "- Alert Panel (center)",
                MessageType.Info);
        }

        private void CreateMissionHUD()
        {
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
            CreateThreatMeter(canvasObj.transform, hud);
            CreateSquadPanel(canvasObj.transform, hud);
            CreateMissionInfo(canvasObj.transform, hud);
            CreateResourceBars(canvasObj.transform, hud);
            CreateAlertPanel(canvasObj.transform, hud);

            // Select the created object
            Selection.activeGameObject = canvasObj;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Mission HUD");

            Debug.Log("[HUD Builder] Mission HUD created successfully!");
        }

        private void CreateThreatMeter(Transform parent, MissionHUD hud)
        {
            // Container
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
            frameImg.sprite = LoadSprite("ThreatMeter/Threat_Frame.png");
            frameImg.type = Image.Type.Sliced;
            SetStretch(frame.GetComponent<RectTransform>());

            // Fill
            GameObject fill = CreateUIElement("Fill", container.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = LoadSprite("ThreatMeter/Threat_Clear.png");
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
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            // Assign to HUD via serialized fields
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("threatFill").objectReferenceValue = fillImg;
            hudSO.FindProperty("threatFrame").objectReferenceValue = frameImg;
            hudSO.FindProperty("threatLabel").objectReferenceValue = labelText;

            // Load threat sprites array
            SerializedProperty spritesArray = hudSO.FindProperty("threatSprites");
            spritesArray.arraySize = 5;
            spritesArray.GetArrayElementAtIndex(0).objectReferenceValue = LoadSprite("ThreatMeter/Threat_Clear.png");
            spritesArray.GetArrayElementAtIndex(1).objectReferenceValue = LoadSprite("ThreatMeter/Threat_Detected.png");
            spritesArray.GetArrayElementAtIndex(2).objectReferenceValue = LoadSprite("ThreatMeter/Threat_Alerted.png");
            spritesArray.GetArrayElementAtIndex(3).objectReferenceValue = LoadSprite("ThreatMeter/Threat_Swarm.png");
            spritesArray.GetArrayElementAtIndex(4).objectReferenceValue = LoadSprite("ThreatMeter/Threat_Critical.png");

            hudSO.ApplyModifiedProperties();
        }

        private void CreateSquadPanel(Transform parent, MissionHUD hud)
        {
            // Container
            GameObject container = CreateUIElement("SquadPanel", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(500, 100);

            // Background panel
            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Panels/Panel_Main.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.9f);

            // Horizontal layout for squad frames
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Create squad frame prefab template
            GameObject framePrefab = CreateSquadFramePrefab();

            // Assign to HUD
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("squadContainer").objectReferenceValue = container.transform;
            hudSO.FindProperty("squadFramePrefab").objectReferenceValue = framePrefab;
            hudSO.ApplyModifiedProperties();
        }

        private GameObject CreateSquadFramePrefab()
        {
            // Create prefab in project
            GameObject frame = new GameObject("SquadFramePrefab");

            RectTransform rt = frame.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 70);

            // Frame image
            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = LoadSprite("SquadFrames/SquadFrame_Normal.png");
            frameImg.type = Image.Type.Sliced;

            // Health bar background
            GameObject healthBg = CreateUIElement("HealthBarBg", frame.transform);
            Image healthBgImg = healthBg.AddComponent<Image>();
            healthBgImg.sprite = LoadSprite("ProgressBars/ProgressBar_Frame.png");
            healthBgImg.type = Image.Type.Sliced;
            RectTransform healthBgRt = healthBg.GetComponent<RectTransform>();
            healthBgRt.anchorMin = new Vector2(0, 0);
            healthBgRt.anchorMax = new Vector2(1, 0);
            healthBgRt.pivot = new Vector2(0.5f, 0);
            healthBgRt.anchoredPosition = new Vector2(0, -8);
            healthBgRt.sizeDelta = new Vector2(-10, 8);

            // Health bar fill
            GameObject healthFill = CreateUIElement("HealthBarFill", healthBg.transform);
            Image healthFillImg = healthFill.AddComponent<Image>();
            healthFillImg.sprite = LoadSprite("ProgressBars/ProgressBar_Health.png");
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            SetStretch(healthFill.GetComponent<RectTransform>(), 2);

            // Add SquadFrameUI component
            SquadFrameUI squadUI = frame.AddComponent<SquadFrameUI>();

            // Assign references
            SerializedObject so = new SerializedObject(squadUI);
            so.FindProperty("frameImage").objectReferenceValue = frameImg;
            so.FindProperty("normalFrame").objectReferenceValue = LoadSprite("SquadFrames/SquadFrame_Normal.png");
            so.FindProperty("selectedFrame").objectReferenceValue = LoadSprite("SquadFrames/SquadFrame_Selected.png");
            so.FindProperty("woundedFrame").objectReferenceValue = LoadSprite("SquadFrames/SquadFrame_Wounded.png");
            so.FindProperty("criticalFrame").objectReferenceValue = LoadSprite("SquadFrames/SquadFrame_Critical.png");
            so.FindProperty("deadFrame").objectReferenceValue = LoadSprite("SquadFrames/SquadFrame_Dead.png");
            so.FindProperty("healthBar").objectReferenceValue = healthFillImg;
            so.FindProperty("healthBarBackground").objectReferenceValue = healthBgImg;
            so.ApplyModifiedProperties();

            // Save as prefab
            string prefabPath = "Assets/UI/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
            }

            string fullPath = prefabPath + "/SquadFrame.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(frame, fullPath);
            DestroyImmediate(frame);

            return prefab;
        }

        private void CreateMissionInfo(Transform parent, MissionHUD hud)
        {
            // Container
            GameObject container = CreateUIElement("MissionInfo", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(280, 120);

            // Background
            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Panels/Panel_Secondary.png");
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
            objText.text = "- Locate the target\n- Eliminate hostiles\n- Extract safely";
            objText.fontSize = 14;
            objText.alignment = TextAlignmentOptions.TopLeft;
            objText.color = new Color(0.8f, 0.8f, 0.8f);
            RectTransform objRt = objective.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0, 0);
            objRt.anchorMax = new Vector2(1, 1);
            objRt.offsetMin = new Vector2(15, 15);
            objRt.offsetMax = new Vector2(-15, -40);

            // Assign to HUD
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("missionTitle").objectReferenceValue = titleText;
            hudSO.FindProperty("missionTimer").objectReferenceValue = timerText;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objText;
            hudSO.FindProperty("missionPanel").objectReferenceValue = bgImg;
            hudSO.ApplyModifiedProperties();
        }

        private void CreateResourceBars(Transform parent, MissionHUD hud)
        {
            // Container
            GameObject container = CreateUIElement("ResourceBars", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 20);
            rt.sizeDelta = new Vector2(220, 80);

            // Background
            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Panels/Panel_Tooltip.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.85f);

            // Ammo bar
            GameObject ammoContainer = CreateResourceBar(container.transform, "Ammo", "AMMO",
                LoadSprite("ProgressBars/ProgressBar_Ammo.png"),
                LoadSprite("Icons/Icon_Ammo.png"),
                new Vector2(0, 25));

            // Supply bar
            GameObject supplyContainer = CreateResourceBar(container.transform, "Supply", "SUPPLY",
                LoadSprite("ProgressBars/ProgressBar_Resource.png"),
                LoadSprite("Icons/Icon_Medical.png"),
                new Vector2(0, -15));

            // Get references
            Image ammoBar = ammoContainer.transform.Find("Fill").GetComponent<Image>();
            TextMeshProUGUI ammoText = ammoContainer.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            Image supplyBar = supplyContainer.transform.Find("Fill").GetComponent<Image>();
            TextMeshProUGUI supplyText = supplyContainer.transform.Find("Text").GetComponent<TextMeshProUGUI>();

            // Assign to HUD
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("ammoBar").objectReferenceValue = ammoBar;
            hudSO.FindProperty("ammoText").objectReferenceValue = ammoText;
            hudSO.FindProperty("supplyBar").objectReferenceValue = supplyBar;
            hudSO.FindProperty("supplyText").objectReferenceValue = supplyText;
            hudSO.ApplyModifiedProperties();
        }

        private GameObject CreateResourceBar(Transform parent, string name, string label, Sprite fillSprite, Sprite iconSprite, Vector2 position)
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
            iconRt.anchoredPosition = new Vector2(0, 0);
            iconRt.sizeDelta = new Vector2(20, 20);

            // Bar background
            GameObject barBg = CreateUIElement("Background", container.transform);
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.sprite = LoadSprite("ProgressBars/ProgressBar_Frame.png");
            barBgImg.type = Image.Type.Sliced;
            RectTransform barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0, 0.5f);
            barBgRt.anchorMax = new Vector2(1, 0.5f);
            barBgRt.pivot = new Vector2(0.5f, 0.5f);
            barBgRt.offsetMin = new Vector2(25, -8);
            barBgRt.offsetMax = new Vector2(-45, 8);

            // Bar fill
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
            textRt.anchoredPosition = new Vector2(0, 0);
            textRt.sizeDelta = new Vector2(40, 20);

            return container;
        }

        private void CreateAlertPanel(Transform parent, MissionHUD hud)
        {
            // Container
            GameObject container = CreateUIElement("AlertPanel", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 150);
            rt.sizeDelta = new Vector2(400, 60);

            // Background
            Image bgImg = container.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Panels/Panel_Alert.png");
            bgImg.type = Image.Type.Sliced;

            // Icon
            GameObject icon = CreateUIElement("Icon", container.transform);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.sprite = LoadSprite("Icons/Icon_Warning.png");
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
            textComp.text = "Alert message here";
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

            // Assign to HUD
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("alertContainer").objectReferenceValue = container;
            hudSO.FindProperty("alertIcon").objectReferenceValue = iconImg;
            hudSO.FindProperty("alertText").objectReferenceValue = textComp;
            hudSO.ApplyModifiedProperties();
        }

        #region Helpers

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

        private Sprite LoadSprite(string relativePath)
        {
            string fullPath = $"{uiAssetPath}/{relativePath}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            if (sprite == null)
            {
                // Try loading as texture and getting sprite
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
                if (tex != null)
                {
                    // Ensure texture is set to sprite mode
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

            if (sprite == null)
            {
                Debug.LogWarning($"[HUD Builder] Could not load sprite: {fullPath}");
            }

            return sprite;
        }

        #endregion
    }
}
