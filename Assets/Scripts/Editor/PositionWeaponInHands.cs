using UnityEngine;
using UnityEditor;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Helps position weapons correctly in soldier hands by analyzing animation poses
    /// </summary>
    public class PositionWeaponInHands : EditorWindow
    {
        private GameObject soldierPrefab;
        private Vector3 weaponPosition = new Vector3(0.02f, -0.05f, 0.08f);
        private Vector3 weaponRotation = new Vector3(0f, 90f, 180f);
        private Vector3 weaponScale = Vector3.one;

        [MenuItem("Tools/Edge of Universe/Position Weapon in Hands")]
        public static void ShowWindow()
        {
            GetWindow<PositionWeaponInHands>("Weapon Positioning");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Position Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            soldierPrefab = (GameObject)EditorGUILayout.ObjectField("Soldier Prefab", soldierPrefab, typeof(GameObject), false);

            GUILayout.Space(10);
            GUILayout.Label("Weapon Transform (Local to Right Hand)", EditorStyles.boldLabel);

            weaponPosition = EditorGUILayout.Vector3Field("Position", weaponPosition);
            weaponRotation = EditorGUILayout.Vector3Field("Rotation", weaponRotation);
            weaponScale = EditorGUILayout.Vector3Field("Scale", weaponScale);

            GUILayout.Space(10);

            if (GUILayout.Button("Apply to Selected Soldier in Scene", GUILayout.Height(30)))
            {
                ApplyToSelectedInScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Apply to All Soldier Prefabs", GUILayout.Height(30)))
            {
                ApplyToAllPrefabs();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "1. Load a soldier prefab above\n" +
                "2. Place soldier in scene to test\n" +
                "3. Adjust position/rotation values\n" +
                "4. Click 'Apply to Selected' to update scene instance\n" +
                "5. Once happy, click 'Apply to All Prefabs'",
                MessageType.Info);

            GUILayout.Space(10);

            // Presets
            GUILayout.Label("Presets:", EditorStyles.boldLabel);
            if (GUILayout.Button("Preset: Assault Rifle (Forward)"))
            {
                weaponPosition = new Vector3(0.05f, 0.0f, 0.1f);
                weaponRotation = new Vector3(-90f, 0f, 90f);
                weaponScale = Vector3.one;
            }
            if (GUILayout.Button("Preset: Assault Rifle (Aimed)"))
            {
                weaponPosition = new Vector3(0.0f, 0.05f, 0.15f);
                weaponRotation = new Vector3(-85f, 0f, 90f);
                weaponScale = Vector3.one;
            }
            if (GUILayout.Button("Preset: Low Ready"))
            {
                weaponPosition = new Vector3(0.08f, -0.1f, 0.05f);
                weaponRotation = new Vector3(-110f, 0f, 90f);
                weaponScale = Vector3.one;
            }
        }

        private void ApplyToSelectedInScene()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a soldier in the scene", "OK");
                return;
            }

            Transform rightHand = FindRightHand(selected.transform);
            if (rightHand == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find right hand bone", "OK");
                return;
            }

            Transform weapon = rightHand.Find("Weapon");
            if (weapon == null)
            {
                EditorUtility.DisplayDialog("Error", "No weapon found in right hand", "OK");
                return;
            }

            Undo.RecordObject(weapon, "Adjust Weapon Position");
            weapon.localPosition = weaponPosition;
            weapon.localRotation = Quaternion.Euler(weaponRotation);
            weapon.localScale = weaponScale;

            Debug.Log($"[Weapon Position] Applied to {selected.name}");
        }

        private void ApplyToAllPrefabs()
        {
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

                weapon.localPosition = weaponPosition;
                weapon.localRotation = Quaternion.Euler(weaponRotation);
                weapon.localScale = weaponScale;

                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                updatedCount++;
                Debug.Log($"[Weapon Position] Updated {prefabAsset.name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Updated weapon position in {updatedCount} soldier prefabs!\n\n" +
                $"Position: {weaponPosition}\n" +
                $"Rotation: {weaponRotation}\n\n" +
                $"Run: Tools > Recreate Units",
                "OK");
        }

        private Transform FindRightHand(Transform root)
        {
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
                if (result != null)
                    return result;
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
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
