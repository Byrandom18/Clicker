using System;
using TMPro;
using UnityEngine;

namespace Clicker
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text shadow;

        RectTransform _rect;
        Vector2 _velocity;
        float _gravity;
        float _life;
        float _duration;
        Color _labelColor;
        Color _shadowColor;
        float _scale = 1f;
        Action<DamagePopup> _finished;
        bool _playing;

        public bool IsPlaying => _playing;

        void Awake()
        {
            Cache();
        }

        public void Play(
            string text,
            Vector2 anchoredPos,
            Vector2 velocity,
            float gravity,
            float duration,
            Color color,
            float scale,
            Action<DamagePopup> finished)
        {
            Cache();
            _finished = finished;
            _velocity = velocity;
            _gravity = gravity;
            _duration = Mathf.Max(0.05f, duration);
            _life = 0f;
            _playing = true;
            _labelColor = color;
            _shadowColor = new Color(0.08f, 0.05f, 0.02f, 0.75f);
            _scale = Mathf.Max(0.2f, scale);

            if (_rect != null)
            {
                _rect.anchoredPosition = anchoredPos;
                _rect.localScale = Vector3.one * _scale;
                _rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-8f, 8f));
            }

            ApplyText(text);
            ApplyAlpha(1f);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Stop()
        {
            if (!_playing)
                return;
            _playing = false;
            gameObject.SetActive(false);
            var done = _finished;
            _finished = null;
            done?.Invoke(this);
        }

        void Update()
        {
            if (!_playing)
                return;

            float dt = Time.deltaTime;
            _life += dt;
            _velocity.y -= _gravity * dt;
            if (_rect != null)
                _rect.anchoredPosition += _velocity * dt;

            float t = _life / _duration;
            if (t < 0.08f)
            {
                float s = Mathf.Lerp(0.72f, 1.08f, t / 0.08f) * _scale;
                if (_rect != null)
                    _rect.localScale = new Vector3(s, s, 1f);
            }
            else if (_rect != null)
                _rect.localScale = Vector3.one * _scale;

            if (t > 0.45f)
                ApplyAlpha(Mathf.Clamp01(1f - (t - 0.45f) / 0.55f));

            if (_life >= _duration)
                Stop();
        }

        void Cache()
        {
            if (_rect == null)
                _rect = transform as RectTransform;
            if (label == null && _rect != null)
            {
                var found = _rect.Find("Label");
                if (found != null)
                    label = found.GetComponent<TMP_Text>();
            }
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (shadow == null && _rect != null)
            {
                var found = _rect.Find("Shadow");
                if (found != null)
                    shadow = found.GetComponent<TMP_Text>();
            }
        }

        void ApplyText(string text)
        {
            if (label != null)
                label.text = text;
            if (shadow != null)
                shadow.text = text;
        }

        void ApplyAlpha(float a)
        {
            if (label != null)
            {
                Color c = _labelColor;
                c.a = _labelColor.a * a;
                label.color = c;
            }

            if (shadow != null)
            {
                Color c = _shadowColor;
                c.a = _shadowColor.a * a;
                shadow.color = c;
            }
        }

        public static DamagePopup Create(Transform parent, TMP_FontAsset font, float fontSize)
        {
            var go = new GameObject("DamagePopup", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 72f);
            rt.localScale = Vector3.one;

            var popup = go.AddComponent<DamagePopup>();
            popup.shadow = CreateLabel(rt, "Shadow", font, fontSize, new Color(0.08f, 0.05f, 0.02f, 0.75f), new Vector2(3f, -3f));
            popup.label = CreateLabel(rt, "Label", font, fontSize, Color.white, Vector2.zero);
            popup._rect = rt;
            go.SetActive(false);
            return popup;
        }

        static TMP_Text CreateLabel(Transform parent, string name, TMP_FontAsset font, float fontSize, Color color, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offset;
            rt.offsetMax = offset;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.text = "0";
            return tmp;
        }
    }
}
