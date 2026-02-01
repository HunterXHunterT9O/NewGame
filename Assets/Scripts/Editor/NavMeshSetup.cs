using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Editor utility to set up and bake NavMesh for RTS unit movement.
    /// </summary>
    public class NavMeshSetup : EditorWindow
    {
        private float agentRadius = 0.5f;
        private float agentHeight = 2f;
        private float maxSlope = 45f;
        private float stepHeight = 0.4f;

        [MenuItem("Tools/Edge of Universe/Setup NavMesh")]
        public static void ShowWindow()
        {
            GetWindow<NavMeshSetup>("NavMesh Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("NavMesh Setup for RTS", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "1. Find or create a ground object\n" +
                "2. Add NavMeshSurface component\n" +
                "3. Bake the NavMesh for unit pathfinding",
                MessageType.Info);

            GUILayout.Space(10);
            GUILayout.Label("Agent Settings", EditorStyles.boldLabel);

            agentRadius = EditorGUILayout.FloatField("Agent Radius", agentRadius);
            agentHeight = EditorGUILayout.FloatField("Agent Height", agentHeight);
            maxSlope = EditorGUILayout.Slider("Max Slope", maxSlope, 0f, 60f);
            stepHeight = EditorGUILayout.FloatField("Step Height", stepHeight);

            GUILayout.Space(20);

            if (GUILayout.Button("Setup & Bake NavMesh", GUILayout.Height(40)))
            {
                SetupAndBakeNavMesh();
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add NavMeshSurface Only"))
            {
                AddNavMeshSurface();
            }
            if (GUILayout.Button("Bake Only"))
            {
                BakeNavMesh();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Clear NavMesh"))
            {
                ClearNavMesh();
            }
        }

        private void SetupAndBakeNavMesh()
        {
            // Find or create ground
            GameObject ground = FindOrCreateGround();
            if (ground == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find or create ground object.", "OK");
                return;
            }

            // Ensure ground is static
            ground.isStatic = true;

            // Add NavMeshSurface
            NavMeshSurface surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = ground.AddComponent<NavMeshSurface>();
                Debug.Log("[NavMesh Setup] Added NavMeshSurface to ground.");
            }

            // Configure surface
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

            // Set agent settings
            surface.agentTypeID = GetOrCreateAgentType();

            // Bake
            surface.BuildNavMesh();

            Debug.Log("[NavMesh Setup] NavMesh baked successfully!");
            EditorUtility.DisplayDialog("Success", "NavMesh has been baked!\n\nUnits with NavMeshAgent can now pathfind.", "OK");

            // Select the ground to show the NavMesh in scene view
            Selection.activeGameObject = ground;
        }

        private void AddNavMeshSurface()
        {
            GameObject ground = FindOrCreateGround();
            if (ground == null) return;

            ground.isStatic = true;

            NavMeshSurface surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = ground.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                surface.agentTypeID = GetOrCreateAgentType();

                Debug.Log("[NavMesh Setup] NavMeshSurface added. Click 'Bake Only' or use Window > AI > Navigation to bake.");
            }
            else
            {
                Debug.Log("[NavMesh Setup] NavMeshSurface already exists.");
            }

            Selection.activeGameObject = ground;
        }

        private void BakeNavMesh()
        {
            NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                EditorUtility.DisplayDialog("Error", "No NavMeshSurface found in scene.\nClick 'Add NavMeshSurface Only' first.", "OK");
                return;
            }

            surface.BuildNavMesh();
            Debug.Log("[NavMesh Setup] NavMesh baked!");
        }

        private void ClearNavMesh()
        {
            NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
            {
                surface.RemoveData();
                Debug.Log("[NavMesh Setup] NavMesh cleared.");
            }
        }

        private GameObject FindOrCreateGround()
        {
            // Look for existing ground
            GameObject ground = GameObject.Find("Ground");
            if (ground != null) return ground;

            // Look for any large plane/terrain
            foreach (var terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                return terrain.gameObject;
            }

            // Look for objects named floor, terrain, etc.
            string[] groundNames = { "Floor", "Terrain", "Platform", "Base" };
            foreach (var name in groundNames)
            {
                ground = GameObject.Find(name);
                if (ground != null) return ground;
            }

            // Create a default ground
            if (EditorUtility.DisplayDialog("No Ground Found",
                "No ground object found in the scene.\n\nCreate a default ground plane?",
                "Create", "Cancel"))
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 units
                ground.isStatic = true;

                // Apply a basic material
                var renderer = ground.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.3f, 0.35f, 0.3f);
                    renderer.material = mat;
                }

                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
                return ground;
            }

            return null;
        }

        private int GetOrCreateAgentType()
        {
            // Get the Humanoid agent type (default)
            // In a more complex setup, you'd create custom agent types
            var settings = NavMesh.GetSettingsByIndex(0);
            return settings.agentTypeID;
        }

        [MenuItem("Tools/Edge of Universe/Quick Bake NavMesh %&n")]
        public static void QuickBake()
        {
            NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                Debug.Log("[NavMesh Setup] Quick bake complete!");
            }
            else
            {
                Debug.LogWarning("[NavMesh Setup] No NavMeshSurface in scene. Use Tools > Edge of Universe > Setup NavMesh first.");
            }
        }
    }
}
