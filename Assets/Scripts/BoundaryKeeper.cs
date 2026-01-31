using UnityEngine;

public class BoundaryKeeper : MonoBehaviour
{
    private float boundaryRadius;
    private float pushForce;
    private Rigidbody rb;

    public void Initialize(float radius, float force)
    {
        boundaryRadius = radius;
        pushForce = force;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float distanceFromCenter = transform.position.magnitude;

        // If object is getting too far from center, push it back
        if (distanceFromCenter > boundaryRadius * 0.8f)
        {
            // Calculate how far past the soft boundary we are
            float overDistance = distanceFromCenter - (boundaryRadius * 0.8f);
            float maxOverDistance = boundaryRadius * 0.2f;
            float strength = Mathf.Clamp01(overDistance / maxOverDistance);

            // Push toward center with increasing force
            Vector3 directionToCenter = -transform.position.normalized;
            rb.AddForce(directionToCenter * pushForce * strength, ForceMode.Acceleration);
        }
    }
}
