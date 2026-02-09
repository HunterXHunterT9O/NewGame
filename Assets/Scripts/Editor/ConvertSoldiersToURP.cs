using UnityEngine;
using UnityEditor;
using System.Linq;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Converts Elite Soldier materials from Built-in RP to URP
    /// </summary>
    public class ConvertSoldiersToURP : EditorWindow
    {
        [MenuItem("Tools/Edge of Universe/Fix Elite Soldier Materials")]
        public static void ShowWindow()
        {
            ConvertMaterials();
        }

        private static void ConvertMaterials()
        {
            string[] soldierPaths = new string[]
            {
                "Assets/Elite_Soldiers/Prefab/Soldier_01.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_02.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_03.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_04.prefab"
            };

            int fixedCount = 0;
            int totalMaterials = 0;

            foreach (string path in soldierPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Material Converter] Could not find prefab: {path}");
                    continue;
                }

                // Get all renderers in the prefab
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);

                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;

                        totalMaterials++;

                        // Check if material needs conversion
                        if (mat.shader.name.Contains("Standard") ||
                            mat.shader.name.Contains("Legacy") ||
                            mat.shader.name.Contains("Mobile") ||
                            !mat.shader.name.Contains("Universal Render Pipeline"))
                        {
                            // Save original properties
                            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                            Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

                            // Convert to URP Lit shader
                            mat.shader = Shader.Find("Universal Render Pipeline/Lit");

                            // Restore properties with URP naming
                            if (mat.HasProperty("_BaseColor"))
                                mat.SetColor("_BaseColor", color);

                            if (mainTex != null && mat.HasProperty("_BaseMap"))
                                mat.SetTexture("_BaseMap", mainTex);

                            if (normalMap != null && mat.HasProperty("_BumpMap"))
                                mat.SetTexture("_BumpMap", normalMap);

                            if (mat.HasProperty("_Metallic"))
                                mat.SetFloat("_Metallic", metallic);

                            if (mat.HasProperty("_Smoothness"))
                                mat.SetFloat("_Smoothness", smoothness);

                            EditorUtility.SetDirty(mat);
                            fixedCount++;

                            Debug.Log($"[Material Converter] Converted: {mat.name}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[Material Converter] Complete!</color>\n" +
                      $"Converted {fixedCount}/{totalMaterials} materials to URP.\n" +
                      $"Elite Soldiers are now ready to use.");

            EditorUtility.DisplayDialog("Material Conversion Complete",
                $"Converted {fixedCount} materials to URP.\n\nElite Soldiers are ready!",
                "OK");
        }
    }
}
