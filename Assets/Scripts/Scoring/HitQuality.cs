using UnityEngine;
using Reflex.Core;

namespace Reflex.Scoring
{
    public enum HitQualityType
    {
        Perfect = 0,
        Great = 1,
        Good = 2,
        Just = 3,
        Miss = 4
    }

    public static class HitQuality
    {
        public static HitQualityType Evaluate(float ringProgress, GameConfig config)
        {
            // ringProgress goes from 0 (outer edge) to 1 (center)
            // deviation = how far from perfect (0 = dead on)
            float deviation = Mathf.Abs(1.0f - ringProgress);

            if (deviation <= config.perfectZone) return HitQualityType.Perfect;
            if (deviation <= config.greatZone) return HitQualityType.Great;
            if (deviation <= config.goodZone) return HitQualityType.Good;
            if (deviation <= config.justZone) return HitQualityType.Just;
            return HitQualityType.Miss;
        }

        public static Color GetColor(HitQualityType type)
        {
            return type switch
            {
                HitQualityType.Perfect => new Color(1f, 0.84f, 0f),       // Gold
                HitQualityType.Great => new Color(0f, 0.9f, 1f),          // Cyan
                HitQualityType.Good => new Color(0.3f, 1f, 0.3f),         // Green
                HitQualityType.Just => new Color(0.7f, 0.7f, 0.7f),       // Gray
                _ => new Color(1f, 0.2f, 0.2f)                            // Red
            };
        }

        public static string GetLabel(HitQualityType type)
        {
            return type switch
            {
                HitQualityType.Perfect => "PERFECT!",
                HitQualityType.Great => "GREAT!",
                HitQualityType.Good => "GOOD",
                HitQualityType.Just => "JUST",
                _ => "MISS"
            };
        }
    }
}
