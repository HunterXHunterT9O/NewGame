using UnityEngine;
using System;
using System.Collections;

namespace EdgeOfUniverse.RTS
{
    public class DropshipController : MonoBehaviour
    {
        [Header("Launch/Land")]
        [SerializeField] private float launchHeight = 25f;
        [SerializeField] private float launchDuration = 1.2f;
        [SerializeField] private float landDuration = 1.5f;

        [Header("Travel")]
        [SerializeField] private float cruiseHeight = 60f;
        [SerializeField] private float travelSpeed = 40f;

        [Header("Engine Trails")]
        [SerializeField] private Color trailColor = new Color(0.31f, 0.71f, 0.78f, 1f);

        private ParticleSystem[] engineTrails;
        private Animator animator;
        private Transform model;

        public bool IsFlying { get; private set; }

        public event Action OnLaunchComplete;
        public event Action OnArrivalComplete;

        private void Awake()
        {
            if (transform.childCount > 0)
            {
                model = transform.GetChild(0);
                if (model != null)
                {
                    animator = model.GetComponent<Animator>();
                    // Many imported models face -Z; rotate model child so visual forward matches transform.forward
                    model.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            CreateEngineTrails();
            SetTrailsActive(false);
        }

        private void CreateEngineTrails()
        {
            engineTrails = new ParticleSystem[2];
            Vector3[] nozzlePositions = new Vector3[]
            {
                new Vector3(-3f, 0f, -6f),
                new Vector3(3f, 0f, -6f)
            };

            for (int i = 0; i < 2; i++)
            {
                GameObject trailObj = new GameObject($"EngineTrail_{i}");
                trailObj.transform.SetParent(transform);
                trailObj.transform.localPosition = nozzlePositions[i];
                trailObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 3f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                main.startColor = trailColor;
                main.maxParticles = 100;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = ps.emission;
                emission.rateOverTime = 40;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                shape.radius = 0.05f;

                var colorOverLife = ps.colorOverLifetime;
                colorOverLife.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(Color.white, 0.3f), new GradientColorKey(trailColor * 0.5f, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
                );
                colorOverLife.color = grad;

                var sizeOverLife = ps.sizeOverLifetime;
                sizeOverLife.enabled = true;
                sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));

                var renderer = trailObj.GetComponent<ParticleSystemRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                mat.SetColor("_BaseColor", trailColor);
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 1); // Additive
                renderer.material = mat;

                engineTrails[i] = ps;
            }
        }

        private void SetTrailsActive(bool active)
        {
            if (engineTrails == null) return;
            foreach (var trail in engineTrails)
            {
                if (trail == null) continue;
                if (active) trail.Play();
                else trail.Stop();
            }
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            Vector3 dir = targetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }

        public void LaunchSequence()
        {
            Debug.Log("[Dropship] LaunchSequence called");
            StartCoroutine(LaunchCoroutine());
        }

        public void TravelTo(Transform destination, Action<float> onProgress)
        {
            Debug.Log($"[Dropship] TravelTo called, destination: {destination.position}");
            StartCoroutine(TravelCoroutine(destination.position, onProgress));
        }

        public void ArrivalSequence()
        {
            Debug.Log("[Dropship] ArrivalSequence called");
            StartCoroutine(ArrivalCoroutine());
        }

        private IEnumerator LaunchCoroutine()
        {
            IsFlying = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * launchHeight;

            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                try { animator.Play("Dropship_Anim_01"); }
                catch (System.Exception e) { Debug.LogWarning($"[Dropship] Animation error: {e.Message}"); }
            }

            yield return new WaitForSeconds(0.3f);
            SetTrailsActive(true);

            float elapsed = 0f;
            while (elapsed < launchDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseInOutCubic(elapsed / launchDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            transform.position = endPos;
            OnLaunchComplete?.Invoke();
        }

        private IEnumerator TravelCoroutine(Vector3 destination, Action<float> onProgress)
        {
            Vector3 start = transform.position;
            Vector3 end = new Vector3(destination.x, destination.y + launchHeight, destination.z);

            // Quadratic Bezier control point at cruise height
            Vector3 midPoint = (start + end) * 0.5f;
            midPoint.y = cruiseHeight;

            // Face travel direction
            Vector3 travelDir = (end - start).normalized;
            travelDir.y = 0f;
            if (travelDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(travelDir);
            }

            float totalDistance = Vector3.Distance(start, end);
            float elapsed = 0f;
            float travelDuration = totalDistance / travelSpeed;
            travelDuration = Mathf.Max(travelDuration, 2f);

            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / travelDuration;

                // Quadratic Bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
                float oneMinusT = 1f - t;
                Vector3 pos = oneMinusT * oneMinusT * start +
                              2f * oneMinusT * t * midPoint +
                              t * t * end;

                // Calculate tangent direction for facing
                Vector3 tangent = 2f * oneMinusT * (midPoint - start) + 2f * t * (end - midPoint);
                if (tangent.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(tangent.normalized);
                }

                transform.position = pos;
                onProgress?.Invoke(t);

                yield return null;
            }

            transform.position = end;
            onProgress?.Invoke(1f);
        }

        private IEnumerator ArrivalCoroutine()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(startPos.x, startPos.y - launchHeight, startPos.z);

            float elapsed = 0f;
            while (elapsed < landDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseInOutCubic(elapsed / landDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            transform.position = endPos;
            SetTrailsActive(false);
            IsFlying = false;
            OnArrivalComplete?.Invoke();
        }
    }
}
