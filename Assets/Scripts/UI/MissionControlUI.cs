using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using EdgeOfUniverse.RTS;

namespace EdgeOfUniverse.UI
{
    public class MissionControlUI : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI missionCountText;
        [SerializeField] private TextMeshProUGUI resourceSummaryText;

        [Header("Fleet Cargo Display")]
        [SerializeField] private TextMeshProUGUI fleetMineralsText;
        [SerializeField] private TextMeshProUGUI fleetFuelText;
        [SerializeField] private TextMeshProUGUI fleetBioMatterText;
        [SerializeField] private TextMeshProUGUI fleetTechSalvageText;

        [Header("Planet Info Panel")]
        [SerializeField] private RectTransform planetInfoPanel;
        [SerializeField] private CanvasGroup planetInfoGroup;
        [SerializeField] private TextMeshProUGUI planetNameText;
        [SerializeField] private Image[] dangerDiamonds;
        [SerializeField] private Image dangerBar;
        [SerializeField] private TextMeshProUGUI missionBriefText;

        [Header("Resource Bars")]
        [SerializeField] private Image mineralsBar;
        [SerializeField] private TextMeshProUGUI mineralsText;
        [SerializeField] private Image fuelBar;
        [SerializeField] private TextMeshProUGUI fuelText;
        [SerializeField] private Image bioMatterBar;
        [SerializeField] private TextMeshProUGUI bioMatterText;
        [SerializeField] private Image techSalvageBar;
        [SerializeField] private TextMeshProUGUI techSalvageText;

        [Header("Deploy Button")]
        [SerializeField] private Button deployButton;
        [SerializeField] private TextMeshProUGUI deployButtonText;

        [Header("Confirmation Overlay")]
        [SerializeField] private RectTransform confirmPanel;
        [SerializeField] private CanvasGroup confirmGroup;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private TextMeshProUGUI confirmText;

        [Header("Bottom Bar")]
        [SerializeField] private TextMeshProUGUI dropshipStatusText;
        [SerializeField] private Image travelProgressBar;
        [SerializeField] private TextMeshProUGUI travelProgressText;

        [Header("Mining Results")]
        [SerializeField] private MiningResultsUI miningResultsUI;

        [Header("Scanline Overlay")]
        [SerializeField] private RawImage scanlineOverlay;

        // Danger colors (matching MissionHUD threat palette)
        private static readonly Color DangerGreen = new Color(0.22f, 0.63f, 0.29f);
        private static readonly Color DangerAmber = new Color(0.78f, 0.55f, 0.16f);
        private static readonly Color DangerOrange = new Color(0.90f, 0.47f, 0.12f);
        private static readonly Color DangerRed = new Color(0.71f, 0.18f, 0.18f);
        private static readonly Color DangerCritical = new Color(1f, 0.12f, 0.12f);
        private static readonly Color CyanPrimary = new Color(0.31f, 0.71f, 0.78f);

        private MissionControlManager manager;
        private Coroutine typewriterCoroutine;
        private Coroutine pulseCoroutine;
        private Coroutine dangerPulseCoroutine;

