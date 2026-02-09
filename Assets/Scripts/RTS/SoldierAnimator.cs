using UnityEngine;
using UnityEngine.AI;

namespace EdgeOfUniverse.RTS
{
    /// <summary>
    /// Controls soldier animations based on NavMeshAgent movement.
    /// Automatically detects idle, walk, and run states.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class SoldierAnimator : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string isMovingParameterName = "IsMoving";

        [Header("Speed Thresholds")]
        [SerializeField] private float walkThreshold = 0.1f;
        [SerializeField] private float runThreshold = 3f;

        [Header("Smoothing")]
        [SerializeField] private float animationSmoothTime = 0.1f;

        private Animator animator;
        private NavMeshAgent agent;
        private float currentAnimSpeed;
        private float animSpeedVelocity;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (animator == null || agent == null || animator.runtimeAnimatorController == null)
                return;

            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            // Get agent velocity magnitude
            float speed = agent.velocity.magnitude;

            // Normalize speed (0 = idle, 0.5 = walk, 1.0 = run)
            float normalizedSpeed = Mathf.Clamp01(speed / agent.speed);

            // Smooth the speed value for cleaner transitions
            currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, normalizedSpeed, ref animSpeedVelocity, animationSmoothTime);

            // Update animator parameters
            if (HasParameter(speedParameterName))
            {
                animator.SetFloat(speedParameterName, currentAnimSpeed);
            }

            if (HasParameter(isMovingParameterName))
            {
                animator.SetBool(isMovingParameterName, currentAnimSpeed > walkThreshold);
            }

            // Fallback: Try common parameter names
            if (HasParameter("Horizontal"))
            {
                animator.SetFloat("Horizontal", 0f);
            }

            if (HasParameter("Vertical"))
            {
                animator.SetFloat("Vertical", currentAnimSpeed);
            }
        }

        private bool HasParameter(string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        #region Debug

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-detect animator if not set
            if (animator == null)
                animator = GetComponent<Animator>();

            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || agent == null) return;

            // Draw velocity vector
            Gizmos.color = Color.green;
            Vector3 velocityViz = agent.velocity;
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + velocityViz);
        }
#endif

        #endregion
    }
}
