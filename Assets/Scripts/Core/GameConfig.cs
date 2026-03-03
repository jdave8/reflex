using UnityEngine;

namespace Reflex.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Reflex/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Circle Mechanics")]
        [Tooltip("Visual radius of the target circle in world units")]
        public float circleWorldRadius = 0.6f;

        [Tooltip("Outer ring starts at this multiple of circle radius")]
        public float ringStartRadiusMultiplier = 3.0f;

        [Tooltip("Seconds for ring to fully close at difficulty 0")]
        public float baseRingCloseTime = 2.0f;

        [Header("Hit Zones (fraction of ring travel, 0 = dead center)")]
        public float perfectZone = 0.08f;
        public float greatZone = 0.18f;
        public float goodZone = 0.30f;
        public float justZone = 0.45f;

        [Header("Scoring")]
        public int baseHitPoints = 100;
        public float[] qualityMultipliers = { 3.0f, 2.0f, 1.5f, 1.0f, 0f };
        public float streakMultiplierPerHit = 0.01f;
        public float maxStreakMultiplier = 5.0f;
        public float timeSurvivalMultiplierRate = 0.001f;

        [Header("Difficulty Curve")]
        [Tooltip("Fastest possible ring close time (human limit floor)")]
        public float minRingCloseTime = 0.4f;

        [Tooltip("How quickly ring close time decreases per hit")]
        public float difficultyRampRate = 0.02f;

        [Tooltip("Seconds between spawns at game start")]
        public float baseSpawnInterval = 2.5f;

        [Tooltip("Minimum seconds between spawns")]
        public float minSpawnInterval = 0.6f;

        [Tooltip("How quickly spawn interval decreases per hit")]
        public float spawnIntervalRampRate = 0.015f;

        [Tooltip("Hit count where 2nd simultaneous circle can appear")]
        public int multiCircleThreshold = 200;

        [Tooltip("Hit count where 3rd simultaneous circle can appear")]
        public int tripleCircleThreshold = 400;

        [Header("Stadium Tiers")]
        public int[] stadiumThresholds = { 0, 50, 150, 300, 500 };
        public string[] stadiumNames = { "Rookie", "Club", "Arena", "Stadium", "Legend" };

        [Header("Spawn Bounds")]
        [Tooltip("Viewport fraction margin from screen edges")]
        public float spawnMargin = 0.1f;

        [Tooltip("Minimum distance between active circles (world units)")]
        public float minCircleSpacing = 1.5f;

        [Header("Visual")]
        public Color backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        public float deathFizzleDuration = 1.2f;
        public float scoreCardDelay = 0.3f;

        [Header("Circle Colors")]
        public Color circleBaseColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        public Color ringColor = new Color(0f, 0.9f, 1f, 1f);
        public float ringGlowIntensity = 0.5f;
    }
}
