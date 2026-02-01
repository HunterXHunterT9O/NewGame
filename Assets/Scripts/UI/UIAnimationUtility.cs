using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace EdgeOfUniverse.UI
{
    /// <summary>
    /// Reusable UI animation utilities with consistent timing and easing.
    /// Provides the "juice" that makes interactions feel polished.
    /// </summary>
    public static class UIAnimationUtility
    {
        // Standard timings
        public const float QUICK_DURATION = 0.1f;
        public const float STANDARD_DURATION = 0.2f;
        public const float EMPHASIS_DURATION = 0.35f;

        #region Scale Animations

        /// <summary>
        /// Hover scale animation - scale up with ease out back
        /// </summary>
        public static IEnumerator HoverScale(Transform target, float scale = 1.05f, float duration = 0.15f)
        {
            Vector3 start = target.localScale;
            Vector3 end = start.normalized * scale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutBack(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(start, end, t);
                yield return null;
            }

            target.localScale = end;
        }

        /// <summary>
        /// Unhover - return to original scale
        /// </summary>
        public static IEnumerator UnhoverScale(Transform target, Vector3 originalScale, float duration = 0.1f)
        {
            Vector3 start = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutQuad(elapsed / duration);
                target.localScale = Vector3.Lerp(start, originalScale, t);
                yield return null;
            }

            target.localScale = originalScale;
        }

        /// <summary>
        /// Click punch animation - quick squash then return
        /// </summary>
        public static IEnumerator ClickPunch(Transform target, float squashScale = 0.92f, float duration = 0.15f)
        {
            Vector3 original = target.localScale;
            Vector3 squash = original * squashScale;
            Vector3 overshoot = original * 1.03f;

            // Quick squash
            float elapsed = 0f;
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.3f);
                target.localScale = Vector3.Lerp(original, squash, t);
                yield return null;
            }

            // Overshoot bounce
            elapsed = 0f;
            while (elapsed < duration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(elapsed / (duration * 0.4f));
                target.localScale = Vector3.Lerp(squash, overshoot, t);
                yield return null;
            }

            // Settle
            elapsed = 0f;
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.3f);
                target.localScale = Vector3.Lerp(overshoot, original, t);
                yield return null;
            }

            target.localScale = original;
        }

        /// <summary>
        /// Pulse animation - rhythmic scale oscillation
        /// </summary>
        public static IEnumerator Pulse(Transform target, float intensity = 1.08f, float duration = 0.4f, int loops = 1)
        {
            Vector3 original = target.localScale;
            Vector3 peak = original * intensity;

            for (int i = 0; i < loops; i++)
            {
                // Up
                float elapsed = 0f;
                while (elapsed < duration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = EaseOutQuad(elapsed / (duration * 0.5f));
                    target.localScale = Vector3.Lerp(original, peak, t);
                    yield return null;
                }

                // Down
                elapsed = 0f;
                while (elapsed < duration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = EaseInQuad(elapsed / (duration * 0.5f));
                    target.localScale = Vector3.Lerp(peak, original, t);
                    yield return null;
                }
            }

            target.localScale = original;
        }

        #endregion

        #region Fade Animations

        /// <summary>
        /// Fade in a CanvasGroup
        /// </summary>
        public static IEnumerator FadeIn(CanvasGroup group, float duration = 0.2f, bool enableInteraction = true)
        {
            group.gameObject.SetActive(true);
            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(elapsed / duration);
                group.alpha = Mathf.Lerp(start, 1f, t);
                yield return null;
            }

            group.alpha = 1f;
            if (enableInteraction)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Fade out a CanvasGroup
        /// </summary>
        public static IEnumerator FadeOut(CanvasGroup group, float duration = 0.15f, bool deactivate = false)
        {
            group.interactable = false;
            group.blocksRaycasts = false;

            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInQuad(elapsed / duration);
                group.alpha = Mathf.Lerp(start, 0f, t);
                yield return null;
            }

            group.alpha = 0f;
            if (deactivate)
            {
                group.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Fade an Image color
        /// </summary>
        public static IEnumerator FadeImage(Image image, float targetAlpha, float duration = 0.2f)
        {
            Color start = image.color;
            Color end = start;
            end.a = targetAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutQuad(elapsed / duration);
                image.color = Color.Lerp(start, end, t);
                yield return null;
            }

            image.color = end;
        }

        /// <summary>
        /// Crossfade between two colors
        /// </summary>
        public static IEnumerator ColorTransition(Graphic graphic, Color target, float duration = 0.2f)
        {
            Color start = graphic.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutQuad(elapsed / duration);
                graphic.color = Color.Lerp(start, target, t);
                yield return null;
            }

            graphic.color = target;
        }

        #endregion

        #region Appear/Disappear

        /// <summary>
        /// Slide and fade in from direction
        /// </summary>
        public static IEnumerator SlideIn(RectTransform target, CanvasGroup group, Vector2 fromOffset, float duration = 0.25f)
        {
            Vector2 originalPos = target.anchoredPosition;
            Vector2 startPos = originalPos + fromOffset;

            target.anchoredPosition = startPos;
            group.alpha = 0f;
            group.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(elapsed / duration);

                target.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
                group.alpha = t;

                yield return null;
            }

            target.anchoredPosition = originalPos;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        /// <summary>
        /// Slide and fade out in direction
        /// </summary>
        public static IEnumerator SlideOut(RectTransform target, CanvasGroup group, Vector2 toOffset, float duration = 0.15f, bool deactivate = false)
        {
            Vector2 startPos = target.anchoredPosition;
            Vector2 endPos = startPos + toOffset;

            group.interactable = false;
            group.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInQuad(elapsed / duration);

                target.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                group.alpha = 1f - t;

                yield return null;
            }

            target.anchoredPosition = startPos; // Reset position
            group.alpha = 0f;

            if (deactivate)
            {
                group.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Pop in with scale bounce
        /// </summary>
        public static IEnumerator PopIn(Transform target, CanvasGroup group, float duration = 0.25f)
        {
            target.localScale = Vector3.zero;
            group.alpha = 0f;
            group.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float scale = EaseOutBack(t);
                target.localScale = Vector3.one * scale;
                group.alpha = EaseOutQuad(t);

                yield return null;
            }

            target.localScale = Vector3.one;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        #endregion

        #region Special Effects

        /// <summary>
        /// Flash effect - quick white overlay
        /// </summary>
        public static IEnumerator Flash(Image overlay, Color flashColor, float duration = 0.1f)
        {
            if (overlay == null) yield break;

            overlay.gameObject.SetActive(true);
            overlay.color = flashColor;

            // Quick in
            float elapsed = 0f;
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            Color start = flashColor;
            Color end = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            elapsed = 0f;

            while (elapsed < duration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.7f);
                overlay.color = Color.Lerp(start, end, t);
                yield return null;
            }

            overlay.color = end;
            overlay.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shake a RectTransform
        /// </summary>
        public static IEnumerator Shake(RectTransform target, float intensity = 5f, float duration = 0.2f)
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

        /// <summary>
        /// Text typewriter effect
        /// </summary>
        public static IEnumerator Typewriter(TextMeshProUGUI text, string content, float charDelay = 0.03f)
        {
            text.text = "";
            text.ForceMeshUpdate();

            foreach (char c in content)
            {
                text.text += c;
                yield return new WaitForSeconds(charDelay);
            }
        }

        /// <summary>
        /// Text flash effect (alpha pulsing)
        /// </summary>
        public static IEnumerator TextFlash(TextMeshProUGUI text, int flashes = 3, float flashDuration = 0.1f)
        {
            for (int i = 0; i < flashes; i++)
            {
                text.alpha = 0.3f;
                yield return new WaitForSeconds(flashDuration * 0.5f);
                text.alpha = 1f;
                yield return new WaitForSeconds(flashDuration * 0.5f);
            }
        }

        /// <summary>
        /// Grayscale transition for death/disabled states
        /// </summary>
        public static IEnumerator GrayscaleTransition(Image[] images, float duration = 0.5f)
        {
            Color[] originalColors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    originalColors[i] = images[i].color;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] == null) continue;

                    Color original = originalColors[i];
                    float gray = (original.r + original.g + original.b) / 3f;
                    Color grayColor = new Color(gray, gray, gray, original.a);

                    images[i].color = Color.Lerp(original, grayColor, t);
                }

                yield return null;
            }
        }

        #endregion

        #region Easing Functions

        public static float EaseInQuad(float t) => t * t;

        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float EaseOutElastic(float t)
        {
            if (t == 0f || t == 1f) return t;

            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        #endregion
    }
}
