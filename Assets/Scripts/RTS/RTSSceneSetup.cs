using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Creates a test scene for RTS camera and unit selection.
/// Attach to an empty GameObject or use the menu item.
/// </summary>
public class RTSSceneSetup : MonoBehaviour
{
    [Header("Ground Settings")]
    [SerializeField] private float groundSize = 100f;
    [SerializeField] private Color groundColor = new Color(0.3f, 0.35f, 0.3f);

    [Header("Unit Settings")]
    [SerializeField] private int unitCount = 5;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private Color unitColor = new Color(0.2f, 0.4f, 0.8f);

    [Header("Camera Settings")]
    [SerializeField] private float initialHeight = 15f;

    private void Start()
    {
        SetupScene();
    }

    [ContextMenu("Setup RTS Scene")]
    public void SetupScene()
    {
        SetupLighting();
        SetupGround();
        SetupNavMesh();
        SetupCamera();
        SetupSelectionSystem();
        SpawnTestUnits();

        Debug.Log("[RTSSceneSetup] Scene setup complete. Controls:\n" +
                  "- WASD/Arrows: Pan camera\n" +
                  "- Mouse wheel: Zoom\n" +
                  "- Middle mouse: Rotate\n" +
                  "- Left click: Select unit\n" +
                  "- Shift+click: Add to selection\n" +
                  "- Drag: Box select\n" +
                  "- Right click: Move selected units\n" +
                  "- Space: Focus on selection\n" +
                  "- Ctrl+A: Select all");
    }

    private void SetupLighting()
    {
        // Find or create directional light
        Light sun = FindAnyObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Sun");
            sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sun.intensity = 1f;
        sun.shadows = LightShadows.Soft;
        sun.color = new Color(1f, 0.95f, 0.9f);
    }

    private void SetupGround()
    {
        // Check for existing ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
        }

        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(groundSize / 10f, 1f, groundSize / 10f);

        // Set up material
        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = groundColor;
            renderer.material = mat;
        }

        // Mark as ground layer
        ground.layer = LayerMask.NameToLayer("Default");

        // Ensure it's static for NavMesh
        ground.isStatic = true;
    }

    private void SetupNavMesh()
    {
        // Add NavMeshSurface if AI Navigation package is available
        var ground = GameObject.Find("Ground");
        if (ground == null) return;

        // Check if NavMeshSurface component type exists
        var navSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navSurfaceType != null)
        {
            var existingSurface = ground.GetComponent(navSurfaceType);
            if (existingSurface == null)
            {
                ground.AddComponent(navSurfaceType);
                Debug.Log("[RTSSceneSetup] NavMeshSurface added. Bake it via Window > AI > Navigation.");
            }
        }
        else
        {
            Debug.LogWarning("[RTSSceneSetup] AI Navigation package not found. Please install it via Package Manager and bake NavMesh manually.");
        }
    }

    private void SetupCamera()
    {
        // Remove existing cameras
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.GetComponentInParent<RTSCameraController>() == null)
            {
                DestroyImmediate(cam.gameObject);
            }
        }

        // Find or create camera rig
        RTSCameraController cameraController = FindAnyObjectByType<RTSCameraController>();
        if (cameraController == null)
        {
            GameObject rigObj = new GameObject("RTSCameraRig");
            cameraController = rigObj.AddComponent<RTSCameraController>();
        }

        cameraController.transform.position = new Vector3(0f, 0f, 0f);
    }

    private void SetupSelectionSystem()
    {
        // Find or create selection system
        UnitSelectionSystem selectionSystem = FindAnyObjectByType<UnitSelectionSystem>();
        if (selectionSystem == null)
        {
            GameObject sysObj = new GameObject("UnitSelectionSystem");
            selectionSystem = sysObj.AddComponent<UnitSelectionSystem>();
        }
    }

    private void SpawnTestUnits()
    {
        // Clear existing test units
        foreach (var unit in FindObjectsByType<SelectableUnit>(FindObjectsSortMode.None))
        {
            if (unit.name.StartsWith("TestUnit"))
            {
                DestroyImmediate(unit.gameObject);
            }
        }

        // Spawn new units in formation
        for (int i = 0; i < unitCount; i++)
        {
            float angle = (i / (float)unitCount) * Mathf.PI * 2f;
            float radius = spawnRadius * (0.5f + Random.value * 0.5f);
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * radius,
                0.5f,
                Mathf.Sin(angle) * radius
            );

            CreateTestUnit(pos, i);
        }

        // Refresh selection system
        var selectionSystem = FindAnyObjectByType<UnitSelectionSystem>();
        if (selectionSystem != null)
        {
            selectionSystem.RefreshUnitList();
        }
    }

    private void CreateTestUnit(Vector3 position, int index)
    {
        // Create capsule as placeholder unit
        GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        unit.name = $"TestUnit_{index}";
        unit.transform.position = position;

        // Set color
        var renderer = unit.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = unitColor;
            renderer.material = mat;
        }

        // Add required components
        unit.AddComponent<SelectableUnit>();

        // Add NavMeshAgent for movement
        NavMeshAgent agent = unit.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.speed = 5f;
        agent.angularSpeed = 120f;

        // Add movement component
        unit.AddComponent<UnitMovement>();
    }

    private void OnDrawGizmos()
    {
        // Draw spawn area
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(Vector3.zero, spawnRadius);
    }
}
