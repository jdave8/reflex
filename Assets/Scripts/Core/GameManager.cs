using System;
using UnityEngine;
using Reflex.Circle;
using Reflex.Scoring;

namespace Reflex.Core
{
    public enum GameState
    {
        Boot,
        ReadyToPlay,
        Playing,
        Death,
        ScoreCard
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private CircleSpawner circleSpawner;

        /// <summary>Expose config for DeathFizzle and other runtime systems.</summary>
        public GameConfig Config => config;

        public GameState CurrentState { get; private set; } = GameState.Boot;

        // Events for other systems to subscribe to
        public event Action OnGameStart;
        public event Action OnGameOver;
        public event Action<HitQualityType, Vector2> OnCircleHit;
        public event Action<Vector2> OnCircleMiss;
        public event Action OnReturnToReady;

        // Stats for current run
        public int ConsecutiveHits { get; private set; }
        public int Score { get; private set; }
        public float TimeSurvived { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Target 60fps on mobile
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            circleSpawner.Initialize();
            TransitionTo(GameState.ReadyToPlay);
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                TimeSurvived += Time.deltaTime;
            }
        }

        public void StartGame()
        {
            if (CurrentState != GameState.ReadyToPlay) return;

            ConsecutiveHits = 0;
            Score = 0;
            TimeSurvived = 0f;

            TransitionTo(GameState.Playing);
            circleSpawner.StartSpawning();
            OnGameStart?.Invoke();
        }

        public void RegisterHit(HitQualityType quality, Vector2 worldPos)
        {
            if (CurrentState != GameState.Playing) return;

            ConsecutiveHits++;

            // Calculate score for this hit
            float qualityMult = config.qualityMultipliers[(int)quality];
            float streakMult = 1f + Mathf.Min(
                ConsecutiveHits * config.streakMultiplierPerHit,
                config.maxStreakMultiplier - 1f
            );
            float timeMult = 1f + TimeSurvived * config.timeSurvivalMultiplierRate;
            int points = Mathf.RoundToInt(config.baseHitPoints * qualityMult * streakMult * timeMult);
            Score += points;

            // Notify spawner
            circleSpawner.OnHit();

            // Find and return the circle
            var activeCircles = circleSpawner.ActiveCircles;
            for (int i = activeCircles.Count - 1; i >= 0; i--)
            {
                var circle = activeCircles[i];
                if (Vector2.Distance(worldPos, (Vector2)circle.transform.position) < 0.1f)
                {
                    // Delay return slightly to allow visual flash
                    StartCoroutine(DelayedReturnCircle(circle, 0.1f));
                    break;
                }
            }

            OnCircleHit?.Invoke(quality, worldPos);

            Debug.Log($"HIT: {quality} | Hits: {ConsecutiveHits} | Score: {Score} | " +
                     $"Points: +{points} (q:{qualityMult:F1} s:{streakMult:F2} t:{timeMult:F2})");
        }

        public void RegisterMiss(Vector2 worldPos)
        {
            if (CurrentState != GameState.Playing) return;

            circleSpawner.StopSpawning();
            OnCircleMiss?.Invoke(worldPos);

            Debug.Log($"MISS! Final Score: {Score} | Hits: {ConsecutiveHits} | " +
                     $"Time: {TimeSurvived:F1}s");

            // Transition to death, then score card after delay
            TransitionTo(GameState.Death);
            OnGameOver?.Invoke();

            // Clear remaining circles and show score card after delay
            Invoke(nameof(ShowScoreCard), config.deathFizzleDuration + config.scoreCardDelay);
        }

        public void ReturnToReady()
        {
            if (CurrentState != GameState.ScoreCard) return;

            circleSpawner.ClearAll();
            TransitionTo(GameState.ReadyToPlay);
            OnReturnToReady?.Invoke();
        }

        private void ShowScoreCard()
        {
            circleSpawner.ClearAll();
            TransitionTo(GameState.ScoreCard);
        }

        private void TransitionTo(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"Game State: {newState}");
        }

        private System.Collections.IEnumerator DelayedReturnCircle(CircleController circle, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (circle != null && circleSpawner.ActiveCircles.Contains(circle))
            {
                circleSpawner.ReturnCircle(circle);
            }
        }

        /// <summary>
        /// Get the current stadium tier based on consecutive hits.
        /// </summary>
        public int GetStadiumTierIndex()
        {
            int tier = 0;
            for (int i = config.stadiumThresholds.Length - 1; i >= 0; i--)
            {
                if (ConsecutiveHits >= config.stadiumThresholds[i])
                {
                    tier = i;
                    break;
                }
            }
            return tier;
        }

        public string GetStadiumTierName()
        {
            int index = GetStadiumTierIndex();
            return config.stadiumNames[index];
        }
    }
}
