using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

namespace EdgeOfUniverse.RTS
{
    public class MissionControlCamera : MonoBehaviour
    {
        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 60f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minZoom = 20f;
        [SerializeField] private float maxZoom = 200f;
        [SerializeField] private float zoomSmoothTime = 0.1f;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitSpeed = 80f;

        [Header("Focus Transition")]
        [SerializeField] private float focusDuration = 0.6f;

        private Transform pivot;
        private Camera cam;
        private float targetZoomHeight;
        private float zoomVelocity;
        private bool isOrbiting;

        private Vector3 defaultPosition;
        private Quaternion defaultRotation;
        private float defaultZoom;

        private Coroutine transitionCoroutine;

        public Camera Camera => cam;

        public event Action<Vector3> OnCameraMove;

        private void Awake()
        {
            SetupCameraRig();
        }

        private void Start()
        {
            targetZoomHeight = cam.transform.localPosition.y;
            defaultPosition = transform.position;
            defaultRotation = transform.rotation;
            defaultZoom = targetZoomHeight;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleOrbit();
        }

        private void SetupCameraRig()
        {
            pivot = transform.Find("Pivot");
            if (pivot == null)
            {
                GameObject pivotObj = new GameObject("Pivot");
                pivotObj.transform.SetParent(transform);
                pivotObj.transform.localPosition = Vector3.zero;
                pivotObj.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
                pivot = pivotObj.transform;
            }

            cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                GameObject camObj = new GameObject("StrategicCamera");
                camObj.transform.SetParent(pivot);
                camObj.transform.localPosition = new Vector3(0f, 100f, -100f);
                camObj.transform.localRotation = Quaternion.identity;
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }
            else
            {
                if (cam.transform.parent != pivot)
                    cam.transform.SetParent(pivot);
            }
        }

        private void HandlePan()
        {
            Vector3 moveDir = Vector3.zero;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                moveDir += GetForward();
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                moveDir -= GetForward();
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                moveDir += GetRight();
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                moveDir -= GetRight();

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                float zoomFactor = Mathf.Lerp(0.5f, 2f, (targetZoomHeight - minZoom) / (maxZoom - minZoom));
                transform.position += moveDir * panSpeed * zoomFactor * Time.deltaTime;
                OnCameraMove?.Invoke(transform.position);
            }
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y / 120f : 0f;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetZoomHeight -= scroll * zoomSpeed;
                targetZoomHeight = Mathf.Clamp(targetZoomHeight, minZoom, maxZoom);
            }

            Vector3 camLocalPos = cam.transform.localPosition;
            float currentHeight = camLocalPos.y;
            if (Mathf.Abs(currentHeight) < 0.01f) return;

            float newHeight = Mathf.SmoothDamp(currentHeight, targetZoomHeight, ref zoomVelocity, zoomSmoothTime);
            float ratio = newHeight / currentHeight;
            cam.transform.localPosition = new Vector3(camLocalPos.x, newHeight, camLocalPos.z * ratio);
        }

        private void HandleOrbit()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
                isOrbiting = true;
            if (mouse.rightButton.wasReleasedThisFrame)
                isOrbiting = false;

            if (isOrbiting)
            {
                float rotateInput = mouse.delta.ReadValue().x * 0.1f;
                transform.Rotate(Vector3.up, rotateInput * orbitSpeed * Time.deltaTime, Space.World);
            }
        }

        private Vector3 GetForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.normalized;
        }

        private Vector3 GetRight()
        {
            Vector3 right = transform.right;
            right.y = 0f;
            return right.normalized;
        }

        public void FocusOnPlanet(Transform planet)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FocusTransition(planet.position));
        }

        public void ReturnToOverview()
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(ReturnTransition());
        }

        /// <summary>
        /// Smooth transition to frame both the dropship and its destination,
        /// then hands full camera control back to the player.
        /// </summary>
        public void FollowDropship(Transform dropship, Transform destination)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FrameTravelTransition(dropship, destination));
        }

        public void StopFollowing()
        {
            // No-op now — camera is already freeform after the initial transition
        }

        private IEnumerator FrameTravelTransition(Transform dropship, Transform destination)
        {
            // Frame the midpoint between dropship and destination
            Vector3 midpoint = dropship.position;
            if (destination != null)
                midpoint = (dropship.position + destination.position) * 0.5f;

            float dist = destination != null ? Vector3.Distance(dropship.position, destination.position) : 100f;
            // Zoom out enough to see both — clamp within bounds
            float endZoom = Mathf.Clamp(dist * 0.4f, minZoom + 10f, maxZoom - 20f);

            Vector3 rigBack = -transform.forward;
            rigBack.y = 0f;
            if (rigBack.sqrMagnitude < 0.01f) rigBack = -Vector3.forward;
            rigBack.Normalize();

            Vector3 endPos = midpoint + rigBack * endZoom * 0.5f;
            endPos.y = midpoint.y;

            Vector3 startPos = transform.position;
            float startZoom = targetZoomHeight;
            float elapsed = 0f;

            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseInOutCubic(elapsed / focusDuration);

                transform.position = Vector3.Lerp(startPos, endPos, t);
                targetZoomHeight = Mathf.Lerp(startZoom, endZoom, t);

                yield return null;
            }

            transform.position = endPos;
            targetZoomHeight = endZoom;
            OnCameraMove?.Invoke(transform.position);
            // Camera is now freeform — player can pan/zoom/orbit during travel
        }

        private IEnumerator FocusTransition(Vector3 planetPos)
        {
            Vector3 startPos = transform.position;
            float endZoom = minZoom + 20f;

            // The camera is above+behind the rig looking down at ~50 degrees.
            // Offset the rig behind the planet so the camera looks AT it.
            Vector3 rigBack = -transform.forward;
            rigBack.y = 0f;
            if (rigBack.sqrMagnitude < 0.01f) rigBack = -Vector3.forward;
            rigBack.Normalize();
            Vector3 endPos = planetPos + rigBack * endZoom * 0.8f;
            endPos.y = planetPos.y;

            float startZoom = targetZoomHeight;
            float elapsed = 0f;

            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseInOutCubic(elapsed / focusDuration);

                transform.position = Vector3.Lerp(startPos, endPos, t);
                targetZoomHeight = Mathf.Lerp(startZoom, endZoom, t);

                yield return null;
            }

            transform.position = endPos;
            targetZoomHeight = endZoom;
            OnCameraMove?.Invoke(transform.position);
        }

        private IEnumerator ReturnTransition()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startZoom = targetZoomHeight;
            float elapsed = 0f;

            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = UI.UIAnimationUtility.EaseInOutCubic(elapsed / focusDuration);

                transform.position = Vector3.Lerp(startPos, defaultPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, defaultRotation, t);
                targetZoomHeight = Mathf.Lerp(startZoom, defaultZoom, t);

                yield return null;
            }

            transform.position = defaultPosition;
            transform.rotation = defaultRotation;
            targetZoomHeight = defaultZoom;
            OnCameraMove?.Invoke(transform.position);
        }
    }
}
