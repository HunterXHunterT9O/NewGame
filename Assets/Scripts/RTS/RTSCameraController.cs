using UnityEngine;
using System;

/// <summary>
/// RTS-style camera controller with pan, zoom, and rotate.
/// Follows the existing project pattern of self-initialization and direct input polling.
/// </summary>
public class RTSCameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 20f;
    [SerializeField] private float edgePanThreshold = 20f;
    [SerializeField] private bool enableEdgePan = true;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoomHeight = 5f;
    [SerializeField] private float maxZoomHeight = 50f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 boundsMin = new Vector2(-100f, -100f);
    [SerializeField] private Vector2 boundsMax = new Vector2(100f, 100f);

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = ~0;

    // Camera rig structure:
    // CameraRig (this) -> Pivot (rotation) -> Camera (zoom)
    private Transform pivot;
    private Camera cam;

    private float targetZoomHeight;
    private float zoomVelocity;
    private bool isRotating;

    // Events
    public event Action<Vector3> OnCameraMove;
    public event Action<float> OnCameraZoom;

    // Public accessors
    public Camera Camera => cam;
    public Vector3 FocusPoint => GetGroundPoint();

    private void Awake()
    {
        SetupCameraRig();
    }

    private void Start()
    {
        targetZoomHeight = cam.transform.localPosition.y;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
        HandleRotation();
        ApplyBounds();
    }

    private void SetupCameraRig()
    {
        // Create pivot if it doesn't exist
        pivot = transform.Find("Pivot");
        if (pivot == null)
        {
            GameObject pivotObj = new GameObject("Pivot");
            pivotObj.transform.SetParent(transform);
            pivotObj.transform.localPosition = Vector3.zero;
            pivotObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            pivot = pivotObj.transform;
        }

        // Find or create camera
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("RTSCamera");
            camObj.transform.SetParent(pivot);
            camObj.transform.localPosition = new Vector3(0f, 20f, -20f);
            camObj.transform.localRotation = Quaternion.identity;
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        else
        {
            // Ensure camera is parented to pivot
            if (cam.transform.parent != pivot)
            {
                cam.transform.SetParent(pivot);
            }
        }
    }

    private void HandlePan()
    {
        Vector3 moveDir = Vector3.zero;

        // WASD input
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDir += GetForward();
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDir -= GetForward();
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDir += GetRight();
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDir -= GetRight();

        // Edge pan
        if (enableEdgePan && !isRotating)
        {
            Vector3 mousePos = Input.mousePosition;

            if (mousePos.x < edgePanThreshold)
                moveDir -= GetRight();
            else if (mousePos.x > Screen.width - edgePanThreshold)
                moveDir += GetRight();

            if (mousePos.y < edgePanThreshold)
                moveDir -= GetForward();
            else if (mousePos.y > Screen.height - edgePanThreshold)
                moveDir += GetForward();
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            moveDir.Normalize();
            // Scale speed with zoom level for consistent feel
            float zoomFactor = Mathf.Lerp(0.5f, 2f, (targetZoomHeight - minZoomHeight) / (maxZoomHeight - minZoomHeight));
            transform.position += moveDir * panSpeed * zoomFactor * Time.deltaTime;
            OnCameraMove?.Invoke(transform.position);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoomHeight -= scroll * zoomSpeed;
            targetZoomHeight = Mathf.Clamp(targetZoomHeight, minZoomHeight, maxZoomHeight);
            OnCameraZoom?.Invoke(targetZoomHeight);
        }

        // Smooth zoom
        Vector3 camLocalPos = cam.transform.localPosition;
        float currentHeight = camLocalPos.y;
        float newHeight = Mathf.SmoothDamp(currentHeight, targetZoomHeight, ref zoomVelocity, zoomSmoothTime);

        // Maintain angle by adjusting Z proportionally
        float ratio = newHeight / currentHeight;
        cam.transform.localPosition = new Vector3(camLocalPos.x, newHeight, camLocalPos.z * ratio);
    }

    private void HandleRotation()
    {
        // Middle mouse to rotate
        if (Input.GetMouseButtonDown(2))
        {
            isRotating = true;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            float rotateInput = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, rotateInput * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void ApplyBounds()
    {
        if (!useBounds) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
        transform.position = pos;
    }

    /// <summary>
    /// Get forward direction on the XZ plane based on camera rotation
    /// </summary>
    private Vector3 GetForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    /// <summary>
    /// Get right direction on the XZ plane based on camera rotation
    /// </summary>
    private Vector3 GetRight()
    {
        Vector3 right = transform.right;
        right.y = 0f;
        return right.normalized;
    }

    /// <summary>
    /// Raycast to find the point on the ground the camera is looking at
    /// </summary>
    public Vector3 GetGroundPoint()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            return hit.point;
        }
        // Fallback: project to Y=0 plane
        float t = -cam.transform.position.y / cam.transform.forward.y;
        return cam.transform.position + cam.transform.forward * t;
    }

    /// <summary>
    /// Smoothly move camera to focus on a world position
    /// </summary>
    public void FocusOn(Vector3 worldPosition, float duration = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(FocusCoroutine(worldPosition, duration));
    }

    private System.Collections.IEnumerator FocusCoroutine(Vector3 target, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(target.x, transform.position.y, target.z);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        OnCameraMove?.Invoke(transform.position);
    }

    /// <summary>
    /// Get a ray from the camera through a screen point
    /// </summary>
    public Ray ScreenPointToRay(Vector3 screenPoint)
    {
        return cam.ScreenPointToRay(screenPoint);
    }
}
