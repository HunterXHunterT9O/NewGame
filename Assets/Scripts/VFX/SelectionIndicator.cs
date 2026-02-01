using UnityEngine;
using System.Collections;

namespace EdgeOfUniverse.VFX
{
    /// <summary>
    /// Investor-grade multi-layer tactical selection indicator.
    /// Replaces the basic ring with a sophisticated holographic display.
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color primaryColor = new Color(0.31f, 0.71f, 0.78f, 0.6f);      // #50B4C8
        [SerializeField] private Color brightColor = new Color(0.5f, 0.89f, 0.97f, 0.8f);        // #80E4F8
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Outer Ring")]
        [SerializeField] private float outerRingRadius = 1.4f;
        [SerializeField] private float outerRingWidth = 0.08f;
        [SerializeField] private float outerRotationSpeed = 15f;
        [SerializeField] private float pulseInterval = 2f;
        [SerializeField] private float pulseScale = 1.1f;

        [Header("Inner Dashes")]
        [SerializeField] private int dashCount = 8;
        [SerializeField] private float innerRotationSpeed = -5f;
        [SerializeField] private float dashFlickerSpeed = 3f;

        [Header("Corner Brackets")]
        [SerializeField] private float bracketSize = 0.3f;
        [SerializeField] private float bracketBreathingSpeed = 1f;
        [SerializeField] private float bracketBreathingAmount = 0.02f;

        [Header("Data Particles")]
        [SerializeField] private int particleCount = 12;
        [SerializeField] private float particleRiseSpeed = 0.5f;
        [SerializeField] private float particleLifetime = 0.5f;

        [Header("Burst Settings")]
        [SerializeField] private float burstDuration = 0.3f;
        [SerializeField] private float burstExpandScale = 1.2f;
        [SerializeField] private int burstSparkCount = 10;

        // Components
        private GameObject outerRing;
        private GameObject innerDashes;
        private GameObject[] brackets = new GameObject[4];
        private ParticleSystem dataParticles;
        private ParticleSystem burstParticles;

        // State
        private float pulseTimer;
        private float currentScale = 1f;
        private bool isVisible;
        private Material outerRingMaterial;
        private Material innerDashMaterial;
        private Material bracketMaterial;

