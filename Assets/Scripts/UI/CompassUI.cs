using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace EdgeOfUniverse.UI
{
    /// <summary>
    /// The compass UI widget - "It points you home."
    /// The emotional anchor of the father-daughter relationship.
    /// </summary>
    public class CompassUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image compassBody;
        [SerializeField] private RectTransform needleTransform;
        [SerializeField] private Image needleImage;
        [SerializeField] private Image glassOverlay;
        [SerializeField] private Image glowEffect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Sprites")]
        [SerializeField] private Sprite compassBodySprite;
        [SerializeField] private Sprite compassNeedleSprite;
        [SerializeField] private Sprite compassGlassSprite;

        [Header("Target Tracking")]
        [SerializeField] private Transform extractionPoint;
        [SerializeField] private Transform daughterTransform;
        [SerializeField] private bool trackDaughter = false;

        [Header("Animation Settings")]
        [SerializeField] private float needleSwayAmount = 5f;
        [SerializeField] private float needleSwaySpeed = 1.5f;
        [SerializeField] private float needleSmoothSpeed = 3f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverDuration = 0.2f;

        [Header("Glow Settings")]
        [SerializeField] private Color normalGlow = new Color(0.8f, 0.6f, 0.3f, 0.3f);     // Warm amber
        [SerializeField] private Color nearExtractionGlow = new Color(0.3f, 0.8f, 0.9f, 0.5f); // Cyan hope
        [SerializeField] private Color dangerGlow = new Color(0.9f, 0.2f, 0.2f, 0.4f);     // Red anxiety
        [SerializeField] private Color storyGlow = new Color(1f, 0.9f, 0.5f, 0.7f);        // Bright gold

        [Header("State")]
        [SerializeField] private CompassState currentState = CompassState.Normal;

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipObject;
        [SerializeField] private string tooltipText = "The compass always points home.";

        public enum CompassState
        {
            Normal,
            NearExtraction,
            Danger,
            StoryMoment
        }

        // Internal state
        private float targetNeedleAngle;
        private float currentNeedleAngle;
        private float swayOffset;
        private bool isHovered;
        private Vector3 originalScale;
        private Camera mainCamera;
        private Coroutine hoverCoroutine;
        private MissionHUD.ThreatLevel currentThreatLevel;

        // Events
        public static event System.Action OnCompassClicked;

        private void Start()
        {
            originalScale = transform.localScale;

            // Find camera
            var camController = FindAnyObjectByType<RTSCameraController>();
            mainCamera = camController != null ? camController.Camera : Camera.main;

            // Initialize visuals
            SetupVisuals();

            // Subscribe to threat changes
            MissionHUD.OnThreatLevelChanged += HandleThreatChanged;

            // Hide tooltip initially
            if (tooltipObject != null)
                tooltipObject.SetActive(false);

            // Initial glow
            UpdateGlow();
        }

        private void OnDestroy()
        {
            MissionHUD.OnThreatLevelChanged -= HandleThreatChanged;
        }

        private void Update()
        {
            UpdateNeedleTarget();
            UpdateNeedleAnimation();
            UpdateGlassReflection();
        }

        #region Setup

        private void SetupVisuals()
        {
            if (compassBody != null && compassBodySprite != null)
                compassBody.sprite = compassBodySprite;

            if (needleImage != null && compassNeedleSprite != null)
                needleImage.sprite = compassNeedleSprite;

            if (glassOverlay != null && compassGlassSprite != null)
                glassOverlay.sprite = compassGlassSprite;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        #endregion

        #region Needle Animation

        private void UpdateNeedleTarget()
        {
            Transform target = trackDaughter && daughterTransform != null
                ? daughterTransform
                : extractionPoint;

            if (target != null && mainCamera != null)
            {
                // Get direction to target in screen space
                Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.position);
                Vector3 compassScreenPos = RectTransformUtility.WorldToScreenPoint(
                    mainCamera, transform.position);

                Vector2 direction = (Vector2)(targetScreenPos - compassScreenPos);

                if (direction.magnitude > 1f)
                {
                    // Convert to angle (0 = up, clockwise positive)
                    targetNeedleAngle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                }
            }
            else
            {
                // No target - needle drifts
                targetNeedleAngle = Mathf.Sin(Time.time * 0.3f) * 30f;
            }
        }

        private void UpdateNeedleAnimation()
        {
            // Add organic sway based on state
            float swayMultiplier = currentState switch
            {
                CompassState.Normal => 1f,
                CompassState.NearExtraction => 0.3f,  // More stable when close to home
                CompassState.Danger => 2.5f,          // Erratic in danger
                CompassState.StoryMoment => 0.1f,     // Almost still for dramatic effect
                _ => 1f
            };

            swayOffset = Mathf.Sin(Time.time * needleSwaySpeed) * needleSwayAmount * swayMultiplier;

            // Additional erratic movement in danger
            if (currentState == CompassState.Danger)
            {
                swayOffset += Mathf.PerlinNoise(Time.time * 5f, 0f) * needleSwayAmount * 2f - needleSwayAmount;
            }

            // Smooth interpolation to target
            float smoothSpeed = currentState == CompassState.Danger ? needleSmoothSpeed * 0.5f : needleSmoothSpeed;
            currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, targetNeedleAngle + swayOffset, Time.deltaTime * smoothSpeed);

            // Apply rotation
            if (needleTransform != null)
            {
                needleTransform.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
            }
        }

        private void UpdateGlassReflection()
        {
            if (glassOverlay == null || mainCamera == null) return;

            // Subtle reflection movement based on camera angle
            Vector3 camForward = mainCamera.transform.forward;
            float offsetX = camForward.x * 0.02f;
            float offsetY = camForward.z * 0.02f;

            // Apply as UV offset or position offset
            glassOverlay.rectTransform.anchoredPosition = new Vector2(offsetX * 10f, offsetY * 10f);
        }

        #endregion

        #region State Management

        /// <summary>
        /// Set the compass state
        /// </summary>
        public void SetState(CompassState state)
        {
            if (currentState == state) return;

            currentState = state;
            UpdateGlow();

            // Special animation for story moment
            if (state == CompassState.StoryMoment)
            {
                StartCoroutine(StoryMomentSequence());
            }
        }

        /// <summary>
        /// Set extraction point for compass to track
        /// </summary>
        public void SetExtractionPoint(Transform point)
        {
            extractionPoint = point;
            CheckExtractionProximity();
        }

        /// <summary>
        /// Set daughter transform for compass to track
        /// </summary>
        public void SetDaughterTransform(Transform daughter)
        {
            daughterTransform = daughter;
        }

        /// <summary>
        /// Toggle between tracking extraction and daughter
        /// </summary>
        public void ToggleTracking()
        {
            trackDaughter = !trackDaughter;

            // Visual feedback for toggle
            StartCoroutine(ToggleFeedback());
        }

        private void CheckExtractionProximity()
        {
            if (extractionPoint == null || mainCamera == null) return;

            float distance = Vector3.Distance(mainCamera.transform.position, extractionPoint.position);

            // If close to extraction and not in danger, show hope
            if (distance < 30f && currentState != CompassState.Danger)
            {
                SetState(CompassState.NearExtraction);
            }
            else if (currentState == CompassState.NearExtraction)
            {
                SetState(CompassState.Normal);
            }
        }

        private void HandleThreatChanged(MissionHUD.ThreatLevel previous, MissionHUD.ThreatLevel current)
        {
            currentThreatLevel = current;

            // Update state based on threat
            if (current >= MissionHUD.ThreatLevel.Swarm)
            {
                SetState(CompassState.Danger);
            }
            else if (currentState == CompassState.Danger)
            {
                // Return to normal or near extraction
                CheckExtractionProximity();
                if (currentState == CompassState.Danger)
                {
                    SetState(CompassState.Normal);
                }
            }

            UpdateGlow();
        }

        private void UpdateGlow()
        {
            if (glowEffect == null) return;

            Color targetGlow = currentState switch
            {
                CompassState.Normal => normalGlow,
                CompassState.NearExtraction => nearExtractionGlow,
                CompassState.Danger => dangerGlow,
                CompassState.StoryMoment => storyGlow,
                _ => normalGlow
            };

            // Modify glow based on threat level even in normal state
            if (currentState == CompassState.Normal && currentThreatLevel >= MissionHUD.ThreatLevel.Alerted)
            {
                targetGlow = Color.Lerp(normalGlow, dangerGlow, (float)currentThreatLevel / 4f);
            }

            StartCoroutine(LerpGlowColor(targetGlow, 0.3f));
        }

        private IEnumerator LerpGlowColor(Color target, float duration)
        {
            if (glowEffect == null) yield break;

            Color start = glowEffect.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                glowEffect.color = Color.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            glowEffect.color = target;
        }

        #endregion

        #region Interaction

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(HoverAnimation(true));

            // Show tooltip
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(HoverAnimation(false));

            // Hide tooltip
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Toggle tracking mode or open minimap
            OnCompassClicked?.Invoke();
            ToggleTracking();
        }

        private IEnumerator HoverAnimation(bool hovering)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = hovering ? originalScale * hoverScale : originalScale;
            float elapsed = 0f;

            while (elapsed < hoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / hoverDuration;

                // Ease out back for satisfying pop
                if (hovering)
                {
                    t = 1f - Mathf.Pow(1f - t, 3f);
                }
                else
                {
                    t = t * t * (3f - 2f * t);
                }

                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        private IEnumerator ToggleFeedback()
        {
            // Quick pulse when toggling
            Vector3 startScale = transform.localScale;

            float elapsed = 0f;
            float duration = 0.15f;

            // Pulse up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.1f, t);
                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale * 1.1f, startScale, t);
                yield return null;
            }

            transform.localScale = startScale;

            // Flash the needle
            if (needleImage != null)
            {
                Color originalColor = needleImage.color;
                needleImage.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                needleImage.color = originalColor;
            }
        }

        private IEnumerator StoryMomentSequence()
        {
            // Dramatic glow buildup
            if (glowEffect != null)
            {
                float duration = 1f;
                float elapsed = 0f;

                Color startColor = glowEffect.color;
                Color peakColor = storyGlow;
                peakColor.a = 0.9f;

                // Build up
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    glowEffect.color = Color.Lerp(startColor, peakColor, t);

                    // Needle stabilizes pointing at daughter
                    trackDaughter = true;

                    yield return null;
                }

                // Hold bright
                yield return new WaitForSeconds(0.5f);

                // Gentle pulse
                for (int i = 0; i < 3; i++)
                {
                    elapsed = 0f;
                    duration = 0.5f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / duration;
                        float pulse = Mathf.Sin(t * Mathf.PI);
                        peakColor.a = 0.7f + pulse * 0.2f;
                        glowEffect.color = peakColor;
                        yield return null;
                    }
                }
            }
        }

        #endregion

        #region Context Menu Testing

        [ContextMenu("Test Normal State")]
        private void TestNormal() => SetState(CompassState.Normal);

        [ContextMenu("Test Near Extraction")]
        private void TestNearExtraction() => SetState(CompassState.NearExtraction);

        [ContextMenu("Test Danger State")]
        private void TestDanger() => SetState(CompassState.Danger);

        [ContextMenu("Test Story Moment")]
        private void TestStoryMoment() => SetState(CompassState.StoryMoment);

        #endregion
    }
}
