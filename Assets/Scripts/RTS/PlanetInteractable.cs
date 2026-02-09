using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

namespace EdgeOfUniverse.RTS
{
    public class PlanetInteractable : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlanetData planetData;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Hover")]
        [SerializeField] private float hoverScaleMultiplier = 1.08f;
        [SerializeField] private float hoverScaleDuration = 0.15f;

        [Header("Selection Ring")]
        [SerializeField] private Color ringColor = new Color(0.31f, 0.71f, 0.78f, 0.6f);
        [SerializeField] private float ringRadius = 2f;
        [SerializeField] private float ringWidth = 0.06f;
        [SerializeField] private float ringRotationSpeed = 10f;

        public PlanetData PlanetData => planetData;

        public event Action<PlanetInteractable> OnPlanetHoverEnter;
        public event Action<PlanetInteractable> OnPlanetHoverExit;
        public event Action<PlanetInteractable> OnPlanetSelected;

        private Transform planetMesh;
        private Vector3 originalScale;
        private bool isHovered;
        private bool isSelected;
        private Coroutine scaleCoroutine;

        private GameObject selectionRingObj;
        private Material ringMaterial;
        private Camera mainCamera;

        private void Start()
        {
            planetMesh = transform.Find("Planet");
            if (planetMesh == null) planetMesh = transform;

            originalScale = planetMesh.localScale;
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var mcCam = FindAnyObjectByType<MissionControlCamera>();
                if (mcCam != null) mainCamera = mcCam.Camera;
            }

            CreateSelectionRing();
            SetRingVisible(false);
        }

        private void Update()
        {
            // Self-rotation
            if (planetMesh != null)
            {
                planetMesh.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            // Ring rotation
            if (selectionRingObj != null && selectionRingObj.activeSelf)
            {
                selectionRingObj.transform.Rotate(Vector3.up, ringRotationSpeed * Time.deltaTime, Space.Self);
            }

            // Manual raycasting for hover/click
            HandleInput();
        }

        private void HandleInput()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    var mcCam = FindAnyObjectByType<MissionControlCamera>();
                    if (mcCam != null) mainCamera = mcCam.Camera;
                }
                if (mainCamera == null) return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            var collider = GetComponent<Collider>();
            if (collider == null) return;

            bool hit = collider.Raycast(ray, out _, 2000f);

            if (hit && !isHovered)
            {
                isHovered = true;
                Debug.Log($"[Planet] Hover enter: {planetData?.planetName ?? gameObject.name}");
                OnHoverEnter();
            }
            else if (!hit && isHovered)
            {
                isHovered = false;
                OnHoverExit();
            }

            if (hit && mouse.leftButton.wasPressedThisFrame)
            {
                Debug.Log($"[Planet] Clicked: {planetData?.planetName ?? gameObject.name}");
                OnPlanetSelected?.Invoke(this);
            }
        }

        private void OnHoverEnter()
        {
            if (!isSelected)
            {
                AnimateScale(originalScale * hoverScaleMultiplier);
            }
            OnPlanetHoverEnter?.Invoke(this);
        }

        private void OnHoverExit()
        {
            if (!isSelected)
            {
                AnimateScale(originalScale);
            }
            OnPlanetHoverExit?.Invoke(this);
        }

        public void Select()
        {
            isSelected = true;
            SetRingVisible(true);
            AnimateScale(originalScale * hoverScaleMultiplier);
        }

        public void Deselect()
        {
            isSelected = false;
            SetRingVisible(false);
            if (!isHovered)
            {
                AnimateScale(originalScale);
            }
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale));
        }

        private IEnumerator ScaleAnimation(Vector3 target)
        {
            if (planetMesh == null) yield break;

            Vector3 start = planetMesh.localScale;
            float elapsed = 0f;

            while (elapsed < hoverScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseOutQuad(elapsed / hoverScaleDuration);
                planetMesh.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }

            planetMesh.localScale = target;
        }

        #region Selection Ring

        private void CreateSelectionRing()
        {
            selectionRingObj = new GameObject("SelectionRing");
            selectionRingObj.transform.SetParent(transform);
            selectionRingObj.transform.localPosition = Vector3.zero;
            selectionRingObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            MeshFilter mf = selectionRingObj.AddComponent<MeshFilter>();
            MeshRenderer mr = selectionRingObj.AddComponent<MeshRenderer>();

            mf.mesh = CreateRingMesh(ringRadius, ringRadius - ringWidth, 64);

            ringMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ringMaterial.SetColor("_BaseColor", ringColor);
            ringMaterial.SetFloat("_Surface", 1);
            ringMaterial.SetFloat("_Blend", 0);
            ringMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ringMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ringMaterial.SetInt("_ZWrite", 0);
            ringMaterial.renderQueue = 3000;
            mr.material = ringMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private Mesh CreateRingMesh(float outerRadius, float innerRadius, int segments)
        {
            Mesh mesh = new Mesh();
            int vertexCount = segments * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);

                uvs[i * 2] = new Vector2((float)i / segments, 1f);
                uvs[i * 2 + 1] = new Vector2((float)i / segments, 0f);

                int nextI = (i + 1) % segments;
                int triIndex = i * 6;

                triangles[triIndex] = i * 2;
                triangles[triIndex + 1] = nextI * 2;
                triangles[triIndex + 2] = i * 2 + 1;
                triangles[triIndex + 3] = nextI * 2;
                triangles[triIndex + 4] = nextI * 2 + 1;
                triangles[triIndex + 5] = i * 2 + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void SetRingVisible(bool visible)
        {
            if (selectionRingObj != null)
                selectionRingObj.SetActive(visible);
        }

        #endregion

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
        }
    }
}