        // Cached
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            CreateIndicatorComponents();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isVisible) return;

            UpdateRotations();
            UpdatePulse();
            UpdateBracketBreathing();
            UpdateDashFlicker();
        }

        #region Component Creation

        private void CreateIndicatorComponents()
        {
            CreateOuterRing();
            CreateInnerDashes();
            CreateCornerBrackets();
            CreateDataParticles();
            CreateBurstParticles();
        }

        private void CreateOuterRing()
        {
            outerRing = new GameObject("OuterRing");
            outerRing.transform.SetParent(cachedTransform);
            outerRing.transform.localPosition = Vector3.zero;
            outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            MeshFilter mf = outerRing.AddComponent<MeshFilter>();
            MeshRenderer mr = outerRing.AddComponent<MeshRenderer>();

            mf.mesh = CreateRingMesh(outerRingRadius, outerRingRadius - outerRingWidth, 64);

            outerRingMaterial = CreateHolographicMaterial(primaryColor);
            mr.material = outerRingMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void CreateInnerDashes()
        {
            innerDashes = new GameObject("InnerDashes");
            innerDashes.transform.SetParent(cachedTransform);
            innerDashes.transform.localPosition = Vector3.zero;
            innerDashes.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            MeshFilter mf = innerDashes.AddComponent<MeshFilter>();
            MeshRenderer mr = innerDashes.AddComponent<MeshRenderer>();

            mf.mesh = CreateDashedRingMesh(outerRingRadius - outerRingWidth - 0.05f,
                                           outerRingRadius - outerRingWidth - 0.12f,
                                           dashCount, 0.6f);

            innerDashMaterial = CreateHolographicMaterial(brightColor);
            mr.material = innerDashMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void CreateCornerBrackets()
        {
            bracketMaterial = CreateHolographicMaterial(primaryColor);

            float bracketRadius = outerRingRadius * 0.9f;
            Vector3[] positions = new Vector3[]
            {
                new Vector3(bracketRadius, 0, bracketRadius),
                new Vector3(-bracketRadius, 0, bracketRadius),
                new Vector3(-bracketRadius, 0, -bracketRadius),
                new Vector3(bracketRadius, 0, -bracketRadius)
            };

            float[] rotations = new float[] { 0f, 90f, 180f, 270f };

            for (int i = 0; i < 4; i++)
            {
                brackets[i] = CreateBracket(positions[i], rotations[i]);
            }
        }

        private GameObject CreateBracket(Vector3 position, float rotationY)
        {
            GameObject bracket = new GameObject("Bracket");
            bracket.transform.SetParent(cachedTransform);
            bracket.transform.localPosition = position;
            bracket.transform.localRotation = Quaternion.Euler(90f, rotationY, 0f);

            MeshFilter mf = bracket.AddComponent<MeshFilter>();
            MeshRenderer mr = bracket.AddComponent<MeshRenderer>();

            mf.mesh = CreateBracketMesh(bracketSize);
            mr.material = bracketMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            return bracket;
        }

        private void CreateDataParticles()
        {
            GameObject particleObj = new GameObject("DataParticles");
            particleObj.transform.SetParent(cachedTransform);
            particleObj.transform.localPosition = Vector3.zero;

            dataParticles = particleObj.AddComponent<ParticleSystem>();
            var main = dataParticles.main;
            main.startLifetime = particleLifetime;
            main.startSpeed = particleRiseSpeed;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 1f, 1f, 0.3f);
            main.maxParticles = particleCount * 2;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = dataParticles.emission;
            emission.rateOverTime = particleCount;

            var shape = dataParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = outerRingRadius * 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLife = dataParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var sizeOverLife = dataParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(highlightColor);
        }

        private void CreateBurstParticles()
        {
            GameObject burstObj = new GameObject("BurstParticles");
            burstObj.transform.SetParent(cachedTransform);
            burstObj.transform.localPosition = Vector3.zero;

            burstParticles = burstObj.AddComponent<ParticleSystem>();
            var main = burstParticles.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSize = 0.15f;
            main.startColor = brightColor;
            main.maxParticles = burstSparkCount * 2;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;

            var emission = burstParticles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstSparkCount) });

            var shape = burstParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 60f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLife = burstParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(brightColor, 0f), new GradientColorKey(primaryColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var trails = burstParticles.trails;
            trails.enabled = true;
            trails.lifetime = 0.15f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = burstObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(brightColor);
            renderer.trailMaterial = CreateAdditiveMaterial(primaryColor);
        }

        #endregion

        #region Mesh Generation

        private Mesh CreateRingMesh(float outerRadius, float innerRadius, int segments)
        {
            Mesh mesh = new Mesh();
            int vertexCount = segments * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);

                uvs[i * 2] = new Vector2((float)i / segments, 1f);
                uvs[i * 2 + 1] = new Vector2((float)i / segments, 0f);

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
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateDashedRingMesh(float outerRadius, float innerRadius, int dashCount, float dashFill)
        {
            Mesh mesh = new Mesh();
            int segmentsPerDash = 8;
            int totalVertices = dashCount * segmentsPerDash * 2;
            int totalTriangles = dashCount * segmentsPerDash * 6;

            Vector3[] vertices = new Vector3[totalVertices];
            int[] triangles = new int[totalTriangles];

            float dashAngle = 360f / dashCount;
            float fillAngle = dashAngle * dashFill;

            int vertIndex = 0;
            int triIndex = 0;

            for (int d = 0; d < dashCount; d++)
            {
                float dashStart = d * dashAngle;
                float angleStep = fillAngle / segmentsPerDash;

                for (int s = 0; s < segmentsPerDash; s++)
                {
                    float angle1 = (dashStart + s * angleStep) * Mathf.Deg2Rad;
                    float angle2 = (dashStart + (s + 1) * angleStep) * Mathf.Deg2Rad;

                    vertices[vertIndex] = new Vector3(Mathf.Cos(angle1) * outerRadius, Mathf.Sin(angle1) * outerRadius, 0);
                    vertices[vertIndex + 1] = new Vector3(Mathf.Cos(angle1) * innerRadius, Mathf.Sin(angle1) * innerRadius, 0);
                    vertices[vertIndex + 2] = new Vector3(Mathf.Cos(angle2) * outerRadius, Mathf.Sin(angle2) * outerRadius, 0);
                    vertices[vertIndex + 3] = new Vector3(Mathf.Cos(angle2) * innerRadius, Mathf.Sin(angle2) * innerRadius, 0);

                    triangles[triIndex] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + 2;
                    triangles[triIndex + 2] = vertIndex + 1;
                    triangles[triIndex + 3] = vertIndex + 2;
                    triangles[triIndex + 4] = vertIndex + 3;
                    triangles[triIndex + 5] = vertIndex + 1;

                    vertIndex += 4;
                    triIndex += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateBracketMesh(float size)
        {
            Mesh mesh = new Mesh();
            float thickness = size * 0.15f;

            // L-shaped bracket
            Vector3[] vertices = new Vector3[]
            {
                // Horizontal part
                new Vector3(0, 0, 0),
                new Vector3(size, 0, 0),
                new Vector3(size, thickness, 0),
                new Vector3(0, thickness, 0),
                // Vertical part
                new Vector3(0, 0, 0),
                new Vector3(thickness, 0, 0),
                new Vector3(thickness, size, 0),
                new Vector3(0, size, 0),
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,  // Horizontal
                4, 6, 5, 4, 7, 6   // Vertical
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        #endregion

        #region Material Creation

        private Material CreateHolographicMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            return mat;
        }

        private Material CreateParticleMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            return mat;
        }

        private Material CreateAdditiveMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 1); // Additive
            return mat;
        }

        #endregion

        #region Animation Updates

        private void UpdateRotations()
        {
            if (outerRing != null)
            {
                outerRing.transform.Rotate(Vector3.forward, outerRotationSpeed * Time.deltaTime, Space.Self);
            }

            if (innerDashes != null)
            {
                innerDashes.transform.Rotate(Vector3.forward, innerRotationSpeed * Time.deltaTime, Space.Self);
            }
        }

        private void UpdatePulse()
        {
            pulseTimer += Time.deltaTime;

            if (pulseTimer >= pulseInterval)
            {
                pulseTimer = 0f;
                StartCoroutine(PulseAnimation());
            }
        }

        private IEnumerator PulseAnimation()
        {
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease out back for satisfying pulse
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, pulseScale, t * 2f)
                    : Mathf.Lerp(pulseScale, 1f, (t - 0.5f) * 2f);

                currentScale = scale;
                ApplyScale();

                yield return null;
            }

            currentScale = 1f;
            ApplyScale();
        }

        private void ApplyScale()
        {
            if (outerRing != null)
                outerRing.transform.localScale = Vector3.one * currentScale;
            if (innerDashes != null)
                innerDashes.transform.localScale = Vector3.one * currentScale;
        }

        private void UpdateBracketBreathing()
        {
            float breathe = 1f + Mathf.Sin(Time.time * bracketBreathingSpeed) * bracketBreathingAmount;

            foreach (var bracket in brackets)
            {
                if (bracket != null)
                {
                    bracket.transform.localScale = Vector3.one * breathe;
                }
            }
        }

        private void UpdateDashFlicker()
        {
            if (innerDashMaterial != null)
            {
                float flicker = 0.7f + Mathf.PerlinNoise(Time.time * dashFlickerSpeed, 0f) * 0.3f;
                Color flickerColor = brightColor;
                flickerColor.a *= flicker;
                innerDashMaterial.SetColor("_BaseColor", flickerColor);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show the selection indicator with burst animation
        /// </summary>
        public void Show()
        {
            if (isVisible) return;

            isVisible = true;
            SetComponentsActive(true);
            StartCoroutine(SelectionBurstSequence());
        }

        /// <summary>
        /// Hide the selection indicator
        /// </summary>
        public void Hide()
        {
            if (!isVisible) return;

            isVisible = false;
            StopAllCoroutines();
            SetComponentsActive(false);
        }

        /// <summary>
        /// Set scale based on unit bounds
        /// </summary>
        public void SetScale(float scale)
        {
            cachedTransform.localScale = Vector3.one * scale;
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            SetComponentsActive(visible);
        }

        private void SetComponentsActive(bool active)
        {
            if (outerRing != null) outerRing.SetActive(active);
            if (innerDashes != null) innerDashes.SetActive(active);
            foreach (var bracket in brackets)
            {
                if (bracket != null) bracket.SetActive(active);
            }
            if (dataParticles != null)
            {
                if (active) dataParticles.Play();
                else dataParticles.Stop();
            }
        }

        private IEnumerator SelectionBurstSequence()
        {
            // 0.00s - Ring expands with fade-in
            float expandDuration = 0.15f;
            float elapsed = 0f;

            // Start small
            currentScale = 0.5f;
            ApplyScale();

            // Fade in materials
            SetMaterialAlpha(0f);

            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / expandDuration;

                // Ease out for snappy expand
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                currentScale = Mathf.Lerp(0.5f, burstExpandScale, easeT);
                ApplyScale();
                SetMaterialAlpha(easeT);

                yield return null;
            }

            // 0.10s - Burst sparks
            if (burstParticles != null)
            {
                burstParticles.Play();
            }

            // 0.15s - Settle to normal
            elapsed = 0f;
            float settleDuration = 0.15f;

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / settleDuration;

                currentScale = Mathf.Lerp(burstExpandScale, 1f, t);
                ApplyScale();

                yield return null;
            }

            currentScale = 1f;
            ApplyScale();
            SetMaterialAlpha(1f);
        }

        private void SetMaterialAlpha(float alpha)
        {
            if (outerRingMaterial != null)
            {
                Color c = outerRingMaterial.GetColor("_BaseColor");
                c.a = primaryColor.a * alpha;
                outerRingMaterial.SetColor("_BaseColor", c);
            }

            if (innerDashMaterial != null)
            {
                Color c = innerDashMaterial.GetColor("_BaseColor");
                c.a = brightColor.a * alpha;
                innerDashMaterial.SetColor("_BaseColor", c);
            }

            if (bracketMaterial != null)
            {
                Color c = bracketMaterial.GetColor("_BaseColor");
                c.a = primaryColor.a * alpha;
                bracketMaterial.SetColor("_BaseColor", c);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up materials
            if (outerRingMaterial != null) Destroy(outerRingMaterial);
            if (innerDashMaterial != null) Destroy(innerDashMaterial);
            if (bracketMaterial != null) Destroy(bracketMaterial);
        }
    }
}
