#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor menu items for RTS scene setup.
/// </summary>
public static class RTSSceneSetupEditor
{
    [MenuItem("Tools/RTS/Create RTS Test Scene")]
    public static void CreateRTSTestScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create setup object
        GameObject setupObj = new GameObject("RTSSceneSetup");
        var setup = setupObj.AddComponent<RTSSceneSetup>();

        // Run setup
        setup.SetupScene();

        // Select the setup object
        Selection.activeGameObject = setupObj;

        Debug.Log("[RTSSceneSetupEditor] RTS test scene created. Remember to bake NavMesh (Window > AI > Navigation).");
    }

    [MenuItem("Tools/RTS/Add RTS Systems to Current Scene")]
    public static void AddRTSSystemsToScene()
    {
        // Add camera if missing
        if (Object.FindAnyObjectByType<RTSCameraController>() == null)
        {
            GameObject rigObj = new GameObject("RTSCameraRig");
            rigObj.AddComponent<RTSCameraController>();
            Debug.Log("[RTSSceneSetupEditor] Added RTSCameraController");
        }

        // Add selection system if missing
        if (Object.FindAnyObjectByType<UnitSelectionSystem>() == null)
        {
            GameObject sysObj = new GameObject("UnitSelectionSystem");
            sysObj.AddComponent<UnitSelectionSystem>();
            Debug.Log("[RTSSceneSetupEditor] Added UnitSelectionSystem");
        }

        Debug.Log("[RTSSceneSetupEditor] RTS systems added to scene.");
    }

    [MenuItem("Tools/RTS/Make Selected Objects Selectable")]
    public static void MakeSelectedObjectsSelectable()
    {
        int count = 0;
        foreach (var obj in Selection.gameObjects)
        {
            if (obj.GetComponent<SelectableUnit>() == null)
            {
                obj.AddComponent<SelectableUnit>();
                count++;
            }

            // Add collider if missing
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<CapsuleCollider>();
            }
        }

        Debug.Log($"[RTSSceneSetupEditor] Made {count} objects selectable.");
    }

    [MenuItem("Tools/RTS/Make Selected Objects Moveable")]
    public static void MakeSelectedObjectsMoveable()
    {
        int count = 0;
        foreach (var obj in Selection.gameObjects)
        {
            // Add SelectableUnit if missing
            if (obj.GetComponent<SelectableUnit>() == null)
            {
                obj.AddComponent<SelectableUnit>();
            }

            // Add collider if missing
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<CapsuleCollider>();
            }

            // Add NavMeshAgent if missing
            if (obj.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            {
                obj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }

            // Add UnitMovement if missing
            if (obj.GetComponent<UnitMovement>() == null)
            {
                obj.AddComponent<UnitMovement>();
                count++;
            }
        }

        Debug.Log($"[RTSSceneSetupEditor] Made {count} objects moveable.");
    }
}
#endif
