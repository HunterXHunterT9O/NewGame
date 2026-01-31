using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class SquadPreviewSetup : EditorWindow
{
    [MenuItem("Tools/Create Squad Preview Scene")]
    static void CreateSquadPreviewScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);

        // Apply dark material to ground
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Material groundMat = CreateURPMaterial(new Color(0.15f, 0.15f, 0.2f, 1f));
        groundRenderer.material = groundMat;

        // Create directional light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = new Color(1f, 0.95f, 0.9f);
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Add ambient fill light
        GameObject fillLight = new GameObject("Fill Light");
        Light fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.3f;
        fill.color = new Color(0.6f, 0.7f, 1f);
        fillLight.transform.rotation = Quaternion.Euler(30, 150, 0);

        // Create camera - positioned to look at soldiers walking toward it
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        camObj.transform.position = new Vector3(0, 1.5f, -2f);
        camObj.transform.rotation = Quaternion.Euler(5, 0, 0);
        cam.fieldOfView = 60f;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        // Add AudioListener for the main camera
        camObj.AddComponent<AudioListener>();

        // Create Squad Manager object
        GameObject squadManager = new GameObject("SquadPreviewManager");
        SquadPreviewScene previewScript = squadManager.AddComponent<SquadPreviewScene>();

        // Load soldier prefabs
        string[] soldierPaths = new string[]
        {
            "Assets/Protofactor/Sci Fi/SciFiCharactersMegaPackVol3/SciFiShooterCharactersPackVol3/SciFiHeavyBattleArmor/Prefabs/SciFiHeavyBattleArmor_ForestCamo Variant.prefab",
            "Assets/Protofactor/Sci Fi/SciFiCharactersMegaPackVol3/SciFiShooterCharactersPackVol3/SciFiHeavyBattleArmor/Prefabs/SciFiHeavyBattleArmor_DesertCamo Variant.prefab",
            "Assets/Protofactor/Sci Fi/SciFiCharactersMegaPackVol3/SciFiShooterCharactersPackVol3/SciFiHeavyBattleArmor/Prefabs/SciFiHeavyBattleArmor_WinterCamo Variant.prefab",
            "Assets/Protofactor/Sci Fi/SciFiCharactersMegaPackVol3/SciFiShooterCharactersPackVol3/SciFiSoldier_01/Prefabs/SciFiSoldier_01_ForestCamo Variant.prefab"
        };

        // Use SerializedObject to set the list
        SerializedObject so = new SerializedObject(previewScript);
        SerializedProperty soldierListProp = so.FindProperty("soldierPrefabs");
        soldierListProp.ClearArray();

        int loadedCount = 0;
        foreach (string path in soldierPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                soldierListProp.InsertArrayElementAtIndex(loadedCount);
                soldierListProp.GetArrayElementAtIndex(loadedCount).objectReferenceValue = prefab;
                loadedCount++;
                Debug.Log($"Loaded soldier: {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"Could not find prefab: {path}");
            }
        }

        // Try to load existing Protofactor animator controller
        string existingControllerPath = "Assets/Protofactor/Sci Fi/Common/Humanoid_Controller.controller";
        var existingController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(existingControllerPath);

        if (existingController != null)
        {
            // Assign as override controller
            SerializedProperty overrideControllerProp = so.FindProperty("overrideController");
            overrideControllerProp.objectReferenceValue = existingController;
            Debug.Log($"Assigned Humanoid_Controller as override");
        }
        else
        {
            Debug.LogWarning("Could not find Humanoid_Controller. Creating custom one...");
            CreateSoldierAnimatorController();
        }

        so.ApplyModifiedProperties();

        // Save scene
        string scenePath = "Assets/Scenes/SquadPreview.unity";

        // Ensure Scenes folder exists
        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        EditorSceneManager.SaveScene(newScene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"Squad Preview Scene created at {scenePath}");
        Debug.Log($"Loaded {loadedCount} soldiers. Select SquadPreviewManager to configure.");

        string controllerStatus = existingController != null
            ? "Humanoid_Controller assigned automatically."
            : "Custom SoldierAnimator controller created.";

        EditorUtility.DisplayDialog("Squad Preview Created",
            $"Scene created at {scenePath}\n\n" +
            $"Loaded {loadedCount} soldier prefabs.\n" +
            $"{controllerStatus}\n\n" +
            "Enter Play mode to see the squad walk in!", "OK");
    }

    static void CreateSoldierAnimatorController()
    {
        string controllerPath = "Assets/Animations/SoldierAnimator.controller";

        // Ensure folder exists
        if (!Directory.Exists("Assets/Animations"))
        {
            Directory.CreateDirectory("Assets/Animations");
        }

        // Check if controller already exists
        var existingController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (existingController != null)
        {
            Debug.Log("SoldierAnimator controller already exists");
            return;
        }

        // Create new animator controller
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Load animation clips
        string animBasePath = "Assets/Protofactor/Sci Fi/Common/Animations/";

        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBasePath + "Humanoid@IdleUnarmed.FBX");
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBasePath + "Humanoid@WalkForwardUnarmed.FBX");

        // If direct clip load fails, try loading from FBX
        if (idleClip == null)
        {
            var idleFbx = AssetDatabase.LoadAllAssetsAtPath(animBasePath + "Humanoid@IdleUnarmed.FBX");
            foreach (var asset in idleFbx)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    idleClip = clip;
                    break;
                }
            }
        }

        if (walkClip == null)
        {
            var walkFbx = AssetDatabase.LoadAllAssetsAtPath(animBasePath + "Humanoid@WalkForwardUnarmed.FBX");
            foreach (var asset in walkFbx)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    walkClip = clip;
                    break;
                }
            }
        }

        // Create states
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 50, 0));
        var walkState = rootStateMachine.AddState("Walk", new Vector3(300, 150, 0));

        if (idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log($"Assigned idle clip: {idleClip.name}");
        }
        else
        {
            Debug.LogWarning("Could not find idle animation clip");
        }

        if (walkClip != null)
        {
            walkState.motion = walkClip;
            Debug.Log($"Assigned walk clip: {walkClip.name}");
        }
        else
        {
            Debug.LogWarning("Could not find walk animation clip");
        }

        // Set idle as default
        rootStateMachine.defaultState = idleState;

        // Create transitions
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsWalking");
        idleToWalk.duration = 0.25f;
        idleToWalk.hasExitTime = false;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsWalking");
        walkToIdle.duration = 0.25f;
        walkToIdle.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Debug.Log($"Created animator controller at {controllerPath}");
    }

    static Material CreateURPMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            Debug.LogError("URP shaders not found!");
            return null;
        }

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        return mat;
    }
}
