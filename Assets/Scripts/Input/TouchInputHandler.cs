using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Reflex.Circle;
using Reflex.Core;
using Reflex.Scoring;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Reflex.Input
{
    public class TouchInputHandler : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private CircleSpawner circleSpawner;

        private Camera _mainCam;

        private void Awake()
        {
            _mainCam = Camera.main;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable(); // Mouse simulation for Editor testing
        }

        private void OnDisable()
        {
            TouchSimulation.Disable();
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            var state = GameManager.Instance.CurrentState;

            // Handle tap-to-start (any tap works — no UI filter)
            if (state == GameState.ReadyToPlay)
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        GameManager.Instance.StartGame();
                        return;
                    }
                }
                return;
            }

            // Handle tap-to-restart from score card (any tap works)
            if (state == GameState.ScoreCard)
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        GameManager.Instance.ReturnToReady();
                        return;
                    }
                }
                return;
            }

            // Handle gameplay taps
            if (state != GameState.Playing) return;

            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    if (!IsPointerOverUI(touch))
                    {
                        ProcessGameTouch(touch.screenPosition);
                    }
                }
            }
        }

        private void ProcessGameTouch(Vector2 screenPos)
        {
            Vector3 worldPos3 = _mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            Vector2 worldPos = new Vector2(worldPos3.x, worldPos3.y);

            // Find the nearest active circle within tap range
            CircleController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var circle in circleSpawner.ActiveCircles)
            {
                if (!circle.IsActive) continue;

                float dist = Vector2.Distance(worldPos, (Vector2)circle.transform.position);

                // Only consider circles within the outer ring radius
                float maxTapRadius = config.circleWorldRadius * config.ringStartRadiusMultiplier;
                if (dist < maxTapRadius && dist < nearestDist)
                {
                    nearest = circle;
                    nearestDist = dist;
                }
            }

            // Tapping empty screen = no penalty (per spec)
            if (nearest == null) return;

            // Evaluate and resolve the hit
            HitQualityType quality = nearest.Resolve();

            if (quality == HitQualityType.Miss)
            {
                // Ring was too far from center - still counts as a hit attempt
                // but the timing was off. In practice, the ring auto-misses
                // when it closes, so direct tap-miss is rare.
                GameManager.Instance.RegisterMiss(nearest.transform.position);
            }
            else
            {
                GameManager.Instance.RegisterHit(quality, nearest.transform.position);
            }
        }

        private bool IsPointerOverUI(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject(touch.touchId);
        }
    }
}
