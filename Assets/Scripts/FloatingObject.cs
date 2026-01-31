using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 randomOffset;
    private float floatStrength;
    private float rotationSpeed;
    private float noiseSpeed;

    public void Initialize(float strength, float rotation, float noise)
    {
        floatStrength = strength;
        rotationSpeed = rotation;
        noiseSpeed = noise;
        randomOffset = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(0f, 100f),
            Random.Range(0f, 100f)
        );
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float time = Time.time * noiseSpeed;

        // Use Perlin noise for smooth, organic floating movement
        Vector3 noiseForce = new Vector3(
            Mathf.PerlinNoise(time + randomOffset.x, randomOffset.y) - 0.5f,
            Mathf.PerlinNoise(time + randomOffset.y, randomOffset.z) - 0.5f,
            Mathf.PerlinNoise(time + randomOffset.z, randomOffset.x) - 0.5f
        ) * floatStrength;

        rb.AddForce(noiseForce, ForceMode.Acceleration);

        // Add random torque for tumbling rotation
        Vector3 torque = new Vector3(
            Mathf.PerlinNoise(time * 0.5f + randomOffset.z, randomOffset.x) - 0.5f,
            Mathf.PerlinNoise(time * 0.5f + randomOffset.x, randomOffset.y) - 0.5f,
            Mathf.PerlinNoise(time * 0.5f + randomOffset.y, randomOffset.z) - 0.5f
        ) * rotationSpeed;

        rb.AddTorque(torque, ForceMode.Acceleration);
    }
}
