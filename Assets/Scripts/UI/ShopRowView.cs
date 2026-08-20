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

        public UpgradeDef Definition => definition;

        void Awake()
        {
            if (buy != null)
                buy.onClick.AddListener(HandleBuy);
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

            if (!unlocked)
            {
                if (cost != null)
                    cost.text = Loc.Locked;
                if (buyLabel != null)
                    buyLabel.text = Loc.Buy;
                if (buy != null)
                    buy.interactable = false;
                return;
            }

            double price = economy.GetCost(definition);
            if (cost != null)
                cost.text = NumberFormatter.Format(price);
            if (buyLabel != null)
                buyLabel.text = Loc.Buy;
            if (buy != null)
                buy.interactable = economy.CanAfford(definition);
        }

        void HandleBuy()
        {
            if (definition != null)
                BuyClicked?.Invoke(definition);
        }
    }
}
