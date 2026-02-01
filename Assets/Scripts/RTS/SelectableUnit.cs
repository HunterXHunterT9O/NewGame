using UnityEngine;
using System;
using EdgeOfUniverse.VFX;

/// <summary>
/// Component that marks a unit as selectable and handles selection visuals.
/// Attach to any unit that should be selectable by the player.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SelectableUnit : MonoBehaviour
{
    [Header("Selection Visuals")]
    [SerializeField] private bool useAdvancedIndicator = true;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Color selectionColor = new Color(0.31f, 0.71f, 0.78f, 1f); // Cyan to match new system
    [SerializeField] private float indicatorScale = 1.5f;

    // Advanced indicator reference
    private SelectionIndicator advancedIndicator;

    [Header("Unit Info")]
    [SerializeField] private string unitName = "Unit";
    [SerializeField] private Sprite unitIcon;
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite classIcon;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    private bool isSelected;
    private Renderer[] renderers;
    private Color[] originalColors;

    // Events
    public event Action<SelectableUnit> OnSelected;
    public event Action<SelectableUnit> OnDeselected;
    public event Action<SelectableUnit, float> OnHealthChanged;
    public event Action<SelectableUnit> OnDeath;

    // Public accessors
    public bool IsSelected => isSelected;
    public string UnitName => unitName;
    public Sprite UnitIcon => unitIcon;
    public Sprite Portrait => portrait;
    public Sprite ClassIcon => classIcon;
    public Vector3 Position => transform.position;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthRatio => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        CacheRenderers();
        CreateSelectionIndicator();
    }

    private void Start()
    {
        SetSelected(false);
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = renderers[i].material.color;
            }
            else if (renderers[i].material.HasProperty("_BaseColor"))
            {
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            }
        }
    }

    private void CreateSelectionIndicator()
    {
        if (selectionIndicator != null) return;

        // Calculate scale based on unit bounds
        Bounds bounds = GetUnitBounds();
        float diameter = Mathf.Max(bounds.size.x, bounds.size.z) * indicatorScale;

        if (useAdvancedIndicator)
        {
            // Create advanced multi-layer tactical indicator
            selectionIndicator = new GameObject("SelectionIndicator");
            selectionIndicator.transform.SetParent(transform);
            selectionIndicator.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            advancedIndicator = selectionIndicator.AddComponent<SelectionIndicator>();
            advancedIndicator.SetScale(diameter * 0.5f);

            selectionIndicator.SetActive(true); // Container active, indicator controls visibility
        }
        else
        {
            // Fallback: Create simple ring/circle indicator below the unit
            selectionIndicator = new GameObject("SelectionIndicator");
            selectionIndicator.transform.SetParent(transform);
            selectionIndicator.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            selectionIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Create mesh for ring
            MeshFilter meshFilter = selectionIndicator.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = selectionIndicator.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateRingMesh(diameter * 0.5f, diameter * 0.4f, 32);

            // Create unlit material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = selectionColor;
            meshRenderer.material = mat;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            selectionIndicator.SetActive(false);
        }
    }

    private Mesh CreateRingMesh(float outerRadius, float innerRadius, int segments)
    {
        Mesh mesh = new Mesh();

        int vertexCount = segments * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 6];

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
            vertices[i * 2 + 1] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);

            int nextI = (i + 1) % segments;
            int triIndex = i * 6;

            triangles[triIndex] = i * 2;
            triangles[triIndex + 1] = nextI * 2;
            triangles[triIndex + 2] = i * 2 + 1;

            triangles[triIndex + 3] = nextI * 2;
            triangles[triIndex + 4] = nextI * 2 + 1;
            triangles[triIndex + 5] = i * 2 + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Bounds GetUnitBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool first = true;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            if (first)
            {
                bounds = r.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return bounds;
    }

    /// <summary>
    /// Set selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        // Show/hide indicator
        if (useAdvancedIndicator && advancedIndicator != null)
        {
            if (selected)
                advancedIndicator.Show();
            else
                advancedIndicator.Hide();
        }
        else if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }

        // Fire events
        if (selected)
        {
            OnSelected?.Invoke(this);
        }
        else
        {
            OnDeselected?.Invoke(this);
        }
    }

    /// <summary>
    /// Apply damage to this unit
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(this, currentHealth);

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke(this);
        }
    }

    /// <summary>
    /// Heal this unit
    /// </summary>
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(this, currentHealth);
    }

    /// <summary>
    /// Set health directly (for initialization)
    /// </summary>
    public void SetHealth(float health, float max = -1)
    {
        if (max > 0) maxHealth = max;
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(this, currentHealth);
    }

    /// <summary>
    /// Get the center point of this unit (accounting for bounds)
    /// </summary>
    public Vector3 GetCenter()
    {
        return GetUnitBounds().center;
    }

    /// <summary>
    /// Check if a screen point is within this unit's screen bounds
    /// </summary>
    public bool ContainsScreenPoint(Camera camera, Vector2 screenPoint)
    {
        Bounds bounds = GetUnitBounds();

        // Check all 8 corners of the bounds
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = bounds.max;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (var corner in corners)
        {
            Vector3 screenPos = camera.WorldToScreenPoint(corner);
            if (screenPos.z < 0) continue;

            min.x = Mathf.Min(min.x, screenPos.x);
            min.y = Mathf.Min(min.y, screenPos.y);
            max.x = Mathf.Max(max.x, screenPos.x);
            max.y = Mathf.Max(max.y, screenPos.y);
        }

        return screenPoint.x >= min.x && screenPoint.x <= max.x &&
               screenPoint.y >= min.y && screenPoint.y <= max.y;
    }

    private void OnDestroy()
    {
        // Clean up dynamic materials
        if (selectionIndicator != null)
        {
            var renderer = selectionIndicator.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                Destroy(renderer.material);
            }
        }
    }
}
