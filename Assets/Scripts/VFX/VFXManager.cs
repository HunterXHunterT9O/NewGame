using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EdgeOfUniverse.VFX
{
    /// <summary>
    /// Manages all visual effects for the RTS game.
    /// Handles pooling and spawning of particle effects.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject moveCommandPrefab;
        [SerializeField] private GameObject selectionBurstPrefab;
        [SerializeField] private GameObject dustTrailPrefab;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject bulletImpactPrefab;
        [SerializeField] private GameObject explosionPrefab;

        [Header("Screen Effects")]
        [SerializeField] private float screenShakeIntensity = 0.3f;
        [SerializeField] private float screenShakeDuration = 0.2f;

        // Object pools
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Transform poolContainer;

        // Screen shake
        private Camera mainCam;
        private Vector3 originalCamPos;
        private bool isShaking;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            poolContainer = new GameObject("VFX_Pool").transform;
            poolContainer.SetParent(transform);

            // Find camera
            var camController = FindAnyObjectByType<RTSCameraController>();
            if (camController != null)
            {
                mainCam = camController.Camera;
            }
            else
            {
                mainCam = Camera.main;
            }
        }

        private void Start()
        {
            // Pre-warm pools
            PrewarmPool("MoveCommand", moveCommandPrefab, 5);
            PrewarmPool("SelectionBurst", selectionBurstPrefab, 10);
            PrewarmPool("DustTrail", dustTrailPrefab, 10);
            PrewarmPool("MuzzleFlash", muzzleFlashPrefab, 10);
            PrewarmPool("BulletImpact", bulletImpactPrefab, 20);
            PrewarmPool("Explosion", explosionPrefab, 5);
        }

        #region Effect Spawning

        /// <summary>
        /// Spawn move command indicator at position
        /// </summary>
        public void SpawnMoveCommand(Vector3 position)
        {
            SpawnEffect("MoveCommand", moveCommandPrefab, position, Quaternion.identity, 2f);
        }

        /// <summary>
        /// Spawn selection burst when unit is selected
        /// </summary>
        public void SpawnSelectionBurst(Vector3 position)
        {
            SpawnEffect("SelectionBurst", selectionBurstPrefab, position, Quaternion.identity, 1f);
        }

        /// <summary>
        /// Spawn dust trail at position (for moving units)
        /// </summary>
        public GameObject SpawnDustTrail(Transform parent)
        {
            if (dustTrailPrefab == null) return null;

            var effect = GetFromPool("DustTrail", dustTrailPrefab);
            if (effect != null)
            {
                effect.transform.SetParent(parent);
                effect.transform.localPosition = Vector3.zero;
                effect.SetActive(true);
            }
            return effect;
        }

        /// <summary>
        /// Spawn muzzle flash at weapon position
        /// </summary>
        public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
        {
            SpawnEffect("MuzzleFlash", muzzleFlashPrefab, position, rotation, 0.5f);
        }

        /// <summary>
        /// Spawn bullet impact at hit position
        /// </summary>
        public void SpawnBulletImpact(Vector3 position, Vector3 normal)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            SpawnEffect("BulletImpact", bulletImpactPrefab, position, rotation, 1f);
        }

        /// <summary>
        /// Spawn explosion effect
        /// </summary>
        public void SpawnExplosion(Vector3 position, float scale = 1f)
        {
            var effect = SpawnEffect("Explosion", explosionPrefab, position, Quaternion.identity, 3f);
            if (effect != null)
            {
                effect.transform.localScale = Vector3.one * scale;
            }

            // Screen shake for nearby explosions
            if (mainCam != null)
            {
                float distance = Vector3.Distance(mainCam.transform.position, position);
                if (distance < 50f)
                {
                    float intensity = Mathf.Lerp(screenShakeIntensity, 0f, distance / 50f);
                    ShakeScreen(intensity, screenShakeDuration);
                }
            }
        }

        private GameObject SpawnEffect(string poolName, GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null) return null;

            var effect = GetFromPool(poolName, prefab);
            if (effect != null)
            {
                effect.transform.position = position;
                effect.transform.rotation = rotation;
                effect.transform.SetParent(poolContainer);
                effect.SetActive(true);

                StartCoroutine(ReturnToPoolAfterDelay(poolName, effect, lifetime));
            }
            return effect;
        }

        #endregion

        #region Screen Effects

        /// <summary>
        /// Shake the screen
        /// </summary>
        public void ShakeScreen(float intensity = -1f, float duration = -1f)
        {
            if (mainCam == null) return;

            if (intensity < 0) intensity = screenShakeIntensity;
            if (duration < 0) duration = screenShakeDuration;

            StartCoroutine(ScreenShakeCoroutine(intensity, duration));
        }

        private IEnumerator ScreenShakeCoroutine(float intensity, float duration)
        {
            if (isShaking) yield break;
            isShaking = true;

            Transform camTransform = mainCam.transform;
            Vector3 originalLocalPos = camTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float dampedIntensity = intensity * (1f - (elapsed / duration));

                float x = Random.Range(-1f, 1f) * dampedIntensity;
                float y = Random.Range(-1f, 1f) * dampedIntensity;

                camTransform.localPosition = originalLocalPos + new Vector3(x, y, 0);
                yield return null;
            }

            camTransform.localPosition = originalLocalPos;
            isShaking = false;
        }

        /// <summary>
        /// Flash the screen with a color (for damage, etc.)
        /// </summary>
        public void FlashScreen(Color color, float duration = 0.1f)
        {
            StartCoroutine(ScreenFlashCoroutine(color, duration));
        }

        private IEnumerator ScreenFlashCoroutine(Color color, float duration)
        {
            // This would need a UI overlay image - simplified version
            yield return new WaitForSeconds(duration);
        }

        #endregion

        #region Object Pooling

        private void PrewarmPool(string poolName, GameObject prefab, int count)
        {
            if (prefab == null) return;

            if (!pools.ContainsKey(poolName))
            {
                pools[poolName] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(prefab, poolContainer);
                obj.SetActive(false);
                pools[poolName].Enqueue(obj);
            }
        }

        private GameObject GetFromPool(string poolName, GameObject prefab)
        {
            if (prefab == null) return null;

            if (!pools.ContainsKey(poolName))
            {
                pools[poolName] = new Queue<GameObject>();
            }

            if (pools[poolName].Count > 0)
            {
                return pools[poolName].Dequeue();
            }

            // Create new if pool empty
            return Instantiate(prefab, poolContainer);
        }

        private void ReturnToPool(string poolName, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);

            if (!pools.ContainsKey(poolName))
            {
                pools[poolName] = new Queue<GameObject>();
            }

            pools[poolName].Enqueue(obj);
        }

        private IEnumerator ReturnToPoolAfterDelay(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(poolName, obj);
        }

        #endregion
    }
}
