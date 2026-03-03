using System.Collections.Generic;
using UnityEngine;
using Reflex.Core;

namespace Reflex.Circle
{
    public class CirclePool : MonoBehaviour
    {
        [SerializeField] private GameObject circlePrefab;
        [SerializeField] private int initialPoolSize = 5;

        private Queue<CircleController> _available = new Queue<CircleController>();
        private GameConfig _config;

        public void Initialize(GameConfig config)
        {
            _config = config;

            for (int i = 0; i < initialPoolSize; i++)
            {
                CircleController circle = CreateInstance();
                _available.Enqueue(circle);
            }
        }

        public CircleController Get()
        {
            CircleController circle;

            if (_available.Count > 0)
            {
                circle = _available.Dequeue();
            }
            else
            {
                circle = CreateInstance();
            }

            return circle;
        }

        public void Return(CircleController circle)
        {
            circle.Deactivate();
            _available.Enqueue(circle);
        }

        public void ReturnAll(List<CircleController> activeCircles)
        {
            for (int i = activeCircles.Count - 1; i >= 0; i--)
            {
                Return(activeCircles[i]);
            }
            activeCircles.Clear();
        }

        private CircleController CreateInstance()
        {
            GameObject go = Instantiate(circlePrefab, transform);
            go.SetActive(false);

            CircleController controller = go.GetComponent<CircleController>();
            controller.Initialize(_config);

            return controller;
        }
    }
}
