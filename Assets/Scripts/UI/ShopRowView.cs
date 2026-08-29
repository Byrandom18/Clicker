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

        public UpgradeDef Definition => definition;

        public void SetDefinition(UpgradeDef def)
        {
            definition = def;
        }

        void Awake()
        {
            if (buy != null)
                buy.onClick.AddListener(HandleBuy);
            if (power != null)
            {
                power.enableAutoSizing = true;
                power.fontSizeMin = 14f;
                power.fontSizeMax = 22f;
                power.textWrappingMode = TextWrappingModes.Normal;
            }

            EnsureLockOverlay();
            ApplyLockOverlayColor();
            SetLocked(false);
        }

        void OnValidate()
        {
            ApplyLockOverlayColor();
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
                cost.text = NumberFormatter.Format(price);
            if (buyLabel != null)
                buyLabel.text = Loc.Buy;
            if (buy != null)
                buy.interactable = unlocked && economy.CanAfford(definition);
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

        void EnsureLockOverlay()
        {
            if (lockOverlay != null)
            {
                if (lockLabel == null)
                    lockLabel = lockOverlay.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            Transform existing = transform.Find("LockOverlay");
            if (existing != null)
            {
                lockOverlay = existing.gameObject;
                lockLabel = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            var overlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            overlay.layer = gameObject.layer;
            var ignore = overlay.GetComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            overlay.transform.SetParent(transform, false);

            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
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
