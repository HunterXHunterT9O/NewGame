using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Collections.Generic;

public class BuildingPieceGenerator : EditorWindow
{
    [MenuItem("Tools/Upgrade ALL Materials to URP")]
    static void UpgradeAllMaterialsToURP()
    {
        // Use Unity's official material upgrader API
        List<MaterialUpgrader> upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));

        if (upgraders == null || upgraders.Count == 0)
        {
            Debug.LogError("No material upgraders found for URP!");
            return;
        }

        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrading Materials to URP");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Materials upgraded to URP!");
        EditorUtility.DisplayDialog("URP Upgrade Complete", "All compatible materials have been upgraded to URP.", "OK");
    }

    [MenuItem("Tools/Setup Building System (Upgrade + Generate)")]
    static void SetupBuildingSystem()
    {
        UpgradeAllMaterialsToURP();
        GenerateBuildingPieces();
    }

    [MenuItem("Tools/Generate Building Pieces")]
    static void GenerateBuildingPieces()
    {
        // Ensure Resources folder exists
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        // SciFi Kit prefab path
        string prefabBasePath = "Assets/_Creepy_Cat/_3D Scifi Kit Vol 3/Prefabs/Builds";

        // Create building pieces
        CreatePiece("Foundation", BuildingCategory.Floors, $"{prefabBasePath}/P_Floor_01.prefab");
        CreatePiece("Foundation Large", BuildingCategory.Floors, $"{prefabBasePath}/P_Floor_Big_Normal_01.prefab");

        CreatePiece("Wall", BuildingCategory.Walls, $"{prefabBasePath}/P_Wall_Normal_Simple_01.prefab");
        CreatePiece("Wall Window", BuildingCategory.Walls, $"{prefabBasePath}/P_Wall_Window_Simple_01.prefab");
        CreatePiece("Wall Door", BuildingCategory.Walls, $"{prefabBasePath}/P_Wall_Door_Simple_01.prefab");
        CreatePiece("Wall Corner", BuildingCategory.Walls, $"{prefabBasePath}/P_Wall_Coin_Simple_01.prefab");

        CreatePiece("Floor", BuildingCategory.Floors, $"{prefabBasePath}/P_Floor_02.prefab");
        CreatePiece("Floor Border", BuildingCategory.Floors, $"{prefabBasePath}/P_Floor_Border_Line_01.prefab");

        CreatePiece("Roof", BuildingCategory.Roofs, $"{prefabBasePath}/P_Roof_01_A.prefab");
        CreatePiece("Roof Corner", BuildingCategory.Roofs, $"{prefabBasePath}/P_Roof_Border_Corner_01.prefab");

        CreatePiece("Stairs", BuildingCategory.Props, $"{prefabBasePath}/P_Stair_Ext_01.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Building pieces generated successfully!");
        EditorUtility.DisplayDialog("Success", "Building pieces have been generated in Assets/Resources", "OK");
    }

    static void CreatePiece(string name, BuildingCategory category, string prefabPath)
    {
        // Load the prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Could not find prefab at {prefabPath}");
            return;
        }

        // Create the ScriptableObject
        BuildingPiece piece = ScriptableObject.CreateInstance<BuildingPiece>();
        piece.pieceName = name;
        piece.category = category;
        piece.prefab = prefab;
        piece.canRotate = true;

        // Save it
        string assetPath = $"Assets/Resources/{name.Replace(" ", "_")}.asset";
        AssetDatabase.CreateAsset(piece, assetPath);

        Debug.Log($"Created {assetPath}");
    }
}
