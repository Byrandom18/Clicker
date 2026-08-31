using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Clicker
{
    public class DamagePopupPool : MonoBehaviour
    {
        [SerializeField] DamagePopup prefab;
        [SerializeField] int prewarm = 16;
        [SerializeField] int maxActive = 40;
        [SerializeField] float fontSize = 40f;
        [SerializeField] Color color = new Color(1f, 0.86f, 0.22f, 1f);
        [SerializeField] float gravity = 1100f;
        [SerializeField] float speedYMin = 540f;
        [SerializeField] float speedYMax = 720f;
        [SerializeField] float spreadX = 160f;
        [SerializeField] float durationMin = 0.75f;
        [SerializeField] float durationMax = 0.95f;
        [SerializeField] Color megaColor = new Color(1f, 0.45f, 0.12f, 1f);
        [SerializeField] float megaScale = 1.7f;
        [SerializeField] float megaDurationMul = 1.45f;
        [SerializeField] float megaSpeedYMul = 1.15f;

        readonly Stack<DamagePopup> _inactive = new Stack<DamagePopup>();
        readonly List<DamagePopup> _active = new List<DamagePopup>();
        RectTransform _rect;
        Canvas _canvas;
        TMP_FontAsset _font;
        bool _warmed;

        void Awake()
        {
            Cache();
            EnsureReady();
        }

        public void Spawn(double amount, Vector2 screenPosition)
        {
            Spawn(amount, screenPosition, false);
        }

        public void SpawnMega(double amount, Vector2 screenPosition)
        {
            Spawn(amount, screenPosition, true);
        }

        void Spawn(double amount, Vector2 screenPosition, bool mega)
        {
            if (amount <= 0d)
                return;

            Cache();
            EnsureReady();
            if (_rect == null)
                return;

            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPosition, cam, out Vector2 local))
                local = Vector2.zero;

            var popup = Rent();
            if (popup == null)
                return;

            float speedY = UnityEngine.Random.Range(speedYMin, speedYMax);
            float duration = UnityEngine.Random.Range(durationMin, durationMax);
            Color tint = color;
            float scale = 1f;
            if (mega)
            {
                speedY *= megaSpeedYMul;
                duration *= megaDurationMul;
                tint = megaColor;
                scale = megaScale;
            }

            Vector2 velocity = new Vector2(UnityEngine.Random.Range(-spreadX, spreadX), speedY);
            popup.Play(NumberFormatter.Format(amount), local, velocity, gravity, duration, tint, scale, Release);
        }

        public static DamagePopupPool Create(Transform canvasRoot)
        {
            var go = new GameObject("DamagePopups", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.SetAsLastSibling();

            var victory = canvasRoot != null ? canvasRoot.GetComponentInChildren<VictoryView>(true) : null;
            if (victory != null && victory.transform.parent == canvasRoot)
                rt.SetSiblingIndex(victory.transform.GetSiblingIndex());

            var group = go.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = true;
            return go.AddComponent<DamagePopupPool>();
        }

        void Cache()
        {
            if (_rect == null)
                _rect = transform as RectTransform;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
            if (_font == null)
                _font = FindFont();
        }

        void EnsureReady()
        {
            if (_warmed)
                return;
            _warmed = true;
            int n = Mathf.Max(0, prewarm);
            for (int i = 0; i < n; i++)
                _inactive.Push(CreateItem());
        }

        DamagePopup Rent()
        {
            if (_inactive.Count == 0 && _active.Count >= Mathf.Max(1, maxActive))
                _active[0].Stop();

            DamagePopup popup = _inactive.Count > 0 ? _inactive.Pop() : CreateItem();
            if (popup == null)
                return null;

            _active.Add(popup);
            return popup;
        }

        void Release(DamagePopup popup)
        {
            if (popup == null)
                return;
            _active.Remove(popup);
            if (!popup.gameObject.activeSelf)
                _inactive.Push(popup);
        }

        DamagePopup CreateItem()
        {
            DamagePopup popup;
            if (prefab != null)
            {
                popup = Instantiate(prefab, transform);
                popup.gameObject.SetActive(false);
            }
            else
                popup = DamagePopup.Create(transform, _font, fontSize);

            return popup;
        }

        TMP_FontAsset FindFont()
        {
            if (prefab != null)
            {
                var prefabLabel = prefab.GetComponentInChildren<TMP_Text>(true);
                if (prefabLabel != null && prefabLabel.font != null)
                    return prefabLabel.font;
            }

            Transform root = _canvas != null ? _canvas.transform : transform;
            var existing = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].font != null)
                    return existing[i].font;
            }

            return TMP_Settings.defaultFontAsset;
        }
    }
}
