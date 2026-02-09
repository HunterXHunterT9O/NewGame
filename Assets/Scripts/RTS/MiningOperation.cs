using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EdgeOfUniverse.RTS
{
    [System.Serializable]
    public struct MiningResult
    {
        public Dictionary<ResourceType, int> extracted;
        public int lost;
        public float duration;
        public int dangerLevel;
        public string planetName;
    }

    public class MiningOperation : MonoBehaviour
    {
        [Header("Mining Settings")]
        [SerializeField] private float miningDuration = 15f;
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] [Range(0.3f, 0.5f)] private float yieldPercent = 0.4f;

        public bool IsMining { get; private set; }

        public event Action<float> OnMiningProgress;
        public event Action<MiningResult> OnMiningComplete;

        private Coroutine miningCoroutine;

        public void StartMining(PlanetInteractable planet)
        {
            if (IsMining || planet == null || planet.PlanetData == null) return;

            Debug.Log($"[Mining] Starting mining on {planet.PlanetData.planetName}");
            miningCoroutine = StartCoroutine(MiningCoroutine(planet));
        }

        public void CancelMining()
        {
            if (miningCoroutine != null)
            {
                StopCoroutine(miningCoroutine);
                miningCoroutine = null;
            }
            IsMining = false;
        }

        private IEnumerator MiningCoroutine(PlanetInteractable planet)
        {
            IsMining = true;
            var data = planet.PlanetData;

            // Apply regeneration before mining
            ApplyRegeneration(data);

            int totalTicks = Mathf.FloorToInt(miningDuration / tickInterval);
            if (totalTicks <= 0) totalTicks = 1;

            // Calculate base yield per resource
            Dictionary<ResourceType, int> totalYield = new Dictionary<ResourceType, int>();
            Dictionary<ResourceType, int> perTickYield = new Dictionary<ResourceType, int>();

            if (data.resources != null)
            {
                foreach (var deposit in data.resources)
                {
                    int available = data.GetAvailableAmount(deposit.type);
                    int baseYield = Mathf.RoundToInt(available * yieldPercent);
                    totalYield[deposit.type] = 0;
                    perTickYield[deposit.type] = Mathf.Max(1, Mathf.CeilToInt((float)baseYield / totalTicks));
                }
            }

            int totalLost = 0;
            float elapsed = 0f;

            // Mining loop
            for (int tick = 0; tick < totalTicks; tick++)
            {
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;

                // Danger roll — higher danger = higher chance of losing this tick
                bool hostileInterference = UnityEngine.Random.value < data.dangerLevel * 0.05f;

                if (hostileInterference)
                {
                    // Lose this tick's yield
                    int tickLoss = 0;
                    foreach (var kvp in perTickYield)
                        tickLoss += kvp.Value;
                    totalLost += tickLoss;
                    Debug.Log($"[Mining] Hostile interference! Lost {tickLoss} resources this tick.");
                }
                else
                {
                    // Extract resources
                    foreach (var kvp in perTickYield)
                    {
                        ResourceType type = kvp.Key;
                        int amount = kvp.Value;

                        // Check we haven't exceeded the target yield
                        int available = data.GetAvailableAmount(type);
                        int maxYield = Mathf.RoundToInt(available * yieldPercent);
                        int remaining = maxYield - totalYield[type];
                        int toExtract = Mathf.Min(amount, remaining);

                        if (toExtract > 0 && FleetInventory.Instance != null)
                        {
                            int added = FleetInventory.Instance.AddResource(type, toExtract);
                            totalYield[type] += added;
                        }
                    }
                }

                float progress = elapsed / miningDuration;
                OnMiningProgress?.Invoke(Mathf.Clamp01(progress));
            }

            // Update depletion
            int totalExtracted = 0;
            foreach (var kvp in totalYield)
                totalExtracted += kvp.Value;

            data.depletedAmount += totalExtracted;
            data.lastMinedTime = Time.time;

            // Build result
            MiningResult result = new MiningResult
            {
                extracted = totalYield,
                lost = totalLost,
                duration = elapsed,
                dangerLevel = data.dangerLevel,
                planetName = data.planetName
            };

            IsMining = false;
            miningCoroutine = null;

            Debug.Log($"[Mining] Complete on {data.planetName}. Extracted {totalExtracted}, Lost {totalLost}");
            OnMiningComplete?.Invoke(result);
        }

        private void ApplyRegeneration(PlanetData data)
        {
            if (data.depletedAmount <= 0 || data.lastMinedTime <= 0f) return;

            float timeSinceLastMine = Time.time - data.lastMinedTime;
            int regenerated = Mathf.FloorToInt(timeSinceLastMine * data.regenRate);
            data.depletedAmount = Mathf.Max(0, data.depletedAmount - regenerated);
        }
    }
}
