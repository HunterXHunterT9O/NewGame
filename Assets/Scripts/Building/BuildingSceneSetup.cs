using UnityEngine;

public class BuildingSceneSetup : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0, 2f, -10f);

    [Header("Ground")]
    [SerializeField] private float groundSize = 200f;

    void Start()
    {
        SetupLighting();
        SetupGround();
        SetupPlayer();
        SetupBuildingSystem();

        // Disable any existing cameras
        Camera existingCam = Camera.main;
        if (existingCam != null)
        {
            existingCam.gameObject.SetActive(false);
        }
    }

    void SetupLighting()
    {
        // Main directional light
        GameObject lightObj = new GameObject("Sun");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);

        // Skybox color (simple)
        Camera.main?.GetComponent<Camera>();
    }

    void SetupGround()
    {
        // Main ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.25f, 0);
        ground.transform.localScale = new Vector3(groundSize, 0.5f, groundSize);
        ground.layer = LayerMask.NameToLayer("Default");
        ground.isStatic = true;

        // Material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.3f, 0.35f, 0.4f));
        renderer.material = mat;

        // Grid overlay plane (visual reference)
        GameObject gridPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridPlane.name = "GridOverlay";
        gridPlane.transform.position = new Vector3(0, 0.01f, 0);
        gridPlane.transform.rotation = Quaternion.Euler(90, 0, 0);
        gridPlane.transform.localScale = new Vector3(groundSize, groundSize, 1);
        Destroy(gridPlane.GetComponent<Collider>());

        // Simple grid material
        Renderer gridRenderer = gridPlane.GetComponent<Renderer>();
        Material gridMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        gridMat.SetColor("_BaseColor", new Color(0.4f, 0.45f, 0.5f, 0.3f));
        gridRenderer.material = gridMat;
    }

    void SetupPlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = playerSpawnPosition;
        player.AddComponent<FirstPersonController>();
        player.tag = "Player";
    }

    void SetupBuildingSystem()
    {
        GameObject buildingManager = new GameObject("BuildingManager");
        buildingManager.AddComponent<BuildingSystem>();
        buildingManager.AddComponent<BuildingUI_Toolkit>(); // Using new UI Toolkit
    }
}
