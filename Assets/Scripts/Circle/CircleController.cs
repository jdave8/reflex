using System;
using UnityEngine;
using Reflex.Core;
using Reflex.Scoring;
using Reflex.Visual;

namespace Reflex.Circle
{
    public class CircleController : MonoBehaviour
    {
        private float _ringProgress;     // 0 = outer edge, 1 = center (perfect)
        private float _ringCloseTime;    // Seconds to fully close
        private bool _resolved;          // Prevents double-tap or double-miss
        private bool _active;

        private CircleVisual _visual;
        private GameConfig _config;

        // Called when the circle misses (ring closes with no tap)
        public event Action<CircleController> OnMissed;

        public bool IsActive => _active;
        public float RingProgress => _ringProgress;

        private void Awake()
        {
            _visual = GetComponent<CircleVisual>();
        }

        public void Initialize(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Activate this circle at a world position with a given close time.
        /// Called by the pool/spawner.
        /// </summary>
        public void Activate(Vector2 worldPos, float closeTime)
        {
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            transform.localScale = Vector3.one * (_config.circleWorldRadius * 2f);

            _ringCloseTime = closeTime;
            _ringProgress = 0f;
            _resolved = false;
            _active = true;

            _visual.ResetVisual(
                _config.ringColor,
                _config.circleBaseColor,
                _config.ringGlowIntensity
            );

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivate and return to pool.
        /// </summary>
        public void Deactivate()
        {
            _active = false;
            _resolved = true;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_active || _resolved) return;

            // Advance ring toward center
            _ringProgress += Time.deltaTime / _ringCloseTime;
            _visual.SetProgress(Mathf.Clamp01(_ringProgress));

            // Ring fully closed without a tap = MISS
            if (_ringProgress >= 1.0f)
            {
                _resolved = true;
                OnMissed?.Invoke(this);
            }
        }

        /// <summary>
        /// Evaluate the hit quality based on current ring progress.
        /// Called by TouchInputHandler when player taps near this circle.
        /// </summary>
        public HitQualityType EvaluateHit()
        {
            if (_resolved || !_active) return HitQualityType.Miss;
            return HitQuality.Evaluate(_ringProgress, _config);
        }

        /// <summary>
        /// Resolve this circle after a tap. Returns the quality.
        /// </summary>
        public HitQualityType Resolve()
        {
            if (_resolved || !_active) return HitQualityType.Miss;

            _resolved = true;
            HitQualityType quality = HitQuality.Evaluate(_ringProgress, _config);

            if (quality != HitQualityType.Miss)
            {
                // Flash the hit color briefly, then deactivate
                _visual.Flash(HitQuality.GetColor(quality), 2f);
                // Deactivate after a brief delay for visual feedback
                Invoke(nameof(Deactivate), 0.1f);
            }

            return quality;
        }
    }
}
