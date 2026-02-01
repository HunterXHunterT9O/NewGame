using UnityEngine;
using UnityEditor;

namespace EdgeOfUniverse.Editor.UIGenerator
{
    /// <summary>
    /// Helper window for setting up SkiaSharp in the Unity project.
    /// </summary>
    public class SkiaSharpSetupHelper : EditorWindow
    {
        private Vector2 scrollPos;
        private bool step1Complete;
        private bool step2Complete;
        private bool step3Complete;

        [MenuItem("Tools/Edge of Universe/SkiaSharp Setup Guide")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkiaSharpSetupHelper>("SkiaSharp Setup");
            window.minSize = new Vector2(450, 400);
        }

        private void OnEnable()
        {
            CheckSetupStatus();
        }

        private void CheckSetupStatus()
        {
            // Check if NuGetForUnity is available
            step1Complete = System.Type.GetType("NugetForUnity.NugetWindow, NuGetForUnity.Editor") != null;

            // Check if SkiaSharp define is set
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            step2Complete = defines.Contains("NUGET_SKIASHARP_INSTALLED");

            // If step 2 is complete, assume step 3 (actual installation) is also complete
            step3Complete = step2Complete;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("SkiaSharp Setup Guide", EditorStyles.boldLabel);
            GUILayout.Label("Follow these steps to enable high-quality UI generation", EditorStyles.miniLabel);
            GUILayout.Space(15);

            // Step 1: NuGetForUnity
            DrawStep(1, "Install NuGetForUnity", step1Complete, () =>
            {
                GUILayout.Label("NuGetForUnity has been added to your manifest.json.");
                GUILayout.Label("Unity should auto-import it. If not:");
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("Refresh Packages", GUILayout.Width(150)))
                {
                    UnityEditor.PackageManager.Client.Resolve();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("After import, you'll see 'NuGet' in the menu bar.");
            });

            GUILayout.Space(10);

            // Step 2: Install SkiaSharp via NuGet
            DrawStep(2, "Install SkiaSharp Package", step2Complete && step1Complete, () =>
            {
                GUILayout.Label("1. Go to: NuGet > Manage NuGet Packages");
                GUILayout.Label("2. Search for 'SkiaSharp'");
                GUILayout.Label("3. Install the latest stable version");
                GUILayout.Space(5);

                if (step1Complete)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button("Open NuGet Window", GUILayout.Width(150)))
                    {
                        EditorApplication.ExecuteMenuItem("NuGet/Manage NuGet Packages");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            });

            GUILayout.Space(10);

            // Step 3: Add Scripting Define
            DrawStep(3, "Add Scripting Define Symbol", step3Complete, () =>
            {
                GUILayout.Label("Add 'NUGET_SKIASHARP_INSTALLED' to Player Settings:");
                GUILayout.Label("Edit > Project Settings > Player > Scripting Define Symbols");
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("Add Define Automatically", GUILayout.Width(180)))
                {
                    AddSkiaSharpDefine();
                }
                if (GUILayout.Button("Open Player Settings", GUILayout.Width(150)))
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                }
                EditorGUILayout.EndHorizontal();
            });

            GUILayout.Space(20);

            // Status
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Status", GUILayout.Width(120)))
            {
                CheckSetupStatus();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (step1Complete && step2Complete && step3Complete)
            {
                EditorGUILayout.HelpBox("Setup complete! You can now use the UI Generator with full SkiaSharp features.", MessageType.Info);

                if (GUILayout.Button("Open UI Generator", GUILayout.Height(30)))
                {
                    SciFiUIGenerator.ShowWindow();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Complete all steps above to enable SkiaSharp features.\n\nNote: The UI Generator works without SkiaSharp using a fallback renderer, but with reduced visual quality.", MessageType.Warning);

                if (GUILayout.Button("Open UI Generator (Fallback Mode)", GUILayout.Height(30)))
                {
                    SciFiUIGenerator.ShowWindow();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStep(int stepNumber, string title, bool complete, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Status indicator
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.fontSize = 16;
            statusStyle.fontStyle = FontStyle.Bold;

            if (complete)
            {
                statusStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
                GUILayout.Label("[OK]", statusStyle, GUILayout.Width(40));
            }
            else
            {
                statusStyle.normal.textColor = new Color(0.8f, 0.6f, 0.2f);
                GUILayout.Label("[  ]", statusStyle, GUILayout.Width(40));
            }

            GUILayout.Label($"Step {stepNumber}: {title}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUI.indentLevel++;
            drawContent?.Invoke();
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void AddSkiaSharpDefine()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (!defines.Contains("NUGET_SKIASHARP_INSTALLED"))
            {
                if (!string.IsNullOrEmpty(defines))
                    defines += ";";
                defines += "NUGET_SKIASHARP_INSTALLED";

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
                Debug.Log("[SkiaSharp Setup] Added NUGET_SKIASHARP_INSTALLED define symbol");

                // Trigger recompilation
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("[SkiaSharp Setup] Define symbol already exists");
            }

            CheckSetupStatus();
        }
    }
}
