using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace EdgeOfUniverse.UI
{
    /// <summary>
    /// UI component for displaying a single squad member's status.
    /// Shows portrait, health bar, and status indicators.
    /// Enhanced with emotional death sequence and polished feedback.
    /// </summary>
    public class SquadFrameUI : MonoBehaviour
    {
        [Header("Frame")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Sprite normalFrame;
        [SerializeField] private Sprite selectedFrame;
        [SerializeField] private Sprite woundedFrame;
        [SerializeField] private Sprite criticalFrame;
        [SerializeField] private Sprite deadFrame;

        [Header("Content")]
        [SerializeField] private Image portrait;
        [SerializeField] private Image healthBar;
        [SerializeField] private Image healthBarBackground;
        [SerializeField] private TextMeshProUGUI unitName;
        [SerializeField] private Image classIcon;

        [Header("Death Sequence Elements")]
        [SerializeField] private Image crackOverlay;
        [SerializeField] private Image staticOverlay;
        [SerializeField] private Image bloodSpatter;
        [SerializeField] private CanvasGroup frameCanvasGroup;

        [Header("Status Indicators")]
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject woundedIndicator;
        [SerializeField] private GameObject suppressedIndicator;
        [SerializeField] private GameObject reloadingIndicator;
        [SerializeField] private GameObject panicIndicator;
        [SerializeField] private TextMeshProUGUI criticalText;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = new Color(0.22f, 0.55f, 0.29f);
        [SerializeField] private Color woundedColor = new Color(0.78f, 0.55f, 0.16f);
        [SerializeField] private Color criticalColor = new Color(0.71f, 0.18f, 0.18f);
        [SerializeField] private Color deadGrayColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        // State
        private SelectableUnit trackedUnit;
        private float lastHealth = -1f;
        private bool isDead;
        private bool isInCriticalPulse;
        private Coroutine criticalPulseCoroutine;
        private Coroutine damageFlashCoroutine;

        // Cached
        private RectTransform rectTransform;
        private Color originalPortraitColor;
        private Color originalFrameColor;

        public SelectableUnit TrackedUnit => trackedUnit;
        public bool IsDead => isDead;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // Cache original colors
            if (portrait != null)
                originalPortraitColor = portrait.color;
            if (frameImage != null)
                originalFrameColor = frameImage.color;

            // Setup canvas group if needed
            if (frameCanvasGroup == null)
                frameCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            // Hide overlays initially
            HideOverlays();
        }

        private void Update()
        {
            if (trackedUnit != null && !isDead)
            {
                UpdateHealthDisplay();
            }
        }

        public void SetUnit(SelectableUnit unit)
        {
            // Reset state
            isDead = false;
            isInCriticalPulse = false;
            StopAllCoroutines();
            ResetVisuals();

            trackedUnit = unit;

            if (unit == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Subscribe to unit events
            unit.OnHealthChanged += HandleHealthChanged;
            unit.OnDeath += HandleUnitDeath;

            // Set unit name
            if (unitName != null)
                unitName.text = unit.UnitName;

            // Set portrait if available
            if (portrait != null && unit.Portrait != null)
                portrait.sprite = unit.Portrait;

            // Set class icon if available
            if (classIcon != null && unit.ClassIcon != null)
                classIcon.sprite = unit.ClassIcon;

            // Initial state update
            lastHealth = unit.HealthRatio;
            UpdateHealthDisplay();
            UpdateFrameState();
            UpdateIndicators();
        }

        public void ClearUnit()
        {
            // Unsubscribe from events
            if (trackedUnit != null)
            {
                trackedUnit.OnHealthChanged -= HandleHealthChanged;
                trackedUnit.OnDeath -= HandleUnitDeath;
            }

            trackedUnit = null;
            isDead = false;
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        private void ResetVisuals()
        {
            if (portrait != null)
                portrait.color = originalPortraitColor;
            if (frameImage != null)
                frameImage.color = originalFrameColor;
            if (frameCanvasGroup != null)
                frameCanvasGroup.alpha = 1f;

            transform.localScale = Vector3.one;
            HideOverlays();
        }

        private void HideOverlays()
        {
            if (crackOverlay != null) crackOverlay.gameObject.SetActive(false);
            if (staticOverlay != null) staticOverlay.gameObject.SetActive(false);
            if (bloodSpatter != null) bloodSpatter.gameObject.SetActive(false);
            if (criticalText != null) criticalText.gameObject.SetActive(false);
        }

        private void UpdateHealthDisplay()
        {
            if (trackedUnit == null) return;

            float healthRatio = trackedUnit.HealthRatio;

            // Only update if health changed
            if (Mathf.Approximately(healthRatio, lastHealth)) return;
            lastHealth = healthRatio;

            // Update health bar fill
            if (healthBar != null)
            {
                healthBar.fillAmount = healthRatio;
                healthBar.color = GetHealthColor(healthRatio);
            }

            // Update frame based on health
            UpdateFrameState();
        }

        private void UpdateFrameState()
        {
            if (frameImage == null || trackedUnit == null) return;

            float healthRatio = trackedUnit.HealthRatio;
            bool isSelected = trackedUnit.IsSelected;
            bool isDead = healthRatio <= 0f;

            Sprite targetFrame;

            if (isDead)
            {
                targetFrame = deadFrame;
            }
            else if (healthRatio <= 0.25f)
            {
                targetFrame = criticalFrame;
            }
            else if (healthRatio <= 0.5f)
            {
                targetFrame = woundedFrame;
            }
            else if (isSelected)
            {
                targetFrame = selectedFrame;
            }
            else
            {
                targetFrame = normalFrame;
            }

            if (targetFrame != null)
                frameImage.sprite = targetFrame;
        }

        private void UpdateIndicators()
        {
            if (trackedUnit == null) return;

            if (selectedIndicator != null)
                selectedIndicator.SetActive(trackedUnit.IsSelected);

            if (woundedIndicator != null)
                woundedIndicator.SetActive(trackedUnit.HealthRatio <= 0.5f && trackedUnit.HealthRatio > 0f);

            // Suppressed indicator would need additional unit state tracking
            if (suppressedIndicator != null)
                suppressedIndicator.SetActive(false);
        }

        private Color GetHealthColor(float ratio)
        {
            if (ratio > 0.5f)
                return healthyColor;
            else if (ratio > 0.25f)
                return woundedColor;
            else
                return criticalColor;
        }

        #region Event Handlers

        private void HandleHealthChanged(SelectableUnit unit, float newHealth)
        {
            if (unit != trackedUnit) return;

            float previousHealth = lastHealth;
            float healthRatio = unit.HealthRatio;

            // Damage taken
            if (healthRatio < previousHealth)
            {
                float damageTaken = previousHealth - healthRatio;
                FlashDamage(damageTaken > 0.2f); // Heavy hit if >20% health lost
            }

            // Entering critical
            if (healthRatio <= 0.25f && previousHealth > 0.25f)
            {
                StartCriticalPulse();
            }
            else if (healthRatio > 0.25f && isInCriticalPulse)
            {
                StopCriticalPulse();
            }

            lastHealth = healthRatio;
        }

        private void HandleUnitDeath(SelectableUnit unit)
        {
            if (unit != trackedUnit) return;

            isDead = true;
            StopCriticalPulse();
            StartCoroutine(DeathSequence());
        }

        #endregion

        #region Death Sequence - "This Should HURT"

        /// <summary>
        /// The full emotional death sequence
        /// </summary>
        private IEnumerator DeathSequence()
        {
            // 0.00s - Frame flashes white
            if (frameImage != null)
            {
                frameImage.color = Color.white;
            }
            yield return new WaitForSeconds(0.1f);

            // 0.10s - Crack effects appear
            if (crackOverlay != null)
            {
                crackOverlay.gameObject.SetActive(true);
                crackOverlay.color = new Color(1f, 1f, 1f, 0f);

                float elapsed = 0f;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.deltaTime;
                    crackOverlay.color = new Color(1f, 1f, 1f, elapsed / 0.2f);
                    yield return null;
                }
            }

            // 0.30s - Color drains to grayscale
            float drainDuration = 0.2f;
            float drainElapsed = 0f;

            Color startPortraitColor = portrait != null ? portrait.color : Color.white;
            Color startFrameColor = frameImage != null ? frameImage.color : Color.white;

            while (drainElapsed < drainDuration)
            {
                drainElapsed += Time.deltaTime;
                float t = drainElapsed / drainDuration;

                if (portrait != null)
                {
                    portrait.color = Color.Lerp(startPortraitColor, deadGrayColor, t);
                }
                if (frameImage != null)
                {
                    frameImage.color = Color.Lerp(startFrameColor, deadGrayColor, t);
                }

                yield return null;
            }

            // 0.50s - Frame "powers down" (LED fade effect)
            if (frameCanvasGroup != null)
            {
                float fadeElapsed = 0f;
                float fadeDuration = 0.2f;

                while (fadeElapsed < fadeDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    float t = fadeElapsed / fadeDuration;

                    // Flicker effect
                    float flicker = Mathf.PerlinNoise(fadeElapsed * 20f, 0f);
                    frameCanvasGroup.alpha = Mathf.Lerp(1f, 0.3f, t) * (0.7f + flicker * 0.3f);

                    yield return null;
                }
            }

            // 0.70s - Portrait goes dark
            if (portrait != null)
            {
                float darkElapsed = 0f;
                Color currentColor = portrait.color;
                Color darkColor = new Color(0.15f, 0.15f, 0.15f, currentColor.a);

                while (darkElapsed < 0.3f)
                {
                    darkElapsed += Time.deltaTime;
                    portrait.color = Color.Lerp(currentColor, darkColor, darkElapsed / 0.3f);
                    yield return null;
                }
            }

            // 1.00s - Static overlay appears
            if (staticOverlay != null)
            {
                staticOverlay.gameObject.SetActive(true);
                staticOverlay.color = new Color(1f, 1f, 1f, 0f);

                float staticElapsed = 0f;
                while (staticElapsed < 0.5f)
                {
                    staticElapsed += Time.deltaTime;

                    // Flickering static
                    float noise = Mathf.PerlinNoise(staticElapsed * 15f, Time.time * 10f);
                    staticOverlay.color = new Color(1f, 1f, 1f, noise * 0.5f);

                    yield return null;
                }

                // Settle to persistent static
                staticOverlay.color = new Color(1f, 1f, 1f, 0.3f);
            }

            // Set dead frame sprite
            if (frameImage != null && deadFrame != null)
            {
                frameImage.sprite = deadFrame;
                frameImage.color = deadGrayColor;
            }

            // Restore alpha but keep grayed
            if (frameCanvasGroup != null)
            {
                frameCanvasGroup.alpha = 0.7f;
            }

            // The frame remains as a memorial - they were someone
        }

        #endregion

        #region Damage Feedback

        /// <summary>
        /// Flash the frame to draw attention
        /// </summary>
        public void FlashDamage(bool heavy = false)
        {
            if (frameImage == null || isDead) return;

            if (damageFlashCoroutine != null)
                StopCoroutine(damageFlashCoroutine);

            damageFlashCoroutine = StartCoroutine(DamageFlashSequence(heavy));
        }

        private IEnumerator DamageFlashSequence(bool heavy)
        {
            float duration = heavy ? 0.25f : 0.15f;

            // Flash red
            if (frameImage != null)
                frameImage.color = Color.red;

            // Shake for heavy hits
            if (heavy && rectTransform != null)
            {
                Vector2 originalPos = rectTransform.anchoredPosition;
                float shakeElapsed = 0f;
                float shakeDuration = 0.15f;

                while (shakeElapsed < shakeDuration)
                {
                    shakeElapsed += Time.deltaTime;
                    float intensity = 5f * (1f - shakeElapsed / shakeDuration);
                    float x = Random.Range(-1f, 1f) * intensity;
                    float y = Random.Range(-1f, 1f) * intensity;
                    rectTransform.anchoredPosition = originalPos + new Vector2(x, y);
                    yield return null;
                }
                rectTransform.anchoredPosition = originalPos;

                // Show blood spatter for heavy hits
                if (bloodSpatter != null)
                {
                    bloodSpatter.gameObject.SetActive(true);
                    bloodSpatter.color = new Color(0.5f, 0f, 0f, 0.6f);

                    // Fade out slowly
                    StartCoroutine(FadeOutOverlay(bloodSpatter, 2f));
                }
            }

            // Fade back to normal
            float elapsed = 0f;
            Color startColor = frameImage != null ? frameImage.color : Color.red;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (frameImage != null)
                    frameImage.color = Color.Lerp(startColor, originalFrameColor, t);

                yield return null;
            }

            if (frameImage != null)
                frameImage.color = originalFrameColor;
        }

        private IEnumerator FadeOutOverlay(Image overlay, float duration)
        {
            if (overlay == null) yield break;

            Color startColor = overlay.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                overlay.color = Color.Lerp(startColor, endColor, elapsed / duration);
                yield return null;
            }

            overlay.gameObject.SetActive(false);
        }

        #endregion

        #region Critical State

        private void StartCriticalPulse()
        {
            if (isInCriticalPulse) return;
            isInCriticalPulse = true;

            if (criticalText != null)
            {
                criticalText.gameObject.SetActive(true);
                criticalText.text = "CRITICAL";
                criticalText.color = criticalColor;
            }

            criticalPulseCoroutine = StartCoroutine(CriticalPulseLoop());
        }

        private void StopCriticalPulse()
        {
            isInCriticalPulse = false;

            if (criticalPulseCoroutine != null)
            {
                StopCoroutine(criticalPulseCoroutine);
                criticalPulseCoroutine = null;
            }

            if (criticalText != null)
                criticalText.gameObject.SetActive(false);

            // Reset colors
            if (healthBar != null)
                healthBar.color = GetHealthColor(trackedUnit?.HealthRatio ?? 0f);
        }

        private IEnumerator CriticalPulseLoop()
        {
            while (isInCriticalPulse && !isDead)
            {
                // Pulse the health bar red
                if (healthBar != null)
                {
                    float elapsed = 0f;
                    float duration = 0.3f;

                    // Bright
                    while (elapsed < duration && isInCriticalPulse)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / duration;
                        healthBar.color = Color.Lerp(criticalColor, Color.red, t);
                        yield return null;
                    }

                    // Dim
                    elapsed = 0f;
                    while (elapsed < duration && isInCriticalPulse)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / duration;
                        healthBar.color = Color.Lerp(Color.red, criticalColor, t);
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        #endregion

        #region Selection Animation

        /// <summary>
        /// Pulse the frame when selected
        /// </summary>
        public void PulseSelected()
        {
            if (frameImage == null || isDead) return;
            StartCoroutine(SelectionPulseSequence());
        }

        private IEnumerator SelectionPulseSequence()
        {
            Vector3 original = Vector3.one;
            Vector3 target = Vector3.one * 1.12f;
            float duration = 0.12f;

            // Quick scale up with overshoot
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = UIAnimationUtility.EaseOutBack(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(original, target, t);
                yield return null;
            }

            // Settle back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(target, original, t);
                yield return null;
            }

            transform.localScale = original;
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (trackedUnit != null)
            {
                trackedUnit.OnHealthChanged -= HandleHealthChanged;
                trackedUnit.OnDeath -= HandleUnitDeath;
            }
        }
    }
}
