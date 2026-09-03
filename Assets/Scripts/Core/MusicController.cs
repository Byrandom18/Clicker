using UnityEngine;

namespace Clicker
{
    public class MusicController : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip clip;
        [SerializeField, Range(0f, 1f)] float volume = 0.35f;
        [SerializeField] bool loop = true;
        [SerializeField] bool playOnStart = true;

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
        }

        public void Play()
        {
            EnsureSource();
            ApplySettings();
            if (source.clip == null)
                return;
            if (!source.isPlaying)
                source.Play();
        }

        public void Stop()
        {
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

        void ApplySettings()
        {
            if (clip == null)
                clip = ClickerCatalog.LoadSfx("music");
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = 64;
            source.ignoreListenerVolume = true;
            source.volume = volume;
            if (clip != null)
                source.clip = clip;
        }
    }
}
