using System.Collections;
using UnityEngine;

namespace Clicker
{
    public class MusicController : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip clip;
        [SerializeField, Range(0f, 1f)] float volume = 0.35f;
        [SerializeField] bool loop = true;
        [SerializeField] bool playOnStart;
        [SerializeField] float fadeInDuration = 3f;

        Coroutine _fadeRoutine;

        void Awake()
        {
            EnsureSource();
            ApplySettings();
        }

        void Start()
        {
            if (playOnStart)
                Play();
        }

        void OnDisable()
        {
            StopFade();
        }

        void OnValidate()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source == null)
                return;
            ApplySettings();
        }

        public void SetMuted(bool muted)
        {
            EnsureSource();
            source.mute = muted;
            if (muted)
                StopFade();
        }

        public void Play(bool fadeIn = false)
        {
            EnsureSource();
            bool wasPlaying = source.isPlaying;
            bool willFade = fadeIn || !wasPlaying;
            ApplySettings(!willFade);
            if (source.clip == null)
                return;
            if (!wasPlaying)
                source.Play();
            if (willFade)
                StartFadeIn();
        }

        public void Stop()
        {
            StopFade();
            if (source != null)
                source.Stop();
        }

        void EnsureSource()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
        }

        void ApplySettings(bool applyVolume = true)
        {
            if (clip == null)
                clip = ClickerCatalog.LoadSfx("music");
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = 64;
            source.ignoreListenerVolume = true;
            if (applyVolume && _fadeRoutine == null)
                source.volume = volume;
            if (clip != null)
                source.clip = clip;
        }

        void StartFadeIn()
        {
            StopFade();
            if (!isActiveAndEnabled)
            {
                source.volume = volume;
                return;
            }

            _fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        void StopFade()
        {
            if (_fadeRoutine == null)
                return;
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        IEnumerator FadeInRoutine()
        {
            float duration = Mathf.Max(0f, fadeInDuration);
            if (duration <= 0f)
            {
                source.volume = volume;
                _fadeRoutine = null;
                yield break;
            }

            source.volume = 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                source.volume = volume * Mathf.Clamp01(t / duration);
                yield return null;
            }

            source.volume = volume;
            _fadeRoutine = null;
        }
    }
}
