using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Handles unit movement using NavMesh pathfinding.
/// Works with UnitSelectionSystem for move commands.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SelectableUnit))]
public class UnitMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float formationSpacing = 2f;

    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";

    private NavMeshAgent agent;
    private SelectableUnit selectableUnit;
    private Vector3 targetPosition;
    private bool hasTarget;

    // Events
    public event Action OnStartedMoving;
    public event Action OnStoppedMoving;
    public event Action<Vector3> OnReachedDestination;

    // Public accessors
    public bool IsMoving => hasTarget && agent.remainingDistance > stoppingDistance;
    public Vector3 TargetPosition => targetPosition;
    public float CurrentSpeed => agent.velocity.magnitude;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selectableUnit = GetComponent<SelectableUnit>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        // Configure agent
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;

        // Note: Movement is handled directly by UnitSelectionSystem.IssueFormationMove()
        // The OnMoveCommand event is for external listeners (VFX, audio, etc.)
    }

    private void Update()
    {
        UpdateMovementState();
        UpdateAnimation();
    }

    private void UpdateMovementState()
    {
        if (!hasTarget) return;

        // Check if we've reached destination
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            hasTarget = false;
            OnReachedDestination?.Invoke(targetPosition);
            OnStoppedMoving?.Invoke();
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude / moveSpeed;

        if (animator.parameters.Length > 0)
        {
            // Try to set parameters if they exist
            try
            {
                animator.SetFloat(moveSpeedParam, speed);
                animator.SetBool(isMovingParam, IsMoving);
            }
            catch
            {
                // Parameters don't exist, ignore
            }
        }
    }

    /// <summary>
    /// Move to a world position
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        // Find valid NavMesh position
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            agent.SetDestination(targetPosition);
            hasTarget = true;

            OnStartedMoving?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[UnitMovement] Could not find valid NavMesh position near {destination}");
        }
    }

    /// <summary>
    /// Move to a position with formation offset
    /// </summary>
    public void MoveToFormation(Vector3 destination, int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
        {
            MoveTo(destination);
            return;
        }

        // Calculate formation offset (simple grid)
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
        int row = unitIndex / columns;
        int col = unitIndex % columns;

        // Center the formation
        float offsetX = (col - (columns - 1) / 2f) * formationSpacing;
        float offsetZ = (row - (totalUnits / columns - 1) / 2f) * formationSpacing;

        Vector3 formationPos = destination + new Vector3(offsetX, 0f, offsetZ);
        MoveTo(formationPos);
    }

    /// <summary>
    /// Stop movement immediately
    /// </summary>
    public void Stop()
    {
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        hasTarget = false;
        OnStoppedMoving?.Invoke();
    }

    /// <summary>
    /// Check if a position is reachable
    /// </summary>
    public bool CanReach(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !hasTarget) return;

        // Draw path
        Gizmos.color = Color.green;
        if (agent != null && agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Draw destination
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
}
