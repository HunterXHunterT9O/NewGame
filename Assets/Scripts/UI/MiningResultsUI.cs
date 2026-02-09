using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using EdgeOfUniverse.RTS;

namespace EdgeOfUniverse.UI
{
    public class MiningResultsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private RectTransform resultsPanel;
        [SerializeField] private CanvasGroup resultsGroup;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI planetNameText;
        [SerializeField] private TextMeshProUGUI dangerText;

        [Header("Resource Rows")]
        [SerializeField] private TextMeshProUGUI mineralsValueText;
        [SerializeField] private TextMeshProUGUI fuelValueText;
        [SerializeField] private TextMeshProUGUI bioMatterValueText;
        [SerializeField] private TextMeshProUGUI techSalvageValueText;

        [Header("Losses")]
        [SerializeField] private GameObject lossRow;
        [SerializeField] private TextMeshProUGUI lossText;

        [Header("Continue Button")]
        [SerializeField] private Button continueButton;

        public event System.Action OnContinue;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        public void Show(MiningResult result)
        {
            if (resultsPanel == null || resultsGroup == null) return;

            // Set header
            if (titleText != null)
                titleText.text = "MINING OPERATION COMPLETE";
            if (planetNameText != null)
                planetNameText.text = result.planetName.ToUpper();
            if (dangerText != null)
            {
                string dangerStars = new string('\u2666', result.dangerLevel) +
                                     new string('\u2662', 5 - result.dangerLevel);
                dangerText.text = $"THREAT: {dangerStars}";
                dangerText.color = GetDangerColor(result.dangerLevel);
            }

            // Populate resource values (start at 0 for count-up)
            if (mineralsValueText != null) mineralsValueText.text = "0";
            if (fuelValueText != null) fuelValueText.text = "0";
            if (bioMatterValueText != null) bioMatterValueText.text = "0";
            if (techSalvageValueText != null) techSalvageValueText.text = "0";

            // Loss row
            if (lossRow != null)
            {
                lossRow.SetActive(result.lost > 0);
            }
            if (lossText != null && result.lost > 0)
            {
                lossText.text = $"HOSTILE INTERFERENCE: -{result.lost} UNITS LOST";
            }

            // Show with PopIn animation
            resultsPanel.gameObject.SetActive(true);
            StartCoroutine(ShowSequence(result));
        }

        private IEnumerator ShowSequence(MiningResult result)
        {
            yield return StartCoroutine(UIAnimationUtility.PopIn(resultsPanel, resultsGroup));

            // Animated count-up for each resource
            float countUpDuration = 0.5f;

            int minerals = GetExtracted(result, ResourceType.Minerals);
            int fuel = GetExtracted(result, ResourceType.Fuel);
            int bio = GetExtracted(result, ResourceType.BioMatter);
            int tech = GetExtracted(result, ResourceType.TechSalvage);

            // Stagger the count-ups
            if (minerals > 0)
            {
                yield return StartCoroutine(CountUp(mineralsValueText, minerals, countUpDuration));
                yield return new WaitForSeconds(0.1f);
            }
            if (fuel > 0)
            {
                yield return StartCoroutine(CountUp(fuelValueText, fuel, countUpDuration));
                yield return new WaitForSeconds(0.1f);
            }
            if (bio > 0)
            {
                yield return StartCoroutine(CountUp(bioMatterValueText, bio, countUpDuration));
                yield return new WaitForSeconds(0.1f);
            }
            if (tech > 0)
            {
                yield return StartCoroutine(CountUp(techSalvageValueText, tech, countUpDuration));
            }
        }

        private int GetExtracted(MiningResult result, ResourceType type)
        {
            if (result.extracted != null && result.extracted.TryGetValue(type, out int val))
                return val;
            return 0;
        }

        private IEnumerator CountUp(TextMeshProUGUI text, int targetValue, float duration)
        {
            if (text == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = UIAnimationUtility.EaseOutQuad(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, t));
                text.text = $"+{current}";
                yield return null;
            }

            text.text = $"+{targetValue}";
            // Flash the final value
            StartCoroutine(UIAnimationUtility.TextFlash(text));
        }

        public void Hide()
        {
            if (resultsGroup != null)
            {
                resultsGroup.alpha = 0f;
                resultsGroup.interactable = false;
                resultsGroup.blocksRaycasts = false;
            }
            if (resultsPanel != null)
                resultsPanel.gameObject.SetActive(false);
        }

        private void OnContinueClicked()
        {
            if (continueButton != null)
                StartCoroutine(UIAnimationUtility.ClickPunch(continueButton.transform));

            StartCoroutine(DismissSequence());
        }

        private IEnumerator DismissSequence()
        {
            if (resultsGroup != null)
            {
                yield return StartCoroutine(UIAnimationUtility.FadeOut(resultsGroup, 0.2f, false));
            }
            // Fire event BEFORE Hide() — Hide deactivates the GameObject which kills the coroutine
            OnContinue?.Invoke();
            Hide();
        }

        private Color GetDangerColor(int level)
        {
            switch (level)
            {
                case 1: return new Color(0.22f, 0.63f, 0.29f);
                case 2: return new Color(0.78f, 0.55f, 0.16f);
                case 3: return new Color(0.90f, 0.47f, 0.12f);
                case 4: return new Color(0.71f, 0.18f, 0.18f);
                case 5: return new Color(1f, 0.12f, 0.12f);
                default: return new Color(0.22f, 0.63f, 0.29f);
            }
        }
    }
}
