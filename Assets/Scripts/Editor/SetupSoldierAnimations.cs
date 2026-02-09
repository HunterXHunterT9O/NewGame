using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Creates an Animator Controller for Elite Soldiers using Protofactor humanoid animations
    /// </summary>
    public class SetupSoldierAnimations : EditorWindow
    {
        [MenuItem("Tools/Edge of Universe/Setup Soldier Animations")]
        public static void CreateAnimatorController()
        {
            string animPath = "Assets/Protofactor/Sci Fi/Common/Animations";
            string outputPath = "Assets/Animations";

            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            // Load animation clips
            AnimationClip idle = LoadAnimationClip($"{animPath}/Humanoid@IdleAimAssaultRifle.FBX");
            AnimationClip walk = LoadAnimationClip($"{animPath}/Humanoid@WalkForwardAssaultRifle.FBX");
            AnimationClip run = LoadAnimationClip($"{animPath}/Humanoid@RunForwardAssaultRifle.FBX");

            if (idle == null || walk == null || run == null)
            {
                Debug.LogError("[Animation Setup] Could not load required animations. Make sure Protofactor animations are imported.");
                EditorUtility.DisplayDialog("Error", "Could not find animation files. Make sure Protofactor Sci-Fi pack is imported.", "OK");
                return;
            }

            // Create Animator Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{outputPath}/SoldierController.controller");

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            // Get root state machine
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Create states
            AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(250, 0, 0));
            idleState.motion = idle;

            AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(250, 100, 0));
            walkState.motion = walk;

            AnimatorState runState = rootStateMachine.AddState("Run", new Vector3(250, 200, 0));
            runState.motion = run;

            // Set default state
            rootStateMachine.defaultState = idleState;

            // Create transitions: Idle <-> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.2f;

            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.2f;

            // Create transitions: Walk <-> Run
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.2f;

            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.2f;

            // Create transition: Run -> Idle (direct)
            AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.2f;

            // Save
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[Animation Setup] Created SoldierController.controller</color>\n" +
                      $"Location: {outputPath}/SoldierController.controller\n" +
                      $"States: Idle, Walk, Run\n" +
                      $"Parameters: Speed, IsMoving");

            // Now assign to soldier prefabs
            AssignControllerToSoldiers(controller);
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void AssignControllerToSoldiers(AnimatorController controller)
        {
            string[] soldierPaths = new string[]
            {
                "Assets/Elite_Soldiers/Prefab/Soldier_01.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_02.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_03.prefab",
                "Assets/Elite_Soldiers/Prefab/Soldier_04.prefab"
            };

            int assignedCount = 0;

            foreach (string path in soldierPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                Animator animator = prefab.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(prefab);
                    assignedCount++;
                    Debug.Log($"[Animation Setup] Assigned controller to {prefab.name}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Animation Setup Complete",
                $"Created Soldier Animation Controller!\n\n" +
                $"• Assigned to {assignedCount} soldier prefabs\n" +
                $"• States: Idle, Walk, Run\n" +
                $"• Now run: Tools > Recreate Units",
                "OK");
        }
    }
}
