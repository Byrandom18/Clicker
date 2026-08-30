using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class ShopRowView : MonoBehaviour
    {
        public static event Action<UpgradeDef> BuyClicked;

        [SerializeField] UpgradeDef definition;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text power;
        [SerializeField] TMP_Text owned;
        [SerializeField] TMP_Text cost;
        [SerializeField] Button buy;
        [SerializeField] TMP_Text buyLabel;
        [SerializeField] GameObject lockOverlay;
        [SerializeField] TMP_Text lockLabel;
        [SerializeField, Range(0f, 1f)] float lockOverlayAlpha = 0.78f;
        [SerializeField] float lockOverlayHeight = 95f;

        static readonly Color AffordTop = new Color(0f, 1f, 188f / 255f, 1f);
        static readonly Color AffordBottom = new Color(63f / 255f, 1f, 0f, 1f);
        static readonly Color PoorTop = new Color(1f, 80f / 255f, 0f, 1f);
        static readonly Color PoorBottom = new Color(1f, 0f, 54f / 255f, 1f);

        public UpgradeDef Definition => definition;
        public Button BuyButton => buy;

        public void PlayBuyFeedback()
        {
            var feedback = buy != null ? buy.GetComponent<UiButtonScaleFeedback>() : null;
            if (feedback != null)
                feedback.PlayPressPulse();
        }

        public void SetDefinition(UpgradeDef def)
        {
            definition = def;
        }

        void FitInContent()
        {
            var le = GetComponent<LayoutElement>();
            if (le == null)
                le = gameObject.AddComponent<LayoutElement>();
            le.minWidth = 0f;
            le.minHeight = lockOverlayHeight;
            le.preferredHeight = lockOverlayHeight;
            le.flexibleWidth = 1f;
            le.layoutPriority = 100;
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();
        }

        void Awake()
        {
            if (buy != null)
            {
                buy.onClick.AddListener(HandleBuy);
                UiButtonScaleFeedback.Ensure(buy);
            }
            if (power != null)
            {
                power.enableAutoSizing = true;
                power.fontSizeMin = 14f;
                power.fontSizeMax = 22f;
                power.textWrappingMode = TextWrappingModes.Normal;
            }

            FitInContent();
            EnsureLockOverlay();
            ApplyLockOverlayColor();
            SetLocked(false);
        }

        void OnValidate()
        {
            ApplyLockOverlayColor();
            if (lockOverlay != null)
                ApplyLockOverlayLayout(lockOverlay.transform as RectTransform);
        }

        void OnDestroy()
        {
            if (buy != null)
                buy.onClick.RemoveListener(HandleBuy);
        }

        public void Bind(EconomyService economy)
        {
            if (definition == null || economy == null)
            {
                if (buy != null)
                    buy.interactable = false;
                SetLocked(false);
                return;
            }

            if (title != null)
                title.text = definition.DisplayName;
            if (power != null)
                power.text = Loc.PlusPower(definition.powerPerCopy, definition.kind == UpgradeKind.Idle);
            if (icon != null)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }

            bool unlocked = economy.IsUnlocked(definition);
            int count = economy.GetCount(definition);
            if (owned != null)
                owned.text = unlocked ? $"×{count}" : string.Empty;

            SetLocked(!unlocked);

            double price = economy.GetCost(definition);
            if (cost != null)
            {
                cost.text = NumberFormatter.Format(price);
                ApplyCostColor(economy.Score >= price);
            }
            if (buyLabel != null)
                buyLabel.text = Loc.Buy;
            if (buy != null)
                buy.interactable = unlocked && economy.CanAfford(definition);
        }

        void ApplyCostColor(bool canAfford)
        {
            if (cost == null)
                return;
            Color top = canAfford ? AffordTop : PoorTop;
            Color bottom = canAfford ? AffordBottom : PoorBottom;
            cost.color = Color.white;
            cost.enableVertexGradient = true;
            cost.colorGradient = new VertexGradient(top, top, bottom, bottom);
        }

        void SetLocked(bool locked)
        {
            EnsureLockOverlay();
            ApplyLockOverlayColor();
            if (lockLabel != null)
                lockLabel.text = Loc.Locked;
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(locked);
                if (locked)
                    lockOverlay.transform.SetAsLastSibling();
            }
        }

        void ApplyLockOverlayColor()
        {
            if (lockOverlay == null)
                return;
            var dim = lockOverlay.GetComponent<Image>();
            if (dim == null)
                return;
            dim.color = new Color(0.02f, 0.02f, 0.04f, lockOverlayAlpha);
        }

        void ApplyLockOverlayLayout(RectTransform overlayRt)
        {
            if (overlayRt == null)
                return;
            overlayRt.anchorMin = new Vector2(0f, 0.5f);
            overlayRt.anchorMax = new Vector2(1f, 0.5f);
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.sizeDelta = new Vector2(0f, lockOverlayHeight);
            overlayRt.anchoredPosition = Vector2.zero;
        }

        void EnsureLockOverlay()
        {
            if (lockOverlay != null)
            {
                if (lockLabel == null)
                    lockLabel = lockOverlay.GetComponentInChildren<TMP_Text>(true);
                ApplyLockOverlayLayout(lockOverlay.transform as RectTransform);
                return;
            }

            Transform existing = transform.Find("LockOverlay");
            if (existing != null)
            {
                lockOverlay = existing.gameObject;
                lockLabel = existing.GetComponentInChildren<TMP_Text>(true);
                ApplyLockOverlayLayout(existing as RectTransform);
                return;
            }

            var overlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            overlay.layer = gameObject.layer;
            var ignore = overlay.GetComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            overlay.transform.SetParent(transform, false);

            var overlayRt = overlay.GetComponent<RectTransform>();
            ApplyLockOverlayLayout(overlayRt);
            overlayRt.SetAsLastSibling();

            var dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.02f, 0.02f, 0.04f, lockOverlayAlpha);
            dim.raycastTarget = true;
            var parentImage = GetComponent<Image>();
            if (parentImage != null && parentImage.sprite != null)
            {
                dim.sprite = parentImage.sprite;
                dim.type = parentImage.type;
                dim.pixelsPerUnitMultiplier = parentImage.pixelsPerUnitMultiplier;
            }

            var labelGo = new GameObject("LockLabel", typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.layer = gameObject.layer;
            labelGo.transform.SetParent(overlay.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 8f);
            labelRt.offsetMax = new Vector2(-16f, -8f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 26f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            TMP_FontAsset font = title != null ? title.font : null;
            if (font == null && cost != null)
                font = cost.font;
            if (font != null)
                label.font = font;
            label.text = Loc.Locked;

            lockOverlay = overlay;
            lockLabel = label;
        }

        void HandleBuy()
        {
            if (definition != null)
                BuyClicked?.Invoke(definition);
        }
    }
}
