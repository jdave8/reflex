using UnityEngine;

namespace Reflex.Visual
{
    [RequireComponent(typeof(MeshRenderer))]
    public class CircleVisual : MonoBehaviour
    {
        private static readonly int ProgressID = Shader.PropertyToID("_Progress");
        private static readonly int RingColorID = Shader.PropertyToID("_RingColor");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");

        private MaterialPropertyBlock _mpb;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void SetProgress(float progress)
        {
            _mpb.SetFloat(ProgressID, progress);
            _meshRenderer.SetPropertyBlock(_mpb);
        }

        public void SetColors(Color ringColor, Color baseColor, float glowIntensity)
        {
            _mpb.SetColor(RingColorID, ringColor);
            _mpb.SetColor(BaseColorID, baseColor);
            _mpb.SetFloat(GlowIntensityID, glowIntensity);
            _meshRenderer.SetPropertyBlock(_mpb);
        }

        public void Flash(Color color, float intensity)
        {
            _mpb.SetColor(RingColorID, color);
            _mpb.SetFloat(GlowIntensityID, intensity);
            _meshRenderer.SetPropertyBlock(_mpb);
        }

        public void ResetVisual(Color ringColor, Color baseColor, float glowIntensity)
        {
            _mpb.SetFloat(ProgressID, 0f);
            _mpb.SetColor(RingColorID, ringColor);
            _mpb.SetColor(BaseColorID, baseColor);
            _mpb.SetFloat(GlowIntensityID, glowIntensity);
            _meshRenderer.SetPropertyBlock(_mpb);
        }
    }
}
