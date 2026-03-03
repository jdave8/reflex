using UnityEngine;
using Reflex.Core;
using Reflex.Scoring;

namespace Reflex.Visual
{
    /// <summary>
    /// Full-screen edge glow that builds when the player maintains a Perfect streak ≥ 5.
    /// Drives the _Intensity property on an EdgeGlow material via MaterialPropertyBlock.
    /// Lives on a world-space Quad sized to fill the camera viewport.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ScreenEdgeGlow : MonoBehaviour
    {
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");

        [Header("Tuning")]
        [SerializeField] private int perfectStreakThreshold = 5;
        [SerializeField] private float intensityPerPerfect = 0.15f;
        [SerializeField] private float maxIntensity = 0.8f;
        [SerializeField] private float buildSpeed = 3f;
        [SerializeField] private float fadeSpeed = 5f;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _perfectStreak;
        private float _currentIntensity;
        private float _targetIntensity;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            _mpb.SetFloat(IntensityID, 0f);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void Start()
        {
            // Size quad to fill orthographic camera viewport
            Camera cam = Camera.main;
            if (cam != null)
            {
                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;
                transform.localScale = new Vector3(width, height, 1f);
                transform.position = new Vector3(
                    cam.transform.position.x,
                    cam.transform.position.y,
                    cam.transform.position.z + 1f // Just in front of camera
                );
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit += OnHit;
                GameManager.Instance.OnCircleMiss += OnMiss;
                GameManager.Instance.OnReturnToReady += OnReset;
                GameManager.Instance.OnGameOver += OnGameOver;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit -= OnHit;
                GameManager.Instance.OnCircleMiss -= OnMiss;
                GameManager.Instance.OnReturnToReady -= OnReset;
                GameManager.Instance.OnGameOver -= OnGameOver;
            }
        }

        private void OnHit(HitQualityType quality, Vector2 pos)
        {
            if (quality == HitQualityType.Perfect)
            {
                _perfectStreak++;
                if (_perfectStreak >= perfectStreakThreshold)
                {
                    _targetIntensity = Mathf.Min(
                        (_perfectStreak - perfectStreakThreshold + 1) * intensityPerPerfect,
                        maxIntensity
                    );
                }
            }
            else
            {
                // Non-perfect hit breaks the streak
                _perfectStreak = 0;
                _targetIntensity = 0f;
            }
        }

        private void OnMiss(Vector2 pos)
        {
            _perfectStreak = 0;
            _targetIntensity = 0f;
        }

        private void OnGameOver()
        {
            _perfectStreak = 0;
            _targetIntensity = 0f;
        }

        private void OnReset()
        {
            _perfectStreak = 0;
            _targetIntensity = 0f;
            _currentIntensity = 0f;
            _mpb.SetFloat(IntensityID, 0f);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void Update()
        {
            if (_renderer == null) return;

            // Smooth transition: builds slowly, fades quickly
            float speed = _targetIntensity > _currentIntensity ? buildSpeed : fadeSpeed;
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, speed * Time.deltaTime);

            _mpb.SetFloat(IntensityID, _currentIntensity);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
