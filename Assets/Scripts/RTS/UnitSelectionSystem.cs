using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles unit selection via click, shift-click, and box selection.
/// Works with RTSCameraController for raycasting.
/// </summary>
public class UnitSelectionSystem : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private LayerMask unitLayer = ~0;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float clickThreshold = 5f; // Pixels - below this is a click, above is drag

    [Header("Box Selection")]
    [SerializeField] private Color boxColor = new Color(0.2f, 0.8f, 0.2f, 0.25f);
    [SerializeField] private Color boxBorderColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [Header("Formation")]
    [SerializeField] private float formationSpacing = 2f;

    private RTSCameraController cameraController;
    private Camera cam;

    private List<SelectableUnit> allUnits = new List<SelectableUnit>();
    private List<SelectableUnit> selectedUnits = new List<SelectableUnit>();

    // Box selection state
    private bool isBoxSelecting;
    private Vector2 boxStartPos;
    private Vector2 boxEndPos;

    // Materials for box rendering
    private Material boxMaterial;
    private Material borderMaterial;

    // Events
    public event Action<List<SelectableUnit>> OnSelectionChanged;
    public event Action<Vector3> OnMoveCommand;

    // Public accessors
    public IReadOnlyList<SelectableUnit> SelectedUnits => selectedUnits;
    public int SelectedCount => selectedUnits.Count;
    public bool HasSelection => selectedUnits.Count > 0;

    private void Awake()
    {
        CreateBoxMaterials();
    }

    private void Start()
    {
        // Find camera controller
        cameraController = FindAnyObjectByType<RTSCameraController>();
        if (cameraController != null)
        {
            cam = cameraController.Camera;
        }
        else
        {
            cam = Camera.main;
        }

        // Find all existing selectable units
        RefreshUnitList();
    }

    private void Update()
    {
        if (cam == null) return;

        HandleLeftClick();
        HandleRightClick();
        HandleHotkeys();
    }

    private void OnGUI()
    {
        if (isBoxSelecting)
        {
            DrawSelectionBox();
        }
    }

    private void CreateBoxMaterials()
    {
        // Create materials for selection box rendering
        boxMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        boxMaterial.hideFlags = HideFlags.HideAndDontSave;
        boxMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        boxMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        boxMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        boxMaterial.SetInt("_ZWrite", 0);

        borderMaterial = new Material(boxMaterial);
    }

    private void HandleLeftClick()
    {
        // Start selection on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            boxStartPos = Input.mousePosition;
            boxEndPos = boxStartPos;
            isBoxSelecting = false;
        }

        // Update box while held
        if (Input.GetMouseButton(0))
        {
            boxEndPos = Input.mousePosition;

            // Check if we've dragged far enough to start box selection
            if (!isBoxSelecting && Vector2.Distance(boxStartPos, boxEndPos) > clickThreshold)
            {
                isBoxSelecting = true;
            }
        }

        // Complete selection on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            if (isBoxSelecting)
            {
                CompleteBoxSelection();
            }
            else
            {
                CompleteClickSelection();
            }

            isBoxSelecting = false;
        }
    }

    private void HandleRightClick()
    {
        if (Input.GetMouseButtonDown(1) && HasSelection)
        {
            // Raycast to ground for move command
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
            {
                IssueFormationMove(hit.point);
            }
        }
    }

    private void IssueFormationMove(Vector3 destination)
    {
        int unitCount = selectedUnits.Count;

        if (unitCount == 1)
        {
            // Single unit - just move directly
            OnMoveCommand?.Invoke(destination);
            return;
        }

        // Multiple units - calculate formation positions
        int columns = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        for (int i = 0; i < unitCount; i++)
        {
            var unit = selectedUnits[i];
            var movement = unit.GetComponent<UnitMovement>();

            if (movement != null)
            {
                int row = i / columns;
                int col = i % columns;

                // Center the formation
                float offsetX = (col - (columns - 1) / 2f) * formationSpacing;
                float offsetZ = (row - (unitCount / columns - 1) / 2f) * formationSpacing;

                Vector3 formationPos = destination + new Vector3(offsetX, 0f, offsetZ);
                movement.MoveTo(formationPos);
            }
        }
    }

    private void HandleHotkeys()
    {
        // Space - focus on selection
        if (Input.GetKeyDown(KeyCode.Space) && HasSelection)
        {
            FocusOnSelection();
        }

        // Ctrl+A - select all
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            && Input.GetKeyDown(KeyCode.A))
        {
            SelectAll();
        }
    }

    private void CompleteClickSelection()
    {
        bool addToSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Raycast for unit
        Ray ray = cam.ScreenPointToRay(boxStartPos);
        SelectableUnit clickedUnit = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, unitLayer))
        {
            clickedUnit = hit.collider.GetComponentInParent<SelectableUnit>();
        }

        if (clickedUnit != null)
        {
            if (addToSelection)
            {
                // Toggle selection
                if (clickedUnit.IsSelected)
                {
                    DeselectUnit(clickedUnit);
                }
                else
                {
                    SelectUnit(clickedUnit);
                }
            }
            else
            {
                // Replace selection
                ClearSelection();
                SelectUnit(clickedUnit);
            }
        }
        else if (!addToSelection)
        {
            // Clicked empty space - clear selection
            ClearSelection();
        }
    }

    private void CompleteBoxSelection()
    {
        bool addToSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!addToSelection)
        {
            ClearSelection();
        }

        // Get box bounds in screen space
        Rect selectionRect = GetSelectionRect();

        // Check each unit
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(unit.Position);

            // Skip if behind camera
            if (screenPos.z < 0) continue;

            // Check if unit is within box
            if (selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                if (!unit.IsSelected)
                {
                    SelectUnit(unit);
                }
            }
        }
    }

    private Rect GetSelectionRect()
    {
        float x = Mathf.Min(boxStartPos.x, boxEndPos.x);
        float y = Mathf.Min(boxStartPos.y, boxEndPos.y);
        float width = Mathf.Abs(boxEndPos.x - boxStartPos.x);
        float height = Mathf.Abs(boxEndPos.y - boxStartPos.y);

        return new Rect(x, y, width, height);
    }

    private void DrawSelectionBox()
    {
        // Convert to GUI coordinates (Y is flipped)
        Rect rect = GetSelectionRect();
        rect.y = Screen.height - rect.y - rect.height;

        // Draw filled box
        GUI.color = boxColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Draw border
        GUI.color = boxBorderColor;
        float borderWidth = 2f;

        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), Texture2D.whiteTexture);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), Texture2D.whiteTexture);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), Texture2D.whiteTexture);
        // Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    /// <summary>
    /// Select a unit (add to selection)
    /// </summary>
    public void SelectUnit(SelectableUnit unit)
    {
        if (unit == null || unit.IsSelected) return;

        unit.SetSelected(true);
        selectedUnits.Add(unit);
        OnSelectionChanged?.Invoke(selectedUnits);
    }

    /// <summary>
    /// Deselect a unit (remove from selection)
    /// </summary>
    public void DeselectUnit(SelectableUnit unit)
    {
        if (unit == null || !unit.IsSelected) return;

        unit.SetSelected(false);
        selectedUnits.Remove(unit);
        OnSelectionChanged?.Invoke(selectedUnits);
    }

    /// <summary>
    /// Clear all selection
    /// </summary>
    public void ClearSelection()
    {
        foreach (var unit in selectedUnits)
        {
            if (unit != null)
            {
                unit.SetSelected(false);
            }
        }

        selectedUnits.Clear();
        OnSelectionChanged?.Invoke(selectedUnits);
    }

    /// <summary>
    /// Select all units
    /// </summary>
    public void SelectAll()
    {
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsSelected)
            {
                unit.SetSelected(true);
                selectedUnits.Add(unit);
            }
        }

        OnSelectionChanged?.Invoke(selectedUnits);
    }

    /// <summary>
    /// Refresh the list of all selectable units in the scene
    /// </summary>
    public void RefreshUnitList()
    {
        allUnits.Clear();
        allUnits.AddRange(FindObjectsByType<SelectableUnit>(FindObjectsSortMode.None));
    }

    /// <summary>
    /// Register a new unit (call when spawning units at runtime)
    /// </summary>
    public void RegisterUnit(SelectableUnit unit)
    {
        if (unit != null && !allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    /// <summary>
    /// Unregister a unit (call when unit is destroyed)
    /// </summary>
    public void UnregisterUnit(SelectableUnit unit)
    {
        if (unit != null)
        {
            allUnits.Remove(unit);
            if (selectedUnits.Contains(unit))
            {
                selectedUnits.Remove(unit);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
        }
    }

    /// <summary>
    /// Focus camera on current selection
    /// </summary>
    public void FocusOnSelection()
    {
        if (!HasSelection || cameraController == null) return;

        // Get center of all selected units
        Vector3 center = Vector3.zero;
        foreach (var unit in selectedUnits)
        {
            center += unit.Position;
        }
        center /= selectedUnits.Count;

        cameraController.FocusOn(center);
    }

    private void OnDestroy()
    {
        if (boxMaterial != null) Destroy(boxMaterial);
        if (borderMaterial != null) Destroy(borderMaterial);
    }
}
