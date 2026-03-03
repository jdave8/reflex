using System.Collections;
using UnityEngine;
using TMPro;
using Reflex.Core;
using Reflex.Scoring;

namespace Reflex.UI
{
    /// <summary>
    /// Displays floating "PERFECT!" / "GREAT!" etc. labels at the hit position.
    /// Uses a pool of TMP_Text elements for zero-allocation feedback.
    /// </summary>
    public class HitFeedbackLabel : MonoBehaviour
    {
        [SerializeField] private RectTransform container;

        private TMP_Text[] _labels;
        private int _nextLabel;
        private Camera _mainCam;

        private const int POOL_SIZE = 5;
        private const float DRIFT_DISTANCE = 80f;
        private const float DURATION = 0.8f;

        private void Start()
        {
            _mainCam = Camera.main;

            // Create pool of reusable labels
            _labels = new TMP_Text[POOL_SIZE];
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var go = new GameObject($"HitLabel_{i}");
                go.transform.SetParent(container, false);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 48;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(300, 80);

                go.SetActive(false);
                _labels[i] = tmp;
            }

            // Subscribe to hit events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit += ShowFeedback;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit -= ShowFeedback;
            }
        }

        private void ShowFeedback(HitQualityType quality, Vector2 worldPos)
        {
            if (_mainCam == null || _labels == null) return;

            var label = _labels[_nextLabel];
            _nextLabel = (_nextLabel + 1) % POOL_SIZE;

            // Set text and color based on quality
            label.text = HitQuality.GetLabel(quality);
            label.color = HitQuality.GetColor(quality);

            // Convert world position to canvas local position
            Vector2 screenPos = _mainCam.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screenPos, null, out Vector2 localPos);

            label.rectTransform.anchoredPosition = localPos;
            label.rectTransform.localScale = Vector3.one;
            label.gameObject.SetActive(true);

            StartCoroutine(AnimateLabel(label));
        }

        private IEnumerator AnimateLabel(TMP_Text label)
        {
            float elapsed = 0f;
            Vector2 startPos = label.rectTransform.anchoredPosition;
            Color startColor = label.color;

            while (elapsed < DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / DURATION;

                // Drift upward
                label.rectTransform.anchoredPosition = startPos + Vector2.up * (DRIFT_DISTANCE * t);

                // Fade out
                Color c = startColor;
                c.a = 1f - t;
                label.color = c;

                // Pop scale on appear
                float scale;
                if (t < 0.1f)
                    scale = Mathf.Lerp(0.5f, 1.2f, t / 0.1f);
                else if (t < 0.2f)
                    scale = Mathf.Lerp(1.2f, 1f, (t - 0.1f) / 0.1f);
                else
                    scale = 1f;

                label.rectTransform.localScale = Vector3.one * scale;

                yield return null;
            }

            label.gameObject.SetActive(false);
        }
    }
}
