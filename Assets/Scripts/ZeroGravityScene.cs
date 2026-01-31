using UnityEngine;

public class ZeroGravityScene : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int objectCount = 30;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 2f);

    [Header("Float Settings")]
    [SerializeField] private float floatStrength = 5f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float noiseSpeed = 0.3f;

    [Header("Boundary")]
    [SerializeField] private float boundaryRadius = 20f;
    [SerializeField] private float boundaryForce = 10f;

    [Header("Player")]
    [SerializeField] private bool spawnPlayer = true;
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0, 1.5f, -10f);

    [Header("Ground")]
    [SerializeField] private bool spawnGround = true;
    [SerializeField] private float groundSize = 100f;

    [Header("UI")]
    [SerializeField] private bool spawnUI = true;

    private Color[] colors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),    // Red
        new Color(0.3f, 1f, 0.3f),    // Green
        new Color(0.3f, 0.5f, 1f),    // Blue
        new Color(1f, 1f, 0.3f),      // Yellow
        new Color(1f, 0.5f, 0f),      // Orange
        new Color(0.8f, 0.3f, 1f),    // Purple
        new Color(0.3f, 1f, 1f),      // Cyan
        new Color(1f, 0.5f, 0.7f)     // Pink
    };

    void Start()
    {
        // Disable global gravity
        Physics.gravity = Vector3.zero;

        // Disable any existing main camera
        Camera existingCam = Camera.main;
        if (existingCam != null && spawnPlayer)
        {
            existingCam.gameObject.SetActive(false);
        }

        SetupLighting();

        if (spawnGround)
        {
            SpawnGround();
        }

        if (spawnPlayer)
        {
            SpawnPlayer();
        }

        if (spawnUI)
        {
            SpawnUI();
        }

        SpawnFloatingObjects();
    }

    void SpawnPlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = playerSpawnPosition;
        player.transform.rotation = Quaternion.identity;
        player.AddComponent<FirstPersonController>();
        player.tag = "Player";
    }

    void SpawnGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(groundSize, 0.5f, groundSize);

        // Apply a grid-like material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
        renderer.material = mat;

        // Make it static for better performance
        ground.isStatic = true;
    }

    void SpawnUI()
    {
        GameObject uiObj = new GameObject("PlayerStatusUI");
        uiObj.AddComponent<PlayerStatusUI>();
    }

    void SetupLighting()
    {
        // Create main directional light if none exists
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasDirectional = false;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                hasDirectional = true;
                break;
            }
        }

        if (!hasDirectional)
        {
            GameObject lightObj = new GameObject("Main Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.95f, 0.9f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // Set ambient light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.2f);
    }

    void SpawnFloatingObjects()
    {
        for (int i = 0; i < objectCount; i++)
        {
            PrimitiveType shapeType = GetRandomShape();
            GameObject obj = GameObject.CreatePrimitive(shapeType);

            // Random position within spawn radius, offset above ground
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            randomPos.y = Mathf.Abs(randomPos.y) + 5f; // Keep above ground
            obj.transform.position = randomPos;

            // Random rotation
            obj.transform.rotation = Random.rotation;

            // Random scale
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            obj.transform.localScale = Vector3.one * scale;

            // Setup Rigidbody
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = scale; // Heavier objects are bigger
            rb.useGravity = false;

            // Give initial random velocity
            rb.linearVelocity = Random.insideUnitSphere * 2f;
            rb.angularVelocity = Random.insideUnitSphere * 2f;

            // Add floating behavior
            FloatingObject floater = obj.AddComponent<FloatingObject>();
            floater.Initialize(floatStrength, rotationSpeed, noiseSpeed);

            // Add boundary keeper
            BoundaryKeeper keeper = obj.AddComponent<BoundaryKeeper>();
            keeper.Initialize(boundaryRadius, boundaryForce);

            // Apply random color material
            Renderer renderer = obj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color color = colors[Random.Range(0, colors.Length)];
            mat.SetColor("_BaseColor", color);

            // Add some emission for visual pop
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.3f);

            renderer.material = mat;

            obj.name = $"Floating_{shapeType}_{i}";
        }
    }

    PrimitiveType GetRandomShape()
    {
        PrimitiveType[] shapes = new PrimitiveType[]
        {
            PrimitiveType.Cube,
            PrimitiveType.Sphere,
            PrimitiveType.Capsule,
            PrimitiveType.Cylinder
        };
        return shapes[Random.Range(0, shapes.Length)];
    }
}
