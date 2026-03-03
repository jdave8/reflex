using System.Collections.Generic;
using UnityEngine;
using Reflex.Core;

namespace Reflex.Circle
{
    public class CircleSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private CirclePool _pool;
        private Camera _mainCam;
        private float _spawnTimer;
        private bool _spawning;
        private int _consecutiveHits;

        private List<CircleController> _activeCircles = new List<CircleController>();

        public List<CircleController> ActiveCircles => _activeCircles;

        private void Awake()
        {
            _pool = GetComponent<CirclePool>();
            _mainCam = Camera.main;
        }

        public void Initialize()
        {
            _pool.Initialize(config);
        }

        public void StartSpawning()
        {
            _spawning = true;
            _consecutiveHits = 0;
            _spawnTimer = 0.5f; // Brief delay before first circle
        }

        public void StopSpawning()
        {
            _spawning = false;
        }

        public void ClearAll()
        {
            _pool.ReturnAll(_activeCircles);
        }

        public void OnHit()
        {
            _consecutiveHits++;
        }

        private void Update()
        {
            if (!_spawning) return;

            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                // Check if we can spawn more circles
                int maxSimultaneous = GetMaxSimultaneousCircles();
                if (_activeCircles.Count < maxSimultaneous)
                {
                    SpawnCircle();
                }

                _spawnTimer = GetSpawnInterval();
            }
        }

        private void SpawnCircle()
        {
            CircleController circle = _pool.Get();

            Vector2 spawnPos = GetRandomSpawnPosition();
            float closeTime = GetRingCloseTime();

            circle.Activate(spawnPos, closeTime);
            circle.OnMissed += HandleCircleMissed;

            _activeCircles.Add(circle);
        }

        public void ReturnCircle(CircleController circle)
        {
            circle.OnMissed -= HandleCircleMissed;
            _activeCircles.Remove(circle);
            _pool.Return(circle);
        }

        private void HandleCircleMissed(CircleController circle)
        {
            // Notify GameManager about the miss
            GameManager.Instance.RegisterMiss(circle.transform.position);
        }

        private Vector2 GetRandomSpawnPosition()
        {
            float margin = config.spawnMargin;
            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float vx = Random.Range(margin, 1f - margin);
                float vy = Random.Range(margin, 1f - margin);

                Vector3 worldPos = _mainCam.ViewportToWorldPoint(new Vector3(vx, vy, 10f));
                Vector2 candidate = new Vector2(worldPos.x, worldPos.y);

                // Check distance from other active circles
                bool tooClose = false;
                foreach (var active in _activeCircles)
                {
                    if (Vector2.Distance(candidate, (Vector2)active.transform.position) < config.minCircleSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return candidate;
            }

            // Fallback: accept whatever we got
            float fx = Random.Range(margin, 1f - margin);
            float fy = Random.Range(margin, 1f - margin);
            Vector3 fallback = _mainCam.ViewportToWorldPoint(new Vector3(fx, fy, 10f));
            return new Vector2(fallback.x, fallback.y);
        }

        /// <summary>
        /// Exponential decay: approaches min but never reaches it.
        /// value(hits) = floor + (ceiling - floor) * e^(-hits * rate)
        /// </summary>
        private float GetRingCloseTime()
        {
            float t = _consecutiveHits * config.difficultyRampRate;
            return config.minRingCloseTime +
                   (config.baseRingCloseTime - config.minRingCloseTime) * Mathf.Exp(-t);
        }

        private float GetSpawnInterval()
        {
            float t = _consecutiveHits * config.spawnIntervalRampRate;
            return config.minSpawnInterval +
                   (config.baseSpawnInterval - config.minSpawnInterval) * Mathf.Exp(-t);
        }

        private int GetMaxSimultaneousCircles()
        {
            if (_consecutiveHits >= config.tripleCircleThreshold) return 3;
            if (_consecutiveHits >= config.multiCircleThreshold) return 2;
            return 1;
        }
    }
}
