using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using EdgeOfUniverse.UI;

namespace EdgeOfUniverse.VFX
{
    /// <summary>
    /// Controls screen-space post-processing effects tied to gameplay events.
    /// Manipulates URP Volume Profile for vignette, chromatic aberration, film grain, etc.
    /// </summary>
    public class ScreenEffectsController : MonoBehaviour
    {
        public static ScreenEffectsController Instance { get; private set; }

        [Header("Volume Reference")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private bool createVolumeIfMissing = true;

        [Header("Damage Effects")]
        [SerializeField] private Color damageVignetteColor = new Color(0.5f, 0f, 0f, 1f);
        [SerializeField] private float damageVignetteIntensity = 0.4f;
        [SerializeField] private float damageVignetteDuration = 0.2f;
        [SerializeField] private float lowHealthVignetteIntensity = 0.3f;
        [SerializeField] private float lowHealthPulseSpeed = 1.5f;

        [Header("Threat Level Effects")]
        [SerializeField] private float maxThreatVignetteIntensity = 0.4f;
        [SerializeField] private float maxChromaticAberration = 0.4f;
        [SerializeField] private float maxFilmGrain = 0.25f;

        [Header("Explosion Effects")]
        [SerializeField] private float explosionFlashDuration = 0.05f;
        [SerializeField] private float explosionChromaticSpike = 0.5f;

        [Header("Screen Shake")]
        [SerializeField] private float maxShakeIntensity = 0.5f;
        [SerializeField] private float shakeDamping = 5f;

        // URP Components
        private VolumeProfile volumeProfile;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private FilmGrain filmGrain;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;

        // State
        private float baseVignetteIntensity;
        private float baseChromaticAberration;
        private float baseFilmGrain;
        private float currentShakeTrauma;
        private MissionHUD.ThreatLevel currentThreatLevel;
        private bool isLowHealth;
        private float lowHealthTimer;

        // Camera shake
        private Camera mainCamera;
        private Vector3 originalCameraLocalPos;

        // Effect coroutines
        private Coroutine damageCoroutine;
        private Coroutine explosionCoroutine;
        private Coroutine criticalPulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupVolume();
            FindCamera();
        }

        private void OnEnable()
        {
            MissionHUD.OnThreatLevelChanged += HandleThreatLevelChanged;
        }

        private void OnDisable()
        {
            MissionHUD.OnThreatLevelChanged -= HandleThreatLevelChanged;
        }

        private void Update()
        {
            UpdateScreenShake();
            UpdateLowHealthPulse();
            UpdateThreatEffects();
        }

        #region Setup

        private void SetupVolume()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindAnyObjectByType<Volume>();

                if (postProcessVolume == null && createVolumeIfMissing)
                {
                    GameObject volumeObj = new GameObject("ScreenEffects_Volume");
                    volumeObj.transform.SetParent(transform);
                    postProcessVolume = volumeObj.AddComponent<Volume>();
                    postProcessVolume.isGlobal = true;
                    postProcessVolume.priority = 100;
                }
            }

