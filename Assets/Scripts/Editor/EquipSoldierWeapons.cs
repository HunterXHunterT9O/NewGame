using UnityEngine;
using UnityEditor;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Attaches weapons to soldier hands
    /// </summary>
    public class EquipSoldierWeapons : EditorWindow
    {
        [MenuItem("Tools/Edge of Universe/Equip Soldier Weapons")]
        public static void EquipWeapons()
        {
            // Load weapon models
            string[] weaponPaths = new string[]
            {
                "Assets/Protofactor/Sci Fi/Common/Weapons/FBX Files/SM_SciFiAssaultRifle_01.FBX",
                "Assets/Protofactor/Sci Fi/Common/Weapons/FBX Files/SM_SciFiAssaultRifle_02.FBX"
            };

            // Soldier prefab paths
            string[] soldierPaths = new string[]
            {
                "Assets/Elite_Soldiers/Prefab/Soldier_01.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_02.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_03.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_04.prefab"
            };

            int equippedCount = 0;

            for (int i = 0; i < soldierPaths.Length; i++)
            {
                // Load soldier prefab
                string soldierPath = soldierPaths[i];
                GameObject soldierPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(soldierPath);
                if (soldierPrefabAsset == null)
                {
                    Debug.LogWarning($"[Weapon Equip] Could not find soldier prefab: {soldierPath}");
                    continue;
                }

                // Pick weapon variant
                int weaponIndex = i % weaponPaths.Length;
                GameObject weaponModel = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPaths[weaponIndex]);

                if (weaponModel == null)
                {
                    Debug.LogWarning($"[Weapon Equip] Could not find weapon: {weaponPaths[weaponIndex]}");
                    continue;
                }

                // Load prefab contents for editing
                string prefabPath = AssetDatabase.GetAssetPath(soldierPrefabAsset);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                // Find right hand bone
                Transform rightHand = FindRightHand(prefabContents.transform);

                if (rightHand == null)
                {
                    Debug.LogWarning($"[Weapon Equip] Could not find right hand bone for {soldierPrefabAsset.name}");
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    continue;
                }

                // Remove existing weapon if any
                Transform existingWeapon = rightHand.Find("Weapon");
                if (existingWeapon != null)
                {
                    DestroyImmediate(existingWeapon.gameObject);
                }

                // Create weapon GameObject (copy from weapon model)
                GameObject weapon = new GameObject("Weapon");
                weapon.transform.SetParent(rightHand);
                // Position for assault rifle idle aim pose
                weapon.transform.localPosition = new Vector3(0.05f, 0.0f, 0.1f);
                weapon.transform.localRotation = Quaternion.Euler(-90f, 0f, 90f);
                weapon.transform.localScale = Vector3.one;

                // Copy mesh and materials from weapon model
                MeshFilter weaponMeshFilter = weaponModel.GetComponentInChildren<MeshFilter>();
                MeshRenderer weaponMeshRenderer = weaponModel.GetComponentInChildren<MeshRenderer>();

                if (weaponMeshFilter != null && weaponMeshRenderer != null)
                {
                    MeshFilter mf = weapon.AddComponent<MeshFilter>();
                    MeshRenderer mr = weapon.AddComponent<MeshRenderer>();

                    mf.sharedMesh = weaponMeshFilter.sharedMesh;
                    mr.sharedMaterials = weaponMeshRenderer.sharedMaterials;

                    // Convert materials to URP
                    ConvertWeaponMaterialsToURP(weapon);
                }

                // Save the modified prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                equippedCount++;
                Debug.Log($"[Weapon Equip] Equipped {soldierPrefabAsset.name} with {weaponModel.name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Weapons Equipped",
                $"Successfully equipped {equippedCount} soldiers with assault rifles!\n\n" +
                $"Now run: Tools > Recreate Units",
                "OK");
        }

        private static Transform FindRightHand(Transform root)
        {
            // Common bone names for right hand
            string[] handNames = new string[]
            {
                "RightHand",
                "Right Hand",
                "R_Hand",
                "RHand",
                "mixamorig:RightHand",
                "Hand_R"
            };

            foreach (string handName in handNames)
            {
                Transform hand = root.Find(handName);
                if (hand != null) return hand;

                // Recursive search
                hand = FindChildRecursive(root, handName);
                if (hand != null) return hand;
            }

            // Fallback: search for any transform with "hand" and "right" in name
            return FindHandRecursive(root, true);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static Transform FindHandRecursive(Transform parent, bool isRight)
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
                if (result != null)
                    return result;
            }
            return null;
        }

        private static void ConvertWeaponMaterialsToURP(GameObject weapon)
        {
            Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;

                    if (mat.shader.name.Contains("Standard") ||
                        mat.shader.name.Contains("Legacy") ||
                        !mat.shader.name.Contains("Universal Render Pipeline"))
                    {
                        // Save properties
                        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                        float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                        float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

                        // Convert to URP
                        mat.shader = Shader.Find("Universal Render Pipeline/Lit");

                        // Restore properties
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
                    }
                }
            }
        }
    }
}
