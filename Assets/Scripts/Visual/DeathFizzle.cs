using System.Collections;
using UnityEngine;
using Reflex.Circle;
using Reflex.Core;

namespace Reflex.Visual
{
    /// <summary>
    /// Plays a shake → red-shift → shrink animation on the missed circle.
    /// Lives on the CirclePrefab. Subscribes to its own CircleController.OnMissed.
    /// </summary>
    [RequireComponent(typeof(CircleController))]
    [RequireComponent(typeof(CircleVisual))]
    public class DeathFizzle : MonoBehaviour
    {
        private CircleController _controller;
        private CircleVisual _visual;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Coroutine _fizzleCoroutine;

        private void Awake()
        {
            _controller = GetComponent<CircleController>();
            _visual = GetComponent<CircleVisual>();
        }

        private void OnEnable()
        {
            if (_controller != null)
                _controller.OnMissed += OnMissed;
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnMissed -= OnMissed;

            if (_fizzleCoroutine != null)
            {
                StopCoroutine(_fizzleCoroutine);
                _fizzleCoroutine = null;
            }
        }

        private void OnMissed(CircleController ctrl)
        {
            _originalPosition = transform.position;
            _originalScale = transform.localScale;
            _fizzleCoroutine = StartCoroutine(FizzleRoutine());
        }

        private IEnumerator FizzleRoutine()
        {
            float duration = 1.2f;
            if (GameManager.Instance != null && GameManager.Instance.Config != null)
                duration = GameManager.Instance.Config.deathFizzleDuration;

            float elapsed = 0f;
            Color missRed = new Color(1f, 0.15f, 0.1f, 1f);

            // Flash red immediately
            _visual.Flash(missRed, 2.5f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;

                // ── Shake: amplitude decreases over time ──
                float shakeAmount = 0.15f * (1f - t);
                Vector2 shakeOffset = Random.insideUnitCircle * shakeAmount;
                transform.position = _originalPosition +
                                     new Vector3(shakeOffset.x, shakeOffset.y, 0f);

                // ── Shrink: accelerating ease-in ──
                float scaleMultiplier = Mathf.Lerp(1f, 0f, t * t);
                transform.localScale = _originalScale * scaleMultiplier;

                // ── Glow fades out ──
                float glowFade = Mathf.Lerp(2.5f, 0f, t);
                Color fading = missRed;
                fading.a = 1f - t;
                _visual.Flash(fading, glowFade);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure it's fully hidden at the end
            transform.localScale = Vector3.zero;
            _fizzleCoroutine = null;
        }
    }
}
