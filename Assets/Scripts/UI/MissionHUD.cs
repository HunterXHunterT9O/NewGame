using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace EdgeOfUniverse.UI
{
    /// <summary>
    /// Mission HUD for RTS gameplay. Displays threat level, squad status,
    /// mission objectives, and resources.
    /// Investor-grade threat meter with terrifying escalation.
    /// </summary>
    public class MissionHUD : MonoBehaviour
    {
        [Header("Threat Meter")]
        [SerializeField] private Image threatFill;
        [SerializeField] private Image threatFrame;
        [SerializeField] private Image threatGlow;
        [SerializeField] private TextMeshProUGUI threatLabel;
        [SerializeField] private Sprite[] threatSprites; // Clear, Detected, Alerted, Swarm, Critical
        [SerializeField] private RectTransform threatContainer;
        [SerializeField] private ParticleSystem threatParticles;

        [Header("Threat Animation Settings")]
        [SerializeField] private float threatFillSpeed = 2f;
        [SerializeField] private AnimationCurve threatPulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.08f);

        [Header("Squad Panel")]
        [SerializeField] private Transform squadContainer;
        [SerializeField] private GameObject squadFramePrefab;
        [SerializeField] private int maxSquadDisplay = 6;

        [Header("Mission Info")]
        [SerializeField] private TextMeshProUGUI missionTitle;
        [SerializeField] private TextMeshProUGUI missionTimer;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private Image missionPanel;

        [Header("Resources")]
        [SerializeField] private Image ammoBar;
        [SerializeField] private Image supplyBar;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI supplyText;

        [Header("Alerts")]
        [SerializeField] private GameObject alertContainer;
        [SerializeField] private Image alertIcon;
        [SerializeField] private TextMeshProUGUI alertText;

        // Threat meter state
        private ThreatLevel currentThreat = ThreatLevel.Clear;
        private ThreatLevel previousThreat = ThreatLevel.Clear;
        private float targetThreatFill;
        private float currentThreatFill;
        private float threatPulseTimer;
        private bool isTransitioning;
        private Coroutine criticalCoroutine;
        private Coroutine pulseCoroutine;

        // Mission state
        private float missionTime;
        private bool missionActive;
        private List<SquadFrameUI> squadFrames = new List<SquadFrameUI>();

        // Screen effects reference (will be found at runtime)
        private VFX.ScreenEffectsController screenEffects;

        // Threat colors - terrifying progression
        private static readonly Color ThreatClear = new Color(0.22f, 0.63f, 0.29f);      // #38A04B - Calm green
        private static readonly Color ThreatDetected = new Color(0.78f, 0.55f, 0.16f);   // #C88C28 - Amber warning
        private static readonly Color ThreatAlerted = new Color(0.90f, 0.47f, 0.12f);    // #E67820 - Orange danger
        private static readonly Color ThreatSwarm = new Color(0.71f, 0.18f, 0.18f);      // #B62E2E - Blood red
        private static readonly Color ThreatCritical = new Color(1f, 0.12f, 0.12f);      // #FF1E1E - SCREAMING red

        // Pulse intervals by threat level (faster = more danger)
        private static readonly float[] PulseIntervals = { 0f, 3f, 1.5f, 0.5f, 0.25f };
        private static readonly float[] PulseScales = { 1f, 1.02f, 1.05f, 1.08f, 1.12f };

        public enum ThreatLevel
        {
            Clear = 0,
            Detected = 1,
            Alerted = 2,
            Swarm = 3,
            Critical = 4
        }

        // Events for screen effects integration
        public static event System.Action<ThreatLevel, ThreatLevel> OnThreatLevelChanged;

        private void Start()
        {
            InitializeHUD();
            screenEffects = FindAnyObjectByType<VFX.ScreenEffectsController>();
        }

        private void Update()
        {
            if (missionActive)
            {
                UpdateMissionTimer();
            }

            UpdateThreatMeterAnimation();
        }

        private void InitializeHUD()
        {
            // Initialize squad frames pool
            if (squadContainer != null && squadFramePrefab != null)
            {
                for (int i = 0; i < maxSquadDisplay; i++)
                {
                    var frame = Instantiate(squadFramePrefab, squadContainer);
                    var frameUI = frame.GetComponent<SquadFrameUI>();
                    if (frameUI == null)
                        frameUI = frame.AddComponent<SquadFrameUI>();

                    squadFrames.Add(frameUI);
                    frame.SetActive(false);
                }
            }

            // Hide alert by default
            if (alertContainer != null)
                alertContainer.SetActive(false);

            // Initialize threat glow
            if (threatGlow != null)
            {
                threatGlow.color = new Color(ThreatClear.r, ThreatClear.g, ThreatClear.b, 0f);
            }

            SetThreatLevel(ThreatLevel.Clear, immediate: true);
        }

        private void UpdateThreatMeterAnimation()
        {
            // Smooth fill interpolation
            if (Mathf.Abs(currentThreatFill - targetThreatFill) > 0.001f)
            {
                currentThreatFill = Mathf.Lerp(currentThreatFill, targetThreatFill, Time.deltaTime * threatFillSpeed);
                if (threatFill != null)
                {
                    threatFill.fillAmount = currentThreatFill;
                }
            }

            // Continuous pulse based on threat level
            if (currentThreat != ThreatLevel.Clear && !isTransitioning)
            {
                float interval = PulseIntervals[(int)currentThreat];
                if (interval > 0)
                {
                    threatPulseTimer += Time.deltaTime;
                    if (threatPulseTimer >= interval)
                    {
                        threatPulseTimer = 0f;
                        TriggerThreatPulse();
                    }
                }
            }
        }

        #region Threat Meter

        /// <summary>
        /// Set threat level with full animation sequence
        /// </summary>
        public void SetThreatLevel(ThreatLevel level, bool immediate = false)
        {
            if (level == currentThreat && !immediate) return;

            previousThreat = currentThreat;
            currentThreat = level;

            // Set target fill based on level (0-1 range)
            targetThreatFill = (float)level / 4f;

            if (immediate)
            {
                currentThreatFill = targetThreatFill;
                if (threatFill != null) threatFill.fillAmount = currentThreatFill;
                UpdateThreatVisuals(level);
            }
            else
            {
                // Play transition animation
                StartCoroutine(ThreatTransitionSequence(previousThreat, level));
            }

            // Fire event for screen effects
            OnThreatLevelChanged?.Invoke(previousThreat, level);

            // Handle critical state special behavior
            if (level == ThreatLevel.Critical)
            {
                if (criticalCoroutine != null) StopCoroutine(criticalCoroutine);
                criticalCoroutine = StartCoroutine(CriticalStateLoop());
            }
            else if (criticalCoroutine != null)
            {
                StopCoroutine(criticalCoroutine);
                criticalCoroutine = null;
            }
        }

        /// <summary>
        /// Set threat as a normalized value (0-1) for smooth transitions
        /// </summary>
        public void SetThreatValue(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            targetThreatFill = normalizedValue;

            // Determine level from value
            ThreatLevel newLevel = normalizedValue switch
            {
                < 0.2f => ThreatLevel.Clear,
                < 0.4f => ThreatLevel.Detected,
                < 0.6f => ThreatLevel.Alerted,
                < 0.8f => ThreatLevel.Swarm,
                _ => ThreatLevel.Critical
            };

            if (newLevel != currentThreat)
            {
                SetThreatLevel(newLevel);
            }
        }

        private IEnumerator ThreatTransitionSequence(ThreatLevel from, ThreatLevel to)
        {
            isTransitioning = true;
            bool escalating = to > from;

            // 0.00s - Flash current fill white
            if (threatFill != null && escalating)
            {
                Color originalColor = threatFill.color;
                threatFill.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                threatFill.color = originalColor;
            }

            // 0.10s - Burst particles upward (crack effect)
            if (threatParticles != null && escalating)
            {
                var main = threatParticles.main;
                main.startColor = GetThreatColor(to);
                threatParticles.Play();
            }

            // 0.15s - Color transition with glow
            float colorDuration = 0.2f;
            float elapsed = 0f;
            Color fromColor = GetThreatColor(from);
            Color toColor = GetThreatColor(to);

            while (elapsed < colorDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / colorDuration;

                // Ease in-out
                t = t * t * (3f - 2f * t);

                if (threatFill != null)
                    threatFill.color = Color.Lerp(fromColor, toColor, t);

                if (threatGlow != null)
                {
                    Color glowColor = toColor;
                    glowColor.a = escalating ? Mathf.Lerp(0.5f, 0.2f, t) : 0f;
                    threatGlow.color = glowColor;
                }

                yield return null;
            }

            // 0.20s - Shake the frame
            if (threatContainer != null && escalating)
            {
                StartCoroutine(ShakeTransform(threatContainer, 0.15f, 3f));
            }

            // 0.25s - Update label with flash
            UpdateThreatLabel(to);

            // 0.30s - Text flashes 3x for escalation
            if (threatLabel != null && escalating)
            {
                for (int i = 0; i < 3; i++)
                {
                    threatLabel.alpha = 0.3f;
                    yield return new WaitForSeconds(0.05f);
                    threatLabel.alpha = 1f;
                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Update sprite
            if (threatFill != null && threatSprites != null && threatSprites.Length > (int)to)
            {
                threatFill.sprite = threatSprites[(int)to];
            }

            // Update glow steady state
            UpdateThreatGlow(to);

            isTransitioning = false;
            threatPulseTimer = 0f;
        }

        private void UpdateThreatVisuals(ThreatLevel level)
        {
            if (threatFill != null)
            {
                threatFill.color = GetThreatColor(level);
                if (threatSprites != null && threatSprites.Length > (int)level)
                {
                    threatFill.sprite = threatSprites[(int)level];
                }
            }

            UpdateThreatLabel(level);
            UpdateThreatGlow(level);
        }

        private void UpdateThreatLabel(ThreatLevel level)
        {
            if (threatLabel == null) return;

            threatLabel.text = GetThreatLabelText(level);
            threatLabel.color = GetThreatColor(level);
        }

        private void UpdateThreatGlow(ThreatLevel level)
        {
            if (threatGlow == null) return;

            Color glowColor = GetThreatColor(level);

            // Glow intensity increases with threat
            float glowAlpha = level switch
            {
                ThreatLevel.Clear => 0f,
                ThreatLevel.Detected => 0.1f,
                ThreatLevel.Alerted => 0.2f,
                ThreatLevel.Swarm => 0.35f,
                ThreatLevel.Critical => 0.5f,
                _ => 0f
            };

            glowColor.a = glowAlpha;
            threatGlow.color = glowColor;
        }

        private void TriggerThreatPulse()
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }

        private IEnumerator PulseAnimation()
        {
            if (threatContainer == null) yield break;

            float targetScale = PulseScales[(int)currentThreat];
            float duration = 0.15f;

            // Pulse up
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1f, targetScale, threatPulseCurve.Evaluate(t));
                threatContainer.localScale = Vector3.one * scale;
                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(targetScale, 1f, t);
                threatContainer.localScale = Vector3.one * scale;
                yield return null;
            }

            threatContainer.localScale = Vector3.one;

            // Glow pulse for high threat
            if (currentThreat >= ThreatLevel.Swarm && threatGlow != null)
            {
                StartCoroutine(GlowPulse());
            }
        }

        private IEnumerator GlowPulse()
        {
            if (threatGlow == null) yield break;

            Color baseColor = threatGlow.color;
            float baseAlpha = baseColor.a;
            float peakAlpha = Mathf.Min(baseAlpha + 0.3f, 0.8f);

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                baseColor.a = Mathf.Lerp(peakAlpha, baseAlpha, t);
                threatGlow.color = baseColor;
                yield return null;
            }

            baseColor.a = baseAlpha;
            threatGlow.color = baseColor;
        }

        private IEnumerator CriticalStateLoop()
        {
            bool showEvac = false;
            float textFlashInterval = 0.5f;
            float screenPulseInterval = 1f;
            float screenPulseTimer = 0f;

            while (currentThreat == ThreatLevel.Critical)
            {
                // Alternate text
                if (threatLabel != null)
                {
                    showEvac = !showEvac;
                    threatLabel.text = showEvac ? "[EVAC NOW]" : "CRITICAL";
                    threatLabel.color = showEvac ? Color.white : ThreatCritical;
                }

                // Screen pulse every second
                screenPulseTimer += textFlashInterval;
                if (screenPulseTimer >= screenPulseInterval)
                {
                    screenPulseTimer = 0f;

                    // Trigger screen effects if available
                    if (screenEffects != null)
                    {
                        screenEffects.TriggerCriticalPulse();
                    }

                    // Extra container shake
                    if (threatContainer != null)
                    {
                        StartCoroutine(ShakeTransform(threatContainer, 0.1f, 5f));
                    }
                }

                yield return new WaitForSeconds(textFlashInterval);
            }

            // Reset to normal text when leaving critical
            if (threatLabel != null)
            {
                UpdateThreatLabel(currentThreat);
            }
        }

        private IEnumerator ShakeTransform(RectTransform target, float duration, float intensity)
        {
            Vector2 originalPos = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float dampedIntensity = intensity * (1f - elapsed / duration);

                float x = Random.Range(-1f, 1f) * dampedIntensity;
                float y = Random.Range(-1f, 1f) * dampedIntensity;

                target.anchoredPosition = originalPos + new Vector2(x, y);
                yield return null;
            }

            target.anchoredPosition = originalPos;
        }

        private string GetThreatLabelText(ThreatLevel level)
        {
            return level switch
            {
                ThreatLevel.Clear => "CLEAR",
                ThreatLevel.Detected => "DETECTED",
                ThreatLevel.Alerted => "ALERTED",
                ThreatLevel.Swarm => "SWARM INCOMING",
                ThreatLevel.Critical => "CRITICAL",
                _ => "UNKNOWN"
            };
        }

        private Color GetThreatColor(ThreatLevel level)
        {
            return level switch
            {
                ThreatLevel.Clear => ThreatClear,
                ThreatLevel.Detected => ThreatDetected,
                ThreatLevel.Alerted => ThreatAlerted,
                ThreatLevel.Swarm => ThreatSwarm,
                ThreatLevel.Critical => ThreatCritical,
                _ => Color.white
            };
        }

        /// <summary>
        /// Get current threat level
        /// </summary>
        public ThreatLevel GetCurrentThreatLevel() => currentThreat;

        #endregion

        #region Squad Panel

        public void UpdateSquadDisplay(List<SelectableUnit> selectedUnits)
        {
            if (squadFrames == null) return;

            // Hide all frames first
            foreach (var frame in squadFrames)
            {
                frame.gameObject.SetActive(false);
            }

            // Show frames for selected units
            int displayCount = Mathf.Min(selectedUnits?.Count ?? 0, maxSquadDisplay);
            for (int i = 0; i < displayCount; i++)
            {
                if (i < squadFrames.Count && selectedUnits[i] != null)
                {
                    squadFrames[i].gameObject.SetActive(true);
                    squadFrames[i].SetUnit(selectedUnits[i]);
                }
            }
        }

        public void ClearSquadDisplay()
        {
            foreach (var frame in squadFrames)
            {
                frame.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Mission Info

        public void SetMissionInfo(string title, string objective)
        {
            if (missionTitle != null)
                missionTitle.text = title;

            if (objectiveText != null)
                objectiveText.text = objective;
        }

        public void StartMissionTimer()
        {
            missionTime = 0f;
            missionActive = true;
        }

        public void StopMissionTimer()
        {
            missionActive = false;
        }

        public void SetMissionTimer(float time)
        {
            missionTime = time;
            UpdateMissionTimerDisplay();
        }

        private void UpdateMissionTimer()
        {
            missionTime += Time.deltaTime;
            UpdateMissionTimerDisplay();
        }

        private void UpdateMissionTimerDisplay()
        {
            if (missionTimer != null)
            {
                int minutes = Mathf.FloorToInt(missionTime / 60f);
                int seconds = Mathf.FloorToInt(missionTime % 60f);
                missionTimer.text = $"{minutes:00}:{seconds:00}";
            }
        }

        #endregion

        #region Resources

        public void SetAmmo(float current, float max)
        {
            float ratio = max > 0 ? current / max : 0f;

            if (ammoBar != null)
                ammoBar.fillAmount = ratio;

            if (ammoText != null)
                ammoText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }

        public void SetSupply(float current, float max)
        {
            float ratio = max > 0 ? current / max : 0f;

            if (supplyBar != null)
                supplyBar.fillAmount = ratio;

            if (supplyText != null)
                supplyText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }

        #endregion

        #region Alerts

        public void ShowAlert(string message, Sprite icon = null, float duration = 3f)
        {
            if (alertContainer == null) return;

            alertContainer.SetActive(true);

            if (alertText != null)
                alertText.text = message;

            if (alertIcon != null && icon != null)
                alertIcon.sprite = icon;

            // Auto-hide after duration
            CancelInvoke(nameof(HideAlert));
            Invoke(nameof(HideAlert), duration);
        }

        public void HideAlert()
        {
            if (alertContainer != null)
                alertContainer.SetActive(false);
        }

        #endregion

        #region Demo / Testing

        [ContextMenu("Demo: Cycle Threat Levels")]
        public void DemoCycleThreat()
        {
            int next = ((int)currentThreat + 1) % 5;
            SetThreatLevel((ThreatLevel)next);
        }

        [ContextMenu("Demo: Show Alert")]
        public void DemoShowAlert()
        {
            ShowAlert("Enemy patrol detected nearby!", null, 3f);
        }

        #endregion
    }
}
