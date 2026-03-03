using UnityEngine;
using Reflex.Core;
using Reflex.Scoring;

namespace Reflex.Visual
{
    /// <summary>
    /// Spawns a burst of particles at each hit location.
    /// Color and count scale with hit quality.
    /// Subscribes to GameManager.OnCircleHit.
    /// </summary>
    public class HitEffectPlayer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem hitParticles;

        private static readonly ParticleSystem.EmitParams EmitParams = new ParticleSystem.EmitParams();

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCircleHit += PlayHitEffect;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCircleHit -= PlayHitEffect;
        }

        private void PlayHitEffect(HitQualityType quality, Vector2 worldPos)
        {
            if (hitParticles == null) return;

            // Move particle system to hit location
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Color by quality
            var main = hitParticles.main;
            Color color = HitQuality.GetColor(quality);
            main.startColor = new ParticleSystem.MinMaxGradient(color, color * 0.7f);

            // Burst count by quality
            int burstCount = quality switch
            {
                HitQualityType.Perfect => 40,
                HitQualityType.Great   => 28,
                HitQualityType.Good    => 16,
                HitQualityType.Just    => 8,
                _                      => 0
            };

            if (burstCount > 0)
            {
                hitParticles.Emit(burstCount);
            }
        }
    }
}