            if (postProcessVolume != null)
            {
                // Create or get profile
                if (postProcessVolume.profile == null)
                {
                    volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    postProcessVolume.profile = volumeProfile;
                }
                else
                {
                    volumeProfile = postProcessVolume.profile;
                }

                SetupVolumeComponents();
            }
        }

        private void SetupVolumeComponents()
        {
            // Vignette
            if (!volumeProfile.TryGet(out vignette))
            {
                vignette = volumeProfile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.intensity.Override(0f);
            vignette.color.Override(damageVignetteColor);
            baseVignetteIntensity = 0f;

            // Chromatic Aberration
            if (!volumeProfile.TryGet(out chromaticAberration))
            {
                chromaticAberration = volumeProfile.Add<ChromaticAberration>(true);
            }
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0f);
            baseChromaticAberration = 0f;

            // Film Grain
            if (!volumeProfile.TryGet(out filmGrain))
            {
                filmGrain = volumeProfile.Add<FilmGrain>(true);
            }
            filmGrain.active = true;
            filmGrain.intensity.Override(0.05f); // Subtle base grain
            filmGrain.type.Override(FilmGrainLookup.Medium);
            baseFilmGrain = 0.05f;

            // Color Adjustments
            if (!volumeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            colorAdjustments.saturation.Override(0f);
            colorAdjustments.postExposure.Override(0f);

            // Bloom
            if (!volumeProfile.TryGet(out bloom))
            {
                bloom = volumeProfile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(0.9f);
        }

        private void FindCamera()
        {
            var cameraController = FindAnyObjectByType<RTSCameraController>();
            if (cameraController != null)
            {
                mainCamera = cameraController.Camera;
            }
            else
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                originalCameraLocalPos = mainCamera.transform.localPosition;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Trigger damage vignette pulse
        /// </summary>
        public void TriggerDamageEffect(float damagePercent = 0.5f)
        {
            if (damageCoroutine != null) StopCoroutine(damageCoroutine);
            damageCoroutine = StartCoroutine(DamageVignetteSequence(damagePercent));

            // Add trauma for shake
            AddTrauma(damagePercent * 0.3f);
        }

        /// <summary>
        /// Set low health state for persistent pulsing
        /// </summary>
        public void SetLowHealthState(bool isLow, float healthPercent = 0.25f)
        {
            isLowHealth = isLow;
            if (!isLow)
            {
                lowHealthTimer = 0f;
            }
        }

        /// <summary>
        /// Trigger explosion flash and shake
        /// </summary>
        public void TriggerExplosion(float intensity = 1f, float distance = 0f)
        {
            // Scale by distance (if provided)
            float distanceScale = distance > 0 ? Mathf.Clamp01(1f - distance / 50f) : 1f;
            float finalIntensity = intensity * distanceScale;

            if (finalIntensity > 0.1f)
            {
                if (explosionCoroutine != null) StopCoroutine(explosionCoroutine);
                explosionCoroutine = StartCoroutine(ExplosionSequence(finalIntensity));

                AddTrauma(finalIntensity * 0.5f);
            }
        }

        /// <summary>
        /// Trigger critical threat pulse (called from MissionHUD)
        /// </summary>
        public void TriggerCriticalPulse()
        {
            if (criticalPulseCoroutine != null) StopCoroutine(criticalPulseCoroutine);
            criticalPulseCoroutine = StartCoroutine(CriticalPulseSequence());
        }

        /// <summary>
        /// Add camera shake trauma
        /// </summary>
        public void AddTrauma(float amount)
        {
            currentShakeTrauma = Mathf.Clamp01(currentShakeTrauma + amount);
        }

        /// <summary>
        /// Flash the screen with a color
        /// </summary>
        public void FlashScreen(Color color, float duration = 0.1f)
        {
            StartCoroutine(ScreenFlashSequence(color, duration));
        }

        #endregion

        #region Effect Sequences

        private IEnumerator DamageVignetteSequence(float intensity)
        {
            if (vignette == null) yield break;

            float targetIntensity = damageVignetteIntensity * intensity;
            float elapsed = 0f;

            // Quick in
            while (elapsed < damageVignetteDuration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (damageVignetteDuration * 0.3f);
                vignette.intensity.Override(Mathf.Lerp(baseVignetteIntensity, targetIntensity, t));
                yield return null;
            }

            // Slower out
            elapsed = 0f;
            while (elapsed < damageVignetteDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (damageVignetteDuration * 0.7f);
                float current = Mathf.Lerp(targetIntensity, GetThreatVignetteIntensity(), t);
                vignette.intensity.Override(current);
                yield return null;
            }

            vignette.intensity.Override(GetThreatVignetteIntensity());
        }

        private IEnumerator ExplosionSequence(float intensity)
        {
            // White flash
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.Override(intensity * 2f);
            }

            // Chromatic spike
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.Override(explosionChromaticSpike * intensity);
            }

            yield return new WaitForSeconds(explosionFlashDuration);

            // Fade out
            float elapsed = 0f;
            float duration = 0.15f;
            float startExposure = colorAdjustments?.postExposure.value ?? 0f;
            float startCA = chromaticAberration?.intensity.value ?? 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (colorAdjustments != null)
                    colorAdjustments.postExposure.Override(Mathf.Lerp(startExposure, 0f, t));

                if (chromaticAberration != null)
                    chromaticAberration.intensity.Override(Mathf.Lerp(startCA, GetThreatChromaticIntensity(), t));

                yield return null;
            }

            if (colorAdjustments != null)
                colorAdjustments.postExposure.Override(0f);

            if (chromaticAberration != null)
                chromaticAberration.intensity.Override(GetThreatChromaticIntensity());
        }

        private IEnumerator CriticalPulseSequence()
        {
            if (vignette == null) yield break;

            // Quick red pulse
            Color criticalColor = new Color(0.8f, 0f, 0f, 1f);
            vignette.color.Override(criticalColor);

            float startIntensity = vignette.intensity.value;
            float peakIntensity = maxThreatVignetteIntensity + 0.15f;

            // Pulse up
            float elapsed = 0f;
            float duration = 0.1f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignette.intensity.Override(Mathf.Lerp(startIntensity, peakIntensity, t));
                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignette.intensity.Override(Mathf.Lerp(peakIntensity, GetThreatVignetteIntensity(), t));
                yield return null;
            }

            vignette.intensity.Override(GetThreatVignetteIntensity());
        }

        private IEnumerator ScreenFlashSequence(Color color, float duration)
        {
            if (colorAdjustments == null) yield break;

            // Flash using color filter
            colorAdjustments.colorFilter.Override(color);
            colorAdjustments.postExposure.Override(0.5f);

            yield return new WaitForSeconds(duration * 0.3f);

            // Fade out
            float elapsed = 0f;
            while (elapsed < duration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.7f);

                Color fadeColor = Color.Lerp(color, Color.white, t);
                colorAdjustments.colorFilter.Override(fadeColor);
                colorAdjustments.postExposure.Override(Mathf.Lerp(0.5f, 0f, t));

                yield return null;
            }

            colorAdjustments.colorFilter.Override(Color.white);
            colorAdjustments.postExposure.Override(0f);
        }

        #endregion

        #region Update Methods

        private void UpdateScreenShake()
        {
            if (mainCamera == null || currentShakeTrauma <= 0) return;

            // Decay trauma
            currentShakeTrauma = Mathf.Max(0, currentShakeTrauma - Time.deltaTime * shakeDamping);

            // Calculate shake (trauma squared for exponential feel)
            float shake = currentShakeTrauma * currentShakeTrauma * maxShakeIntensity;

            float offsetX = Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f;
            float offsetY = Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f;

            mainCamera.transform.localPosition = originalCameraLocalPos +
                new Vector3(offsetX * shake, offsetY * shake, 0f);

            if (currentShakeTrauma <= 0.001f)
            {
                mainCamera.transform.localPosition = originalCameraLocalPos;
            }
        }

        private void UpdateLowHealthPulse()
        {
            if (!isLowHealth || vignette == null) return;

            lowHealthTimer += Time.deltaTime * lowHealthPulseSpeed;
            float pulse = (Mathf.Sin(lowHealthTimer * Mathf.PI * 2f) + 1f) * 0.5f;

            float intensity = Mathf.Lerp(GetThreatVignetteIntensity(), lowHealthVignetteIntensity, pulse);
            vignette.intensity.Override(intensity);

            // Also pulse the color toward red
            Color baseColor = GetThreatVignetteColor();
            Color healthColor = Color.Lerp(baseColor, damageVignetteColor, pulse * 0.5f);
            vignette.color.Override(healthColor);
        }

        private void UpdateThreatEffects()
        {
            // Smooth interpolation toward threat-based values when not in special states
            if (isLowHealth) return;

            if (vignette != null)
            {
                float targetVignette = GetThreatVignetteIntensity();
                float current = vignette.intensity.value;
                if (Mathf.Abs(current - targetVignette) > 0.001f)
                {
                    vignette.intensity.Override(Mathf.Lerp(current, targetVignette, Time.deltaTime * 3f));
                }

                vignette.color.Override(GetThreatVignetteColor());
            }

            if (chromaticAberration != null)
            {
                float targetCA = GetThreatChromaticIntensity();
                float current = chromaticAberration.intensity.value;
                if (Mathf.Abs(current - targetCA) > 0.001f)
                {
                    chromaticAberration.intensity.Override(Mathf.Lerp(current, targetCA, Time.deltaTime * 3f));
                }
            }

            if (filmGrain != null)
            {
                float targetGrain = GetThreatFilmGrainIntensity();
                float current = filmGrain.intensity.value;
                if (Mathf.Abs(current - targetGrain) > 0.001f)
                {
                    filmGrain.intensity.Override(Mathf.Lerp(current, targetGrain, Time.deltaTime * 3f));
                }
            }
        }

        private void HandleThreatLevelChanged(MissionHUD.ThreatLevel previous, MissionHUD.ThreatLevel current)
        {
            currentThreatLevel = current;

            // Immediate effects for escalation
            if (current > previous)
            {
                AddTrauma(0.1f * (int)current);
            }
        }

        #endregion

        #region Threat Level Calculations

        private float GetThreatVignetteIntensity()
        {
            return currentThreatLevel switch
            {
                MissionHUD.ThreatLevel.Clear => 0f,
                MissionHUD.ThreatLevel.Detected => maxThreatVignetteIntensity * 0.15f,
                MissionHUD.ThreatLevel.Alerted => maxThreatVignetteIntensity * 0.35f,
                MissionHUD.ThreatLevel.Swarm => maxThreatVignetteIntensity * 0.65f,
                MissionHUD.ThreatLevel.Critical => maxThreatVignetteIntensity,
                _ => 0f
            };
        }

        private Color GetThreatVignetteColor()
        {
            return currentThreatLevel switch
            {
                MissionHUD.ThreatLevel.Clear => new Color(0f, 0f, 0f, 1f),
                MissionHUD.ThreatLevel.Detected => new Color(0.4f, 0.3f, 0f, 1f),      // Amber tint
                MissionHUD.ThreatLevel.Alerted => new Color(0.5f, 0.2f, 0f, 1f),       // Orange tint
                MissionHUD.ThreatLevel.Swarm => new Color(0.5f, 0.1f, 0.1f, 1f),       // Red tint
                MissionHUD.ThreatLevel.Critical => new Color(0.6f, 0f, 0f, 1f),        // Deep red
                _ => Color.black
            };
        }

        private float GetThreatChromaticIntensity()
        {
            return currentThreatLevel switch
            {
                MissionHUD.ThreatLevel.Clear => 0f,
                MissionHUD.ThreatLevel.Detected => maxChromaticAberration * 0.1f,
                MissionHUD.ThreatLevel.Alerted => maxChromaticAberration * 0.25f,
                MissionHUD.ThreatLevel.Swarm => maxChromaticAberration * 0.5f,
                MissionHUD.ThreatLevel.Critical => maxChromaticAberration,
                _ => 0f
            };
        }

        private float GetThreatFilmGrainIntensity()
        {
            return currentThreatLevel switch
            {
                MissionHUD.ThreatLevel.Clear => baseFilmGrain,
                MissionHUD.ThreatLevel.Detected => baseFilmGrain,
                MissionHUD.ThreatLevel.Alerted => baseFilmGrain + maxFilmGrain * 0.25f,
                MissionHUD.ThreatLevel.Swarm => baseFilmGrain + maxFilmGrain * 0.5f,
                MissionHUD.ThreatLevel.Critical => baseFilmGrain + maxFilmGrain,
                _ => baseFilmGrain
            };
        }

        #endregion
    }
}
