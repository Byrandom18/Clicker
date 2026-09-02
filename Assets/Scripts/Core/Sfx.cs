using UnityEngine;

namespace Clicker
{
    public static class Sfx
    {
        const int ClickVoices = 6;
        const float ClickVolume = 0.62f;
        const float BuyVolume = 0.74f;
        const float PhaseVolume = 0.86f;
        const float ExplosionVolume = 0.92f;
        const float UiVolume = 0.4f;
        const float UiDebounce = 0.04f;

        static AudioSource _oneShot;
        static AudioSource[] _voices;
        static int _voiceIndex;
        static AudioClip[] _clickHits;
        static int _lastClick = -1;
        static AudioClip _buy;
        static AudioClip _phase;
        static AudioClip _explosion;
        static AudioClip _ui;
        static float _lastUiUnscaled = -1f;

        public static void Bind(
            Transform parent,
            AudioClip click,
            AudioClip buy,
            AudioClip phase,
            AudioClip explosion,
            AudioClip ui)
        {
            _clickHits = CollectClicks(click);
            _buy = buy != null ? buy : ClickerCatalog.LoadSfx("buy");
            _phase = phase != null ? phase : ClickerCatalog.LoadSfx("phase");
            _explosion = explosion != null ? explosion : ClickerCatalog.LoadSfx("explosion");
            _ui = ui != null ? ui : ClickerCatalog.LoadSfx("ui");

            if (parent == null)
                return;

            if (_oneShot == null)
            {
                var root = new GameObject("Sfx");
                root.transform.SetParent(parent, false);
                _oneShot = MakeSource(root);
                _voices = new AudioSource[ClickVoices];
                for (int i = 0; i < ClickVoices; i++)
                    _voices[i] = MakeSource(root);
            }
        }

        public static void Release(Transform parent)
        {
            if (_oneShot == null || parent == null)
                return;
            if (_oneShot.transform != null && _oneShot.transform.parent == parent)
            {
                _oneShot = null;
                _voices = null;
                _voiceIndex = 0;
                _lastClick = -1;
            }
        }

        public static void Click()
        {
            if (_clickHits == null || _clickHits.Length == 0 || _voices == null || _voices.Length == 0)
                return;
            var src = _voices[_voiceIndex++ % _voices.Length];
            if (src == null)
                return;
            src.pitch = Random.Range(0.96f, 1.05f);
            src.clip = NextClick();
            src.volume = ClickVolume;
            src.Play();
        }

        static AudioClip NextClick()
        {
            int n = _clickHits.Length;
            int i = 0;
            if (n == 1)
                i = 0;
            else
            {
                i = Random.Range(0, n);
                if (i == _lastClick)
                    i = (i + 1 + Random.Range(0, n - 1)) % n;
            }

            _lastClick = i;
            return _clickHits[i];
        }

        static AudioClip[] CollectClicks(AudioClip assigned)
        {
            var loaded = ClickerCatalog.LoadClickSfx();
            if (assigned == null)
                return loaded;
            if (loaded == null || loaded.Length == 0)
                return new[] { assigned };

            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] == assigned)
                    return loaded;
            }

            var mixed = new AudioClip[loaded.Length + 1];
            mixed[0] = assigned;
            for (int i = 0; i < loaded.Length; i++)
                mixed[i + 1] = loaded[i];
            return mixed;
        }

        public static void Buy()
        {
            PlayOneShot(_buy, BuyVolume);
        }

        public static void Phase()
        {
            PlayOneShot(_phase, PhaseVolume);
        }

        public static void Explosion()
        {
            PlayOneShot(_explosion, ExplosionVolume);
        }

        public static void Ui()
        {
            float now = Time.unscaledTime;
            if (now - _lastUiUnscaled < UiDebounce)
                return;
            _lastUiUnscaled = now;
            PlayOneShot(_ui, UiVolume);
        }

        static void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || _oneShot == null)
                return;
            _oneShot.pitch = 1f;
            _oneShot.PlayOneShot(clip, volume);
        }

        static AudioSource MakeSource(GameObject root)
        {
            var src = root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.priority = 128;
            return src;
        }
    }
}
