using UnityEngine;

namespace EdgeOfUniverse.RTS
{
    public enum ResourceType
    {
        Minerals,
        Fuel,
        BioMatter,
        TechSalvage
    }

    public enum MissionStatus
    {
        Undiscovered,
        Available,
        InProgress,
        Completed,
        Failed
    }

    public enum RingType
    {
        None,
        StarRings,
        StarRings3,
        StarRings4
    }

    public enum CloudDensity
    {
        None,
        Light,
        Average,
        Heavy
    }

    [System.Serializable]
    public class ResourceDeposit
    {
        public ResourceType type;
        [Range(0, 100)] public int amount;
    }

    [CreateAssetMenu(fileName = "NewPlanet", menuName = "Edge of Universe/Planet Data")]
    public class PlanetData : ScriptableObject
    {
        [Header("Identity")]
        public string planetName;
        [TextArea(2, 4)] public string description;
        [TextArea(2, 6)] public string missionBriefing;

        [Header("Danger")]
        [Range(1, 5)] public int dangerLevel = 1;
        public MissionStatus missionStatus = MissionStatus.Available;

        [Header("Resources")]
        public ResourceDeposit[] resources;
        [Tooltip("Units regenerated per second after mining")]
        public float regenRate = 0.5f;

        [Header("Visual Config")]
        public string primaryTexture;
        public string secondaryTexture;
        public CloudDensity cloudDensity = CloudDensity.None;
        public RingType ringType = RingType.None;
        public Color atmosphereColor = new Color(0.3f, 0.6f, 1f, 0.15f);
        public float planetScale = 1f;

        [Header("Map")]
        public Vector3 mapPosition;

        // Runtime depletion tracking (not serialized to asset)
        [System.NonSerialized] public float lastMinedTime;
        [System.NonSerialized] public int depletedAmount;

        /// <summary>
        /// Returns the available amount for a resource, accounting for depletion and regeneration.
        /// </summary>
        public int GetAvailableAmount(ResourceType type)
        {
            if (resources == null) return 0;

            int originalAmount = 0;
            foreach (var res in resources)
            {
                if (res.type == type)
                {
                    originalAmount = res.amount;
                    break;
                }
            }

            if (depletedAmount <= 0) return originalAmount;

            // Calculate regeneration since last mining
            float timeSinceLastMine = Time.time - lastMinedTime;
            int regenerated = Mathf.FloorToInt(timeSinceLastMine * regenRate);
            int currentDepletion = Mathf.Max(0, depletedAmount - regenerated);

            return Mathf.Max(0, originalAmount - currentDepletion);
        }
    }
}
