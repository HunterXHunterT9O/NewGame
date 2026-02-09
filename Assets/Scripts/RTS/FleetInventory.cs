using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeOfUniverse.RTS
{
    public class FleetInventory : MonoBehaviour
    {
        public static FleetInventory Instance { get; private set; }

        [SerializeField] private int maxCapacityPerResource = 500;

        private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        public int MaxCapacity => maxCapacityPerResource;

        public event Action<ResourceType, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize all resource types to 0
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
            }
        }

        public int GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public float GetFillPercent(ResourceType type)
        {
            return (float)GetResource(type) / maxCapacityPerResource;
        }

        public int AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return 0;

            int current = GetResource(type);
            int canAdd = Mathf.Min(amount, maxCapacityPerResource - current);
            if (canAdd <= 0) return 0;

            resources[type] = current + canAdd;
            OnResourceChanged?.Invoke(type, resources[type]);
            return canAdd;
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0) return true;

            int current = GetResource(type);
            if (current < amount) return false;

            resources[type] = current - amount;
            OnResourceChanged?.Invoke(type, resources[type]);
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
