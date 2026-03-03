using UnityEngine;
using Reflex.Core;
using Reflex.Scoring;

namespace Reflex.Audio
{
    /// <summary>
    /// Plays sound effects for hits, misses, and tier-ups.
    /// Uses a pool of 4 AudioSources to prevent overlap clipping.
    /// Generates procedural placeholder tones when no clips are assigned.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Hit Sounds (leave empty for generated tones)")]
        [SerializeField] private AudioClip perfectClip;
        [SerializeField] private AudioClip greatClip;
        [SerializeField] private AudioClip goodClip;
        [SerializeField] private AudioClip justClip;
        [SerializeField] private AudioClip missClip;
        [SerializeField] private AudioClip tierUpClip;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.7f;

        private AudioSource[] _sources;
        private int _sourceIndex;
        private int _lastTier;

        // Generated fallback tones
        private AudioClip _genPerfect;
        private AudioClip _genGreat;
        private AudioClip _genGood;
        private AudioClip _genJust;
        private AudioClip _genMiss;
        private AudioClip _genTierUp;

        private void Awake()
        {
            // Create pooled AudioSources
            _sources = new AudioSource[4];
            for (int i = 0; i < _sources.Length; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f; // 2D
            }

            GenerateTones();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit += OnHit;
                GameManager.Instance.OnCircleMiss += OnMiss;
                GameManager.Instance.OnGameStart += OnGameStart;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCircleHit -= OnHit;
                GameManager.Instance.OnCircleMiss -= OnMiss;
                GameManager.Instance.OnGameStart -= OnGameStart;
            }
        }

        private void OnGameStart()
        {
            _lastTier = 0;
        }

        private void OnHit(HitQualityType quality, Vector2 pos)
        {
            AudioClip clip = quality switch
            {
                HitQualityType.Perfect => perfectClip != null ? perfectClip : _genPerfect,
                HitQualityType.Great   => greatClip   != null ? greatClip   : _genGreat,
                HitQualityType.Good    => goodClip    != null ? goodClip    : _genGood,
                HitQualityType.Just    => justClip    != null ? justClip    : _genJust,
                _                      => null
            };

            float volume = quality switch
            {
                HitQualityType.Perfect => 0.85f,
                HitQualityType.Great   => 0.75f,
                HitQualityType.Good    => 0.65f,
                HitQualityType.Just    => 0.55f,
                _                      => 0.4f
            };

            if (clip != null)
                PlayOneShot(clip, volume);

            // Check for tier promotion
            if (GameManager.Instance != null)
            {
                int tier = GameManager.Instance.GetStadiumTierIndex();
                if (tier > _lastTier)
                {
                    _lastTier = tier;
                    AudioClip tierClip = tierUpClip != null ? tierUpClip : _genTierUp;
                    if (tierClip != null)
                        PlayOneShot(tierClip, 0.9f);
                }
            }
        }

        private void OnMiss(Vector2 pos)
        {
            AudioClip clip = missClip != null ? missClip : _genMiss;
            if (clip != null)
                PlayOneShot(clip, 0.8f);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            AudioSource source = _sources[_sourceIndex];
            _sourceIndex = (_sourceIndex + 1) % _sources.Length;

            source.clip = clip;
            source.volume = volume * masterVolume;
            source.pitch = 1f;
            source.Play();
        }

        // ==================================================================
        //  Procedural tone generation — instant audio without asset files
        // ==================================================================

        private void GenerateTones()
        {
            const int rate = 44100;

            _genPerfect = CreateTone(rate, 0.15f, 880f, 1320f);   // Bright rising
            _genGreat   = CreateTone(rate, 0.12f, 740f, 1100f);
            _genGood    = CreateTone(rate, 0.10f, 660f, 880f);
            _genJust    = CreateTone(rate, 0.08f, 440f, 550f);    // Lower, softer
            _genMiss    = CreateBuzz(rate, 0.30f, 150f);           // Low harsh buzz
            _genTierUp  = CreateChime(rate, 0.40f);                // Rising arpeggio
        }

        /// <summary>Sine sweep with quick exponential decay — clean "tap" sound.</summary>
        private static AudioClip CreateTone(int sampleRate, float duration,
            float freqStart, float freqEnd)
        {
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(freqStart, freqEnd, t * 0.3f);
                float envelope = (1f - t) * (1f - t); // Quick decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate)
                           * envelope * 0.5f;
            }

            var clip = AudioClip.Create("GenTone", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Distorted low-freq buzz — "miss" sound.</summary>
        private static AudioClip CreateBuzz(int sampleRate, float duration, float freq)
        {
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float envelope = 1f - t;
                float phase = 2f * Mathf.PI * i / sampleRate;
                float s = Mathf.Sin(phase * freq) * 0.4f
                        + Mathf.Sin(phase * freq * 2.5f) * 0.2f
                        + Mathf.Sin(phase * freq * 4f) * 0.1f;
                samples[i] = s * envelope * 0.4f;
            }

            var clip = AudioClip.Create("GenBuzz", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Rising C-E-G-C arpeggio chime — "tier up" sound.</summary>
        private static AudioClip CreateChime(int sampleRate, float duration)
        {
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5 E5 G5 C6

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float s = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float noteStart = n * 0.08f;
                    float noteT = t - noteStart;
                    if (noteT > 0f)
                    {
                        float env = Mathf.Max(0f, 1f - noteT * 3f);
                        s += Mathf.Sin(2f * Mathf.PI * notes[n] * i / sampleRate)
                           * env * 0.2f;
                    }
                }

                samples[i] = s;
            }

            var clip = AudioClip.Create("GenChime", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
