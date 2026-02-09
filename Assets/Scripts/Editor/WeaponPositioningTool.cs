using UnityEngine;
using UnityEditor;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Interactive weapon positioning tool with real-time preview.
    /// Uses quaternion rotation to avoid gimbal lock.
    /// </summary>
    public class WeaponPositioningTool : EditorWindow
    {
        private GameObject selectedSoldier;
        private Transform weaponTransform;
        private Transform rightHandTransform;

        // Position
        private float posX = 0.05f;
        private float posY = 0.0f;
        private float posZ = 0.1f;

        // Rotation stored as quaternion to avoid gimbal lock
        private Quaternion weaponRotation = Quaternion.Euler(-90f, 0f, 90f);

        // Rotation step size
        private float rotationStep = 5f;

        // Scale
        private float scale = 1f;

        private bool autoUpdate = true;
        private Vector2 scrollPos;

        // Animation preview
        private bool previewAnimation = false;
        private AnimationClip idleClip;
        private Animator animator;
        private float animationTime = 0f;

        [MenuItem("Tools/Edge of Universe/Weapon Positioning Tool (Enhanced)")]
        public static void ShowWindow()
        {
            WeaponPositioningTool window = GetWindow<WeaponPositioningTool>("Weapon Tool");
            window.minSize = new Vector2(350, 700);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            Selection.selectionChanged -= OnSelectionChanged;

            if (previewAnimation && !Application.isPlaying)
            {
                AnimationMode.StopAnimationMode();
                previewAnimation = false;
            }
        }

        private void OnSelectionChanged()
        {
            if (previewAnimation && !Application.isPlaying)
            {
                AnimationMode.StopAnimationMode();
                previewAnimation = false;
            }

            if (Selection.activeGameObject != null)
            {
                Transform rightHand = FindRightHand(Selection.activeGameObject.transform);
                if (rightHand != null)
                {
                    Transform weapon = rightHand.Find("Weapon");
                    if (weapon != null)
                    {
                        selectedSoldier = Selection.activeGameObject;
                        rightHandTransform = rightHand;
                        weaponTransform = weapon;

                        animator = selectedSoldier.GetComponent<Animator>();
                        if (animator != null && animator.runtimeAnimatorController != null)
                        {
                            LoadIdleClip();
                        }

                        // Load current values
                        posX = weapon.localPosition.x;
                        posY = weapon.localPosition.y;
                        posZ = weapon.localPosition.z;
                        weaponRotation = weapon.localRotation;
                        scale = weapon.localScale.x;

                        Repaint();
                    }
                }
            }
        }

        private void OnUpdate()
        {
            if (autoUpdate && weaponTransform != null)
            {
                ApplyTransform();
            }

            if (previewAnimation && !Application.isPlaying && idleClip != null && selectedSoldier != null)
            {
                animationTime += 0.016f;
                if (animationTime > idleClip.length)
                    animationTime = 0f;

                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(selectedSoldier, idleClip, animationTime);
                AnimationMode.EndSampling();

                SceneView.RepaintAll();
            }
        }

        private void LoadIdleClip()
        {
            string idleAnimPath = "Assets/Protofactor/Sci Fi/Common/Animations/Humanoid@IdleAimAssaultRifle.FBX";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(idleAnimPath);

            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    idleClip = clip;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            GUILayout.Label("Weapon Positioning Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            if (selectedSoldier != null && weaponTransform != null)
            {
                EditorGUILayout.LabelField("Selected:", selectedSoldier.name);
                EditorGUILayout.LabelField("Right Hand:", rightHandTransform?.name ?? "Not Found");
                EditorGUILayout.LabelField("Weapon:", weaponTransform.name);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Edit Mode active. Toggle 'Preview Animation' to see idle pose.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Play Mode active - live preview!", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a soldier in the Hierarchy with a weapon equipped", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Toggles
            autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);

            if (!Application.isPlaying)
            {
                EditorGUI.BeginChangeCheck();
                previewAnimation = EditorGUILayout.Toggle("Preview Animation (Edit Mode)", previewAnimation);
                if (EditorGUI.EndChangeCheck())
                {
                    if (previewAnimation && selectedSoldier != null && idleClip != null)
                    {
                        AnimationMode.StartAnimationMode();
                        animationTime = 0f;
                    }
                    else
                    {
                        AnimationMode.StopAnimationMode();
                    }
                }

                if (previewAnimation && idleClip == null)
                {
                    EditorGUILayout.HelpBox("Idle animation not found.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            DrawSeparator();

            // ── POSITION ──
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            posX = DrawSliderRow("X", posX, -0.3f, 0.3f);
            posY = DrawSliderRow("Y", posY, -0.3f, 0.3f);
            posZ = DrawSliderRow("Z", posZ, -0.3f, 0.3f);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawSeparator();

            // ── ROTATION (Local Axis Buttons) ──
            EditorGUILayout.LabelField("Rotation (Local Axes)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            rotationStep = EditorGUILayout.Slider("Step (degrees)", rotationStep, 1f, 45f);

            EditorGUILayout.Space(5);

            // Pitch (tilt barrel up/down)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pitch", GUILayout.Width(50));
            if (GUILayout.Button("- Down")) RotateLocal(Vector3.right, -rotationStep);
            if (GUILayout.Button("+ Up")) RotateLocal(Vector3.right, rotationStep);
            EditorGUILayout.EndHorizontal();

            // Yaw (turn barrel left/right)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Yaw", GUILayout.Width(50));
            if (GUILayout.Button("- Left")) RotateLocal(Vector3.up, -rotationStep);
            if (GUILayout.Button("+ Right")) RotateLocal(Vector3.up, rotationStep);
            EditorGUILayout.EndHorizontal();

            // Roll (twist weapon)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Roll", GUILayout.Width(50));
            if (GUILayout.Button("- CCW")) RotateLocal(Vector3.forward, -rotationStep);
            if (GUILayout.Button("+ CW")) RotateLocal(Vector3.forward, rotationStep);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Show current euler angles (read-only, for reference)
            Vector3 euler = weaponRotation.eulerAngles;
            EditorGUILayout.LabelField($"Euler: ({NormalizeAngle(euler.x):F1}, {NormalizeAngle(euler.y):F1}, {NormalizeAngle(euler.z):F1})", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawSeparator();

            // ── SCALE ──
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scale = EditorGUILayout.Slider(scale, 0.5f, 2f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawSeparator();

            // ── APPLY ──
            GUI.enabled = weaponTransform != null;
            if (GUILayout.Button("Apply Now", GUILayout.Height(30)))
            {
                ApplyTransform();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // ── PRESETS ──
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Assault Rifle (Forward)"))
            {
                posX = 0.05f; posY = 0.0f; posZ = 0.1f;
                weaponRotation = Quaternion.Euler(-90f, 0f, 90f);
                scale = 1f;
            }

            if (GUILayout.Button("Assault Rifle (Low Ready)"))
            {
                posX = 0.08f; posY = -0.1f; posZ = 0.05f;
                weaponRotation = Quaternion.Euler(-110f, 0f, 90f);
                scale = 1f;
            }

            if (GUILayout.Button("Assault Rifle (High Ready)"))
            {
                posX = 0.03f; posY = 0.08f; posZ = 0.12f;
                weaponRotation = Quaternion.Euler(-85f, 0f, 90f);
                scale = 1f;
            }

            if (GUILayout.Button("Reset to Identity"))
            {
                posX = 0f; posY = 0f; posZ = 0f;
                weaponRotation = Quaternion.identity;
                scale = 1f;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawSeparator();

            // ── COPY VALUES ──
            EditorGUILayout.LabelField("Code Values", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Vector3 e = weaponRotation.eulerAngles;
            EditorGUILayout.SelectableLabel(
                $"Pos: new Vector3({posX:F3}f, {posY:F3}f, {posZ:F3}f)",
                GUILayout.Height(18));
            EditorGUILayout.SelectableLabel(
                $"Rot: Quaternion.Euler({NormalizeAngle(e.x):F1}f, {NormalizeAngle(e.y):F1}f, {NormalizeAngle(e.z):F1}f)",
                GUILayout.Height(18));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // ── SAVE ──
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Save to All Soldier Prefabs", GUILayout.Height(40)))
            {
                SaveToPrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
        }

        private void RotateLocal(Vector3 axis, float degrees)
        {
            weaponRotation = weaponRotation * Quaternion.AngleAxis(degrees, axis);
            if (autoUpdate) ApplyTransform();
            Repaint();
        }

        private float DrawSliderRow(string label, float value, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(15));
            value = EditorGUILayout.Slider(value, min, max);
            if (GUILayout.Button("0", GUILayout.Width(30))) value = 0f;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private void DrawSeparator()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        }

        private void ApplyTransform()
        {
            if (weaponTransform == null) return;

            if (!Application.isPlaying)
                Undo.RecordObject(weaponTransform, "Adjust Weapon Position");

            weaponTransform.localPosition = new Vector3(posX, posY, posZ);
            weaponTransform.localRotation = weaponRotation;
            weaponTransform.localScale = Vector3.one * scale;
        }

        private void SaveToPrefabs()
        {
            Vector3 e = weaponRotation.eulerAngles;
            if (!EditorUtility.DisplayDialog("Save to Prefabs",
                $"Apply these values to all soldier prefabs?\n\n" +
                $"Position: ({posX:F3}, {posY:F3}, {posZ:F3})\n" +
                $"Rotation: ({NormalizeAngle(e.x):F1}, {NormalizeAngle(e.y):F1}, {NormalizeAngle(e.z):F1})\n" +
                $"Scale: {scale:F2}",
                "Yes", "Cancel"))
            {
                return;
            }

            string[] soldierPaths = new string[]
            {
                "Assets/Elite_Soldiers/Prefab/Soldier_01.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_02.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_03.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_04.prefab"
            };

            int updatedCount = 0;

            foreach (string path in soldierPaths)
            {
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabAsset == null) continue;

                string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                Transform rightHand = FindRightHand(prefabContents.transform);
                if (rightHand == null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    continue;
                }

                Transform weapon = rightHand.Find("Weapon");
                if (weapon == null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    continue;
                }

                weapon.localPosition = new Vector3(posX, posY, posZ);
                weapon.localRotation = weaponRotation;
                weapon.localScale = Vector3.one * scale;

                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Updated weapon position in {updatedCount} soldier prefabs!\n\n" +
                $"Run: Tools > Recreate Units to see changes",
                "OK");
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private Transform FindRightHand(Transform root)
        {
            string[] handNames = new string[]
            {
                "RightHand", "Right Hand", "R_Hand", "RHand",
                "mixamorig:RightHand", "Hand_R", "right_hand"
            };

            foreach (string handName in handNames)
            {
                Transform hand = FindChildRecursive(root, handName);
                if (hand != null) return hand;
            }

            return FindHandRecursive(root, true);
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private Transform FindHandRecursive(Transform parent, bool isRight)
        {
            foreach (Transform child in parent)
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("hand"))
                {
                    if (isRight && (nameLower.Contains("right") || nameLower.Contains("_r") || nameLower.Contains("r_")))
                        return child;
                }

                Transform result = FindHandRecursive(child, isRight);
                if (result != null) return result;
            }
            return null;
        }
    }
}
