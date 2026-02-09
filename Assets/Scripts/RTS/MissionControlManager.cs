using UnityEngine;
using System;
using System.Collections;

namespace EdgeOfUniverse.RTS
{
    public enum DropshipState
    {
        Idle,
        Launching,
        Traveling,
        Arriving,
        Mining
    }

    public class MissionControlManager : MonoBehaviour
    {
        public static MissionControlManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DropshipController dropship;
        [SerializeField] private MissionControlCamera missionCamera;
        [SerializeField] private PlanetInteractable[] planets;
        [SerializeField] private MiningOperation miningOperation;
        [SerializeField] private FleetInventory fleetInventory;

        [Header("State")]
        [SerializeField] private int currentPlanetIndex;

        private PlanetInteractable selectedPlanet;
        private PlanetInteractable targetPlanet;
        private DropshipState dropshipState = DropshipState.Idle;

        public PlanetInteractable SelectedPlanet => selectedPlanet;
        public PlanetInteractable CurrentPlanet => currentPlanetIndex >= 0 && currentPlanetIndex < planets.Length ? planets[currentPlanetIndex] : null;
        public DropshipState CurrentState => dropshipState;

        public event Action<PlanetInteractable> OnPlanetSelected;
        public event Action OnPlanetDeselected;
        public event Action<DropshipState> OnDropshipStateChanged;
        public event Action<float> OnTravelProgress;
        public event Action OnMiningStarted;
        public event Action<float> OnMiningProgress;
        public event Action<MiningResult> OnMiningComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Debug.Log($"[MissionControl] Start - dropship: {(dropship != null ? "FOUND" : "NULL")}, camera: {(missionCamera != null ? "FOUND" : "NULL")}, planets: {(planets != null ? planets.Length.ToString() : "NULL")}");

            if (planets != null)
            {
                foreach (var planet in planets)
                {
                    if (planet != null)
                    {
                        planet.OnPlanetSelected += HandlePlanetClicked;
                    }
                }
            }

            if (miningOperation != null)
            {
                miningOperation.OnMiningProgress += HandleMiningProgress;
                miningOperation.OnMiningComplete += HandleMiningComplete;
            }
        }

        private void HandlePlanetClicked(PlanetInteractable planet)
        {
            Debug.Log($"[MissionControl] Planet clicked: {planet.PlanetData?.planetName ?? "unknown"}");
            if (dropshipState != DropshipState.Idle && dropshipState != DropshipState.Mining) return;
            if (dropshipState == DropshipState.Mining) return;

            if (selectedPlanet == planet)
            {
                DeselectPlanet();
                return;
            }

            SelectPlanet(planet);
        }

        public void SelectPlanet(PlanetInteractable planet)
        {
            if (selectedPlanet != null)
            {
                selectedPlanet.Deselect();
            }

            selectedPlanet = planet;
            selectedPlanet.Select();

            if (missionCamera != null)
            {
                missionCamera.FocusOnPlanet(planet.transform);
            }

            OnPlanetSelected?.Invoke(planet);
        }

        public void DeselectPlanet()
        {
            if (selectedPlanet != null)
            {
                selectedPlanet.Deselect();
                selectedPlanet = null;
            }

            if (missionCamera != null)
            {
                missionCamera.ReturnToOverview();
            }

            OnPlanetDeselected?.Invoke();
        }

        public bool CanDeploy()
        {
            if (selectedPlanet == null) return false;
            if (dropshipState != DropshipState.Idle) return false;
            if (CurrentPlanet != null && selectedPlanet == CurrentPlanet) return false;
            return true;
        }

        public void DeployDropship()
        {
            if (!CanDeploy())
            {
                Debug.LogWarning($"[MissionControl] CanDeploy=false. selected={selectedPlanet != null}, state={dropshipState}, dropship={dropship != null}");
                return;
            }

            Debug.Log($"[MissionControl] Deploying to {selectedPlanet.PlanetData.planetName}");
            targetPlanet = selectedPlanet;
            StartCoroutine(TravelSequence());
        }

        private IEnumerator TravelSequence()
        {
            // Phase 1: Launch
            SetDropshipState(DropshipState.Launching);

            if (missionCamera != null)
            {
                missionCamera.ReturnToOverview();
            }

            yield return new WaitForSeconds(0.3f);

            // Face the target before launching
            dropship.FaceTarget(targetPlanet.transform.position);

            // Subscribe, launch, wait, unsubscribe
            bool launchDone = false;
            System.Action onLaunch = () => launchDone = true;
            dropship.OnLaunchComplete += onLaunch;
            dropship.LaunchSequence();
            while (!launchDone) yield return null;
            dropship.OnLaunchComplete -= onLaunch;

            // Phase 2: Travel
            SetDropshipState(DropshipState.Traveling);

            if (missionCamera != null)
            {
                missionCamera.FollowDropship(dropship.transform, targetPlanet.transform);
            }

            bool travelDone = false;
            dropship.TravelTo(targetPlanet.transform, progress =>
            {
                OnTravelProgress?.Invoke(progress);
                if (progress >= 1f) travelDone = true;
            });

            while (!travelDone) yield return null;

            // Phase 3: Arrival
            SetDropshipState(DropshipState.Arriving);

            if (missionCamera != null)
            {
                missionCamera.StopFollowing();
                missionCamera.FocusOnPlanet(targetPlanet.transform);
            }

            bool arrivalDone = false;
            System.Action onArrival = () => arrivalDone = true;
            dropship.OnArrivalComplete += onArrival;
            dropship.ArrivalSequence();
            while (!arrivalDone) yield return null;
            dropship.OnArrivalComplete -= onArrival;

            // Update current planet
            for (int i = 0; i < planets.Length; i++)
            {
                if (planets[i] == targetPlanet)
                {
                    currentPlanetIndex = i;
                    break;
                }
            }

            // Start mining on the target planet
            if (miningOperation != null && targetPlanet != null)
            {
                SetDropshipState(DropshipState.Mining);
                OnMiningStarted?.Invoke();
                miningOperation.StartMining(targetPlanet);
            }
            else
            {
                targetPlanet = null;
                SetDropshipState(DropshipState.Idle);
            }
        }

        private void HandleMiningProgress(float progress)
        {
            OnMiningProgress?.Invoke(progress);
        }

        private void HandleMiningComplete(MiningResult result)
        {
            Debug.Log($"[MissionControl] Mining complete on {result.planetName}");
            targetPlanet = null;
            OnMiningComplete?.Invoke(result);
            // State will be set to Idle when the results UI is dismissed
            // (via OnMiningResultsDismissed called from UI)
        }

        public void OnMiningResultsDismissed()
        {
            SetDropshipState(DropshipState.Idle);
        }

        private void SetDropshipState(DropshipState state)
        {
            dropshipState = state;
            OnDropshipStateChanged?.Invoke(state);
        }

        private void OnDestroy()
        {
            if (planets != null)
            {
                foreach (var planet in planets)
                {
                    if (planet != null)
                    {
                        planet.OnPlanetSelected -= HandlePlanetClicked;
                    }
                }
            }

            if (miningOperation != null)
            {
                miningOperation.OnMiningProgress -= HandleMiningProgress;
                miningOperation.OnMiningComplete -= HandleMiningComplete;
            }

            if (Instance == this)
                Instance = null;
        }
    }
}
