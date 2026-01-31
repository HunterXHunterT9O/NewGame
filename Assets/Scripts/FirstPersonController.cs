using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float floatSpeed = 30f;

    [Header("Hoverpack")]
    [SerializeField] private float hoverForce = 35f;
    [SerializeField] private float gravityForce = 20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Physics")]
    [SerializeField] private float groundedDrag = 8f;
    [SerializeField] private float floatingDrag = 3f;
    [SerializeField] private float landingSpeed = 15f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Camera playerCamera;
    private Rigidbody rb;
    private CapsuleCollider col;
    private float pitch = 0f;
    private bool cursorLocked = true;

    // Input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalMove;
    private bool isSprinting;
    private bool isHovering;

    // State
    public enum PlayerState { Grounded, Floating, Landing }
    public PlayerState CurrentState { get; private set; } = PlayerState.Grounded;
    public bool IsGrounded { get; private set; }
    public float CurrentSpeed { get; private set; }
    public float Altitude { get; private set; }

    // Double-tap detection for C key
    private float lastCPressTime = -1f;
    private float doubleTapThreshold = 0.3f;

    // Events for UI
    public System.Action<PlayerState> OnStateChanged;

    void Start()
    {
        // Setup camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            camObj.transform.localRotation = Quaternion.identity;
            playerCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        // Setup rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Setup collider
        col = GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = Vector3.zero;
        }

        SetState(PlayerState.Grounded);
        LockCursor();
    }

    void Update()
    {
        HandleInput();
        HandleCursorLock();
        CheckGrounded();
        UpdateAltitude();

        if (cursorLocked)
        {
            HandleLook();
        }

        CurrentSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleStatePhysics();
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // WASD movement
        moveInput = Vector2.zero;
        if (keyboard.wKey.isPressed) moveInput.y += 1f;
        if (keyboard.sKey.isPressed) moveInput.y -= 1f;
        if (keyboard.dKey.isPressed) moveInput.x += 1f;
        if (keyboard.aKey.isPressed) moveInput.x -= 1f;


        // Sprint
        isSprinting = keyboard.leftShiftKey.isPressed;

        // Mouse look
        if (cursorLocked)
        {
            lookInput = mouse.delta.ReadValue();
        }

        // Space - Hoverpack (works in all states)
        isHovering = keyboard.spaceKey.isPressed;

        // Vertical movement when floating (E/Q or Ctrl for down)
        verticalMove = 0f;
        if (CurrentState == PlayerState.Floating)
        {
            if (keyboard.eKey.isPressed)
                verticalMove = 1f;
            if (keyboard.leftCtrlKey.isPressed || keyboard.qKey.isPressed)
                verticalMove = -1f;
        }

        // Double C to land
        if (keyboard.cKey.wasPressedThisFrame)
        {
            float timeSinceLastC = Time.time - lastCPressTime;
            if (timeSinceLastC <= doubleTapThreshold && CurrentState == PlayerState.Floating)
            {
                StartLanding();
            }
            lastCPressTime = Time.time;
        }
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void HandleMovement()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (CurrentState == PlayerState.Floating)
        {
            moveDirection += Vector3.up * verticalMove;
        }

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        float currentSpeed = moveSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        if (CurrentState == PlayerState.Floating)
            currentSpeed = floatSpeed;

        rb.AddForce(moveDirection * currentSpeed, ForceMode.Acceleration);
    }

    void HandleStatePhysics()
    {
        rb.useGravity = false;

        // Apply hoverpack thrust or gravity
        if (isHovering)
        {
            rb.AddForce(Vector3.up * hoverForce, ForceMode.Acceleration);
        }
        else if (!IsGrounded)
        {
            rb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
        }

        switch (CurrentState)
        {
            case PlayerState.Grounded:
                rb.linearDamping = groundedDrag;

                // Switch to floating if we hover high enough
                if (!IsGrounded && Altitude > 2f)
                {
                    SetState(PlayerState.Floating);
                }
                break;

            case PlayerState.Floating:
                rb.linearDamping = floatingDrag;

                // Auto-land if we touch ground
                if (IsGrounded && !isHovering)
                {
                    SetState(PlayerState.Grounded);
                }
                break;

            case PlayerState.Landing:
                rb.AddForce(Vector3.down * landingSpeed, ForceMode.Acceleration);
                rb.linearDamping = groundedDrag;

                if (IsGrounded)
                {
                    SetState(PlayerState.Grounded);
                }
                break;
        }
    }

    void StartLanding()
    {
        SetState(PlayerState.Landing);
    }

    void SetState(PlayerState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }

    void CheckGrounded()
    {
        float checkStart = col.height / 2f - col.radius + 0.05f;
        Vector3 origin = transform.position + Vector3.down * checkStart;

        IsGrounded = Physics.SphereCast(
            origin,
            col.radius * 0.9f,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    void UpdateAltitude()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 500f, groundLayer))
        {
            Altitude = hit.distance;
        }
        else
        {
            Altitude = 500f;
        }
    }

    void HandleCursorLock()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (!cursorLocked && mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    void LockCursor()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = true; // Still track for mouse look
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }
}
