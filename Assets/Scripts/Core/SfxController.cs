using UnityEngine;

namespace Clicker
{
    public class SfxController : MonoBehaviour
    {
        static SfxController _instance;

        [Header("Click")]
        [SerializeField] AudioClip[] clickClips;
        [SerializeField, Range(0f, 1f)] float clickVolume = 0.38f;
        [SerializeField, Range(0.8f, 1.2f)] float clickPitchMin = 0.98f;
        [SerializeField, Range(0.8f, 1.2f)] float clickPitchMax = 1.02f;
        [SerializeField, Range(1, 12)] int clickVoices = 6;

        [Header("Buy")]
        [SerializeField] AudioClip buyClip;
        [SerializeField, Range(0f, 1f)] float buyVolume = 0.74f;

        [Header("Phase")]
        [SerializeField] AudioClip phaseClip;
        [SerializeField, Range(0f, 1f)] float phaseVolume = 0.86f;

        [Header("Rewarded attack")]
        [SerializeField] AudioClip explosionClip;
        [SerializeField, Range(0f, 1f)] float explosionVolume = 0.92f;

        [Header("UI")]
        [SerializeField] AudioClip uiClip;
        [SerializeField, Range(0f, 1f)] float uiVolume = 0.4f;
        [SerializeField, Range(0f, 0.2f)] float uiDebounce = 0.04f;

        AudioSource _oneShot;
        AudioSource[] _voices;
        int _voiceIndex;
        int _lastClick = -1;
        float _lastUiUnscaled = -1f;

        public static SfxController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<SfxController>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolveClips();
            EnsureSources();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void PlayClick()
        {
            if (clickClips == null || clickClips.Length == 0 || _voices == null || _voices.Length == 0)
                return;
            var src = _voices[_voiceIndex++ % _voices.Length];
            if (src == null)
                return;
            float min = Mathf.Min(clickPitchMin, clickPitchMax);
            float max = Mathf.Max(clickPitchMin, clickPitchMax);
            src.pitch = Random.Range(min, max);
            src.clip = NextClickClip();
            src.volume = clickVolume;
            src.Play();
        }

        public void PlayBuy()
        {
            PlayOneShot(buyClip, buyVolume);
        }

        public void PlayPhase()
        {
            PlayOneShot(phaseClip, phaseVolume);
        }

        public void PlayExplosion()
        {
            PlayOneShot(explosionClip, explosionVolume);
        }

        public void PlayUi()
        {
            float now = Time.unscaledTime;
            if (now - _lastUiUnscaled < uiDebounce)
                return;
            _lastUiUnscaled = now;
            PlayOneShot(uiClip, uiVolume);
        }

        void ResolveClips()
        {
            if (clickClips == null || clickClips.Length == 0)
                clickClips = ClickerCatalog.LoadClickSfx();
            if (buyClip == null)
                buyClip = ClickerCatalog.LoadSfx("buy");
            if (phaseClip == null)
                phaseClip = ClickerCatalog.LoadSfx("phase");
            if (explosionClip == null)
                explosionClip = ClickerCatalog.LoadSfx("explosion");
            if (uiClip == null)
                uiClip = ClickerCatalog.LoadSfx("ui");
        }

        void EnsureSources()
        {
            if (_oneShot == null)
                _oneShot = GetOrAddSource("OneShot");
            int voices = Mathf.Max(1, clickVoices);
            if (_voices == null || _voices.Length != voices)
            {
                _voices = new AudioSource[voices];
                for (int i = 0; i < voices; i++)
                    _voices[i] = GetOrAddSource("ClickVoice" + i);
            }
        }

        AudioClip NextClickClip()
        {
            int n = clickClips.Length;
            if (n == 1)
            {
                _lastClick = 0;
                return clickClips[0];
            }

            int i = Random.Range(0, n);
            if (i == _lastClick)
                i = (i + 1 + Random.Range(0, n - 1)) % n;
            _lastClick = i;
            return clickClips[i];
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || _oneShot == null)
                return;
            _oneShot.pitch = 1f;
            _oneShot.PlayOneShot(clip, volume);
        }

        AudioSource GetOrAddSource(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            var src = child.GetComponent<AudioSource>();
            if (src == null)
                src = child.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.priority = 128;
            return src;
        }
    }
}