        private void Start()
        {
            manager = MissionControlManager.Instance;
            if (manager == null)
            {
                manager = FindAnyObjectByType<MissionControlManager>();
            }

            Debug.Log($"[MissionControlUI] Start - manager: {(manager != null ? "FOUND" : "NULL")}");

            if (manager != null)
            {
                manager.OnPlanetSelected += ShowPlanetInfo;
                manager.OnPlanetDeselected += HidePlanetInfo;
                manager.OnDropshipStateChanged += UpdateDropshipStatus;
                manager.OnTravelProgress += UpdateTravelProgress;
                manager.OnMiningProgress += UpdateMiningProgress;
                manager.OnMiningComplete += ShowMiningResults;
            }

            if (deployButton != null)
                deployButton.onClick.AddListener(OnDeployClicked);

            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmDeploy);

            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnCancelDeploy);

            if (miningResultsUI != null)
                miningResultsUI.OnContinue += OnMiningResultsDismissed;

            // Subscribe to fleet inventory changes
            if (FleetInventory.Instance != null)
            {
                FleetInventory.Instance.OnResourceChanged += OnFleetResourceChanged;
            }

            // Initial state
            HidePlanetInfo();
            HideConfirmation();
            UpdateDropshipStatus(DropshipState.Idle);
            UpdateTravelProgress(0f);
            UpdateFleetCargoDisplay();
            AnimateScanlines();

            // Set initial location
            if (locationText != null && manager != null && manager.CurrentPlanet != null)
            {
                locationText.text = $"// LOCATION: {manager.CurrentPlanet.PlanetData.planetName.ToUpper()}";
            }
        }

        private void ShowPlanetInfo(PlanetInteractable planet)
        {
            if (planet == null || planet.PlanetData == null) return;

            var data = planet.PlanetData;

            // Slide in panel
            if (planetInfoPanel != null && planetInfoGroup != null)
            {
                StartCoroutine(UIAnimationUtility.SlideIn(
                    planetInfoPanel, planetInfoGroup,
                    new Vector2(300f, 0f), 0.25f));
            }

            // Typewriter planet name
            if (planetNameText != null)
            {
                if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = StartCoroutine(UIAnimationUtility.Typewriter(planetNameText, data.planetName.ToUpper()));
            }

            // Danger display
            UpdateDangerDisplay(data.dangerLevel);

            // Resources with staggered fill (using available amounts)
            StartCoroutine(FillResourceBars(data));

            // Mission brief
            if (missionBriefText != null)
                missionBriefText.text = data.missionBriefing;

            // Deploy button state
            UpdateDeployButton();
        }

        private void HidePlanetInfo()
        {
            if (planetInfoPanel != null && planetInfoGroup != null)
            {
                StartCoroutine(UIAnimationUtility.SlideOut(
                    planetInfoPanel, planetInfoGroup,
                    new Vector2(300f, 0f), 0.15f));
            }

            StopDeployPulse();
        }

        private void UpdateDangerDisplay(int level)
        {
            Color dangerColor = GetDangerColor(level);

            if (dangerDiamonds != null)
            {
                for (int i = 0; i < dangerDiamonds.Length; i++)
                {
                    if (dangerDiamonds[i] != null)
                    {
                        dangerDiamonds[i].color = i < level ? dangerColor : new Color(0.25f, 0.28f, 0.32f, 0.5f);
                    }
                }
            }

            if (dangerBar != null)
            {
                dangerBar.fillAmount = level / 5f;
                dangerBar.color = dangerColor;
            }

            if (level >= 5)
            {
                if (dangerPulseCoroutine != null) StopCoroutine(dangerPulseCoroutine);
                dangerPulseCoroutine = StartCoroutine(DangerPulse());
            }
            else
            {
                if (dangerPulseCoroutine != null)
                {
                    StopCoroutine(dangerPulseCoroutine);
                    dangerPulseCoroutine = null;
                }
            }
        }

        private IEnumerator DangerPulse()
        {
            while (true)
            {
                if (dangerBar != null)
                {
                    float alpha = 0.6f + Mathf.Sin(Time.time * 4f) * 0.4f;
                    Color c = DangerCritical;
                    c.a = alpha;
                    dangerBar.color = c;
                }
                yield return null;
            }
        }

        private Color GetDangerColor(int level)
        {
            switch (level)
            {
                case 1: return DangerGreen;
                case 2: return DangerAmber;
                case 3: return DangerOrange;
                case 4: return DangerRed;
                case 5: return DangerCritical;
                default: return DangerGreen;
            }
        }

        private IEnumerator FillResourceBars(PlanetData data)
        {
            SetResourceBar(mineralsBar, mineralsText, 0, "Minerals");
            SetResourceBar(fuelBar, fuelText, 0, "Fuel");
            SetResourceBar(bioMatterBar, bioMatterText, 0, "BioMatter");
            SetResourceBar(techSalvageBar, techSalvageText, 0, "TechSalvage");

            if (data.resources == null) yield break;

            // Use available amounts (accounting for depletion/regen)
            int minerals = data.GetAvailableAmount(ResourceType.Minerals);
            int fuel = data.GetAvailableAmount(ResourceType.Fuel);
            int bio = data.GetAvailableAmount(ResourceType.BioMatter);
            int tech = data.GetAvailableAmount(ResourceType.TechSalvage);

            yield return StartCoroutine(AnimateBar(mineralsBar, mineralsText, minerals, "Minerals"));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(AnimateBar(fuelBar, fuelText, fuel, "Fuel"));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(AnimateBar(bioMatterBar, bioMatterText, bio, "BioMatter"));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(AnimateBar(techSalvageBar, techSalvageText, tech, "TechSalvage"));
        }

        private IEnumerator AnimateBar(Image bar, TextMeshProUGUI text, int targetValue, string label)
        {
            if (bar == null) yield break;

            float duration = 0.3f;
            float elapsed = 0f;
            float targetFill = targetValue / 100f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = UIAnimationUtility.EaseOutQuad(elapsed / duration);
                float currentFill = Mathf.Lerp(0f, targetFill, t);
                bar.fillAmount = currentFill;

                if (text != null)
                    text.text = $"{Mathf.RoundToInt(currentFill * 100)}";

                yield return null;
            }

            bar.fillAmount = targetFill;
            if (text != null)
                text.text = $"{targetValue}";
        }

        private void SetResourceBar(Image bar, TextMeshProUGUI text, int value, string label)
        {
            if (bar != null) bar.fillAmount = value / 100f;
            if (text != null) text.text = $"{value}";
        }

        // ======== Fleet Cargo Display ========

        private void UpdateFleetCargoDisplay()
        {
            var fleet = FleetInventory.Instance;
            if (fleet == null) return;

            if (fleetMineralsText != null)
                fleetMineralsText.text = fleet.GetResource(ResourceType.Minerals).ToString();
            if (fleetFuelText != null)
                fleetFuelText.text = fleet.GetResource(ResourceType.Fuel).ToString();
            if (fleetBioMatterText != null)
                fleetBioMatterText.text = fleet.GetResource(ResourceType.BioMatter).ToString();
            if (fleetTechSalvageText != null)
                fleetTechSalvageText.text = fleet.GetResource(ResourceType.TechSalvage).ToString();

            // Update resource summary in top bar
            if (resourceSummaryText != null)
            {
                int total = fleet.GetResource(ResourceType.Minerals) +
                            fleet.GetResource(ResourceType.Fuel) +
                            fleet.GetResource(ResourceType.BioMatter) +
                            fleet.GetResource(ResourceType.TechSalvage);
                resourceSummaryText.text = $"FLEET CARGO: {total}";
            }
        }

        private void OnFleetResourceChanged(ResourceType type, int newAmount)
        {
            UpdateFleetCargoDisplay();
        }

        // ======== Deploy Button ========

        private void UpdateDeployButton()
        {
            if (deployButton == null) return;

            bool canDeploy = manager != null && manager.CanDeploy();
            deployButton.interactable = canDeploy;

            if (deployButtonText != null)
                deployButtonText.text = canDeploy ? "DEPLOY DROPSHIP" : "CURRENT LOCATION";

            if (canDeploy)
            {
                StartDeployPulse();
            }
            else
            {
                StopDeployPulse();
            }
        }

        private void StartDeployPulse()
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(DeployButtonPulse());
        }

        private void StopDeployPulse()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator DeployButtonPulse()
        {
            if (deployButton == null) yield break;

            var colors = deployButton.colors;
            Color baseColor = CyanPrimary;

            while (true)
            {
                float glow = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
                colors.normalColor = baseColor * glow;
                deployButton.colors = colors;
                yield return null;
            }
        }

        private void OnDeployClicked()
        {
            if (manager == null || !manager.CanDeploy()) return;

            if (deployButton != null)
                StartCoroutine(UIAnimationUtility.ClickPunch(deployButton.transform));

            ShowConfirmation();
        }

        private void ShowConfirmation()
        {
            if (confirmPanel == null || confirmGroup == null) return;

            if (manager != null && manager.SelectedPlanet != null && confirmText != null)
            {
                confirmText.text = $"Deploy dropship to\n{manager.SelectedPlanet.PlanetData.planetName}?";
            }

            StartCoroutine(UIAnimationUtility.PopIn(confirmPanel, confirmGroup));
        }

        private void HideConfirmation()
        {
            if (confirmGroup != null)
            {
                confirmGroup.alpha = 0f;
                confirmGroup.interactable = false;
                confirmGroup.blocksRaycasts = false;
                if (confirmPanel != null)
                    confirmPanel.gameObject.SetActive(false);
            }
        }

        private void OnConfirmDeploy()
        {
            HideConfirmation();
            if (manager != null)
            {
                manager.DeployDropship();
            }
        }

        private void OnCancelDeploy()
        {
            if (confirmGroup != null)
            {
                StartCoroutine(UIAnimationUtility.FadeOut(confirmGroup, 0.15f, true));
            }
        }

        // ======== Dropship & Mining Status ========

        private void UpdateDropshipStatus(DropshipState state)
        {
            if (dropshipStatusText == null) return;

            string statusText;
            switch (state)
            {
                case DropshipState.Idle: statusText = "STANDBY"; break;
                case DropshipState.Launching: statusText = "LAUNCHING"; break;
                case DropshipState.Traveling: statusText = "IN TRANSIT"; break;
                case DropshipState.Arriving: statusText = "ARRIVING"; break;
                case DropshipState.Mining: statusText = "MINING"; break;
                default: statusText = "UNKNOWN"; break;
            }

            dropshipStatusText.text = $"Dropship: {statusText}";
            StartCoroutine(UIAnimationUtility.TextFlash(dropshipStatusText));

            if (state == DropshipState.Idle)
            {
                UpdateDeployButton();

                if (locationText != null && manager != null && manager.CurrentPlanet != null)
                {
                    locationText.text = $"// LOCATION: {manager.CurrentPlanet.PlanetData.planetName.ToUpper()}";
                }

                // Reset progress bar
                UpdateTravelProgress(0f);
            }
            else if (state == DropshipState.Mining)
            {
                if (deployButton != null)
                    deployButton.interactable = false;
                if (deployButtonText != null)
                    deployButtonText.text = "MINING IN PROGRESS";
                StopDeployPulse();

                // Reset progress bar for mining
                UpdateTravelProgress(0f);
                if (travelProgressText != null)
                    travelProgressText.text = "MINING: 0%";
            }
            else
            {
                if (deployButton != null)
                    deployButton.interactable = false;
                StopDeployPulse();
            }
        }

        private void UpdateTravelProgress(float progress)
        {
            if (travelProgressBar != null)
                travelProgressBar.fillAmount = progress;

            if (travelProgressText != null)
                travelProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        private void UpdateMiningProgress(float progress)
        {
            if (travelProgressBar != null)
                travelProgressBar.fillAmount = progress;

            if (travelProgressText != null)
                travelProgressText.text = $"MINING: {Mathf.RoundToInt(progress * 100)}%";
        }

        private void ShowMiningResults(MiningResult result)
        {
            if (miningResultsUI != null)
            {
                miningResultsUI.Show(result);
            }
            else
            {
                // No results UI — just dismiss immediately
                OnMiningResultsDismissed();
            }
        }

        private void OnMiningResultsDismissed()
        {
            if (manager != null)
            {
                manager.OnMiningResultsDismissed();
            }

            // Refresh fleet cargo display
            UpdateFleetCargoDisplay();

            // Refresh planet info if still selected (show reduced resources)
            if (manager != null && manager.SelectedPlanet != null)
            {
                StartCoroutine(FillResourceBars(manager.SelectedPlanet.PlanetData));
            }
        }

        // ======== Scanlines ========

        private void AnimateScanlines()
        {
            if (scanlineOverlay != null)
            {
                StartCoroutine(ScrollScanlines());
            }
        }

        private IEnumerator ScrollScanlines()
        {
            float scrollSpeed = 0.03f;
            Rect uvRect = scanlineOverlay.uvRect;
            float flickerTimer = 0f;

            while (true)
            {
                uvRect.y += scrollSpeed * Time.deltaTime;
                scanlineOverlay.uvRect = uvRect;

                flickerTimer += Time.deltaTime;
                if (flickerTimer > UnityEngine.Random.Range(4f, 12f))
                {
                    flickerTimer = 0f;
                    StartCoroutine(HolographicFlicker());
                }

                yield return null;
            }
        }

        private IEnumerator HolographicFlicker()
        {
            if (scanlineOverlay == null) yield break;

            Color original = scanlineOverlay.color;
            scanlineOverlay.color = new Color(1, 1, 1, 0.12f);
            yield return new WaitForSeconds(0.04f);
            scanlineOverlay.color = new Color(1, 1, 1, 0.01f);
            yield return new WaitForSeconds(0.06f);
            scanlineOverlay.color = new Color(1, 1, 1, 0.08f);
            yield return new WaitForSeconds(0.03f);
            scanlineOverlay.color = original;
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnPlanetSelected -= ShowPlanetInfo;
                manager.OnPlanetDeselected -= HidePlanetInfo;
                manager.OnDropshipStateChanged -= UpdateDropshipStatus;
                manager.OnTravelProgress -= UpdateTravelProgress;
                manager.OnMiningProgress -= UpdateMiningProgress;
                manager.OnMiningComplete -= ShowMiningResults;
            }

            if (FleetInventory.Instance != null)
            {
                FleetInventory.Instance.OnResourceChanged -= OnFleetResourceChanged;
            }

            if (miningResultsUI != null)
            {
                miningResultsUI.OnContinue -= OnMiningResultsDismissed;
            }
        }
    }
}
