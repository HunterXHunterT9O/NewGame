using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxPlaceDistance = 20f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;

    [Header("Grid Snapping")]
    [SerializeField] private bool useGridSnap = true;
    [SerializeField] private float gridSize = 1f;

    // Current state
    private GameObject ghostObject;
    private BuildingPiece currentPiece;
    private float currentRotation = 0f;
    private bool canPlace = false;
    private bool isInBuildMode = false;

    // References
    private Camera playerCamera;
    private List<Renderer> ghostRenderers = new List<Renderer>();
    private List<Material[]> originalMaterials = new List<Material[]>();

    // Events
    public System.Action<bool> OnBuildModeChanged;
    public System.Action<BuildingPiece> OnPieceSelected;
    public System.Action OnPiecePlaced;

    public bool IsInBuildMode => isInBuildMode;
    public BuildingPiece CurrentPiece => currentPiece;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        CreateGhostMaterials();
    }

    void CreateGhostMaterials()
    {
        if (ghostMaterialValid == null)
        {
            ghostMaterialValid = CreateTransparentMaterial(new Color(0.2f, 1f, 0.4f, 0.5f));
        }

        if (ghostMaterialInvalid == null)
        {
            ghostMaterialInvalid = CreateTransparentMaterial(new Color(1f, 0.3f, 0.2f, 0.5f));
        }
    }

    Material CreateTransparentMaterial(Color color)
    {
        // Use URP Lit shader - must exist in URP projects
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            // Try Unlit as backup (also URP)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        // DO NOT fallback to "Standard" shader - it's BiRP and causes purple materials in URP

        if (shader == null)
        {
            Debug.LogError("URP shaders not found! Ensure URP is properly installed.");
            return null;
        }

        Material mat = new Material(shader);

        // Set base color with alpha (URP uses _BaseColor, not _Color)
        mat.SetColor("_BaseColor", color);

        // Configure for transparency
        mat.SetFloat("_Surface", 1f); // 1 = Transparent
        mat.SetFloat("_Blend", 0f);   // 0 = Alpha blend
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000; // Transparent queue

        // Enable transparency keyword
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }

    void Update()
    {
        if (!isInBuildMode) return;

        HandleInput();
        UpdateGhostPosition();
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // Rotate with R/T or Q/E
        if (keyboard.rKey.isPressed)
            currentRotation += rotationSpeed * Time.deltaTime;
        if (keyboard.tKey.isPressed)
            currentRotation -= rotationSpeed * Time.deltaTime;

        // Place with left click
        if (mouse.leftButton.wasPressedThisFrame && canPlace)
        {
            PlaceCurrentPiece();
        }

        // Cancel with right click or Escape
        if (mouse.rightButton.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
        {
            ExitBuildMode();
        }
    }

    void UpdateGhostPosition()
    {
        if (ghostObject == null) return;

        // Try to get camera if we don't have one
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<Camera>();

            if (playerCamera == null) return;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask))
        {
            Vector3 position = hit.point;

            // Grid snapping
            if (useGridSnap)
            {
                position.x = Mathf.Round(position.x / gridSize) * gridSize;
                position.y = Mathf.Round(position.y / gridSize) * gridSize;
                position.z = Mathf.Round(position.z / gridSize) * gridSize;
            }

            // Apply snap offset
            if (currentPiece != null)
            {
                position += currentPiece.snapOffset;
            }

            ghostObject.transform.position = position;
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            // Check if placement is valid (simple overlap check)
            canPlace = !CheckOverlap(position);
            UpdateGhostMaterial(canPlace);

            ghostObject.SetActive(true);
        }
        else
        {
            ghostObject.SetActive(false);
            canPlace = false;
        }
    }

    bool CheckOverlap(Vector3 position)
    {
        // Simple overlap sphere check
        Collider[] colliders = Physics.OverlapSphere(position, 0.3f);
        foreach (var col in colliders)
        {
            if (col.gameObject != ghostObject && !col.isTrigger)
            {
                // Ignore ground
                if (col.gameObject.name == "Ground") continue;
                return true;
            }
        }
        return false;
    }

    void UpdateGhostMaterial(bool valid)
    {
        Material mat = valid ? ghostMaterialValid : ghostMaterialInvalid;
        if (mat == null)
        {
            Debug.LogWarning("Ghost material is null!");
            return;
        }

        foreach (var renderer in ghostRenderers)
        {
            renderer.material = mat;
        }
    }

    public void SelectPiece(BuildingPiece piece)
    {
        if (piece == null)
        {
            Debug.LogError("SelectPiece: piece is null");
            return;
        }
        if (piece.prefab == null)
        {
            Debug.LogError($"SelectPiece: prefab is null for {piece.pieceName}");
            return;
        }

        Debug.Log($"SelectPiece: Creating ghost for {piece.pieceName}");

        currentPiece = piece;
        currentRotation = 0f;

        // Destroy old ghost
        if (ghostObject != null)
        {
            Destroy(ghostObject);
        }

        // Create new ghost from the actual prefab
        ghostObject = Instantiate(piece.prefab);
        ghostObject.name = "BuildingGhost";

        // Disable all colliders and rigidbodies on ghost
        foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        foreach (var rb in ghostObject.GetComponentsInChildren<Rigidbody>())
        {
            Destroy(rb);
        }

        // Cache renderers
        ghostRenderers.Clear();
        originalMaterials.Clear();
        ghostRenderers.AddRange(ghostObject.GetComponentsInChildren<Renderer>());

        // Apply ghost materials
        UpdateGhostMaterial(true);

        isInBuildMode = true;
        OnBuildModeChanged?.Invoke(true);
        OnPieceSelected?.Invoke(piece);
    }

    void PlaceCurrentPiece()
    {
        if (currentPiece == null || currentPiece.prefab == null) return;

        // Instantiate the actual piece
        GameObject placed = Instantiate(currentPiece.prefab);
        placed.transform.position = ghostObject.transform.position;
        placed.transform.rotation = ghostObject.transform.rotation;
        placed.name = currentPiece.pieceName;

        OnPiecePlaced?.Invoke();

        // Stay in build mode with same piece selected (Satisfactory style)
    }

    public void ExitBuildMode()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }

        currentPiece = null;
        isInBuildMode = false;
        ghostRenderers.Clear();

        OnBuildModeChanged?.Invoke(false);
    }

    public void ToggleBuildMode()
    {
        if (isInBuildMode)
        {
            ExitBuildMode();
        }
    }
}
