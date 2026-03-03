using UnityEngine;
using Reflex.Core;

namespace Reflex.Visual
{
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (_camera != null && config != null)
            {
                _camera.backgroundColor = config.backgroundColor;
            }
        }
    }
}
