using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clicker
{
    public class HoldPulse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action pulse;
        bool _held;
        float _timer;

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            _timer = 0f;
            pulse?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => _held = false;
        public void OnPointerExit(PointerEventData eventData) => _held = false;

        void Update()
        {
            if (!_held)
                return;
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 0.38f)
            {
                pulse?.Invoke();
                _timer = 0.28f;
            }
        }
    }

    public class ShopRowView : MonoBehaviour
    {
        public Text title;
        public Text detail;
        public Button buyButton;
        public Text buyLabel;

        public int ShopIndex { get; private set; }

        public void Setup(int shopIndex, Action<int> onBuy)
        {
            ShopIndex = shopIndex;
            if (buyButton == null)
                return;
            var hold = buyButton.GetComponent<HoldPulse>();
            if (hold == null)
                hold = buyButton.gameObject.AddComponent<HoldPulse>();
            hold.pulse = () => onBuy?.Invoke(ShopIndex);
            buyButton.onClick.RemoveAllListeners();
        }

        public void Render(UpgradeDef def, bool unlocked, int owned, double cost, bool canBuy, bool maxed)
        {
            if (def == null)
                return;
            if (title != null)
                title.text = def.DisplayName + (def.isIdle ? "  ·  " + Loc.IdlePower : "  ·  " + Loc.ClickPower);
            if (detail != null)
                detail.text = Loc.PlusPower(def.powerPerCopy, def.isIdle) + "  ·  " + owned + " " + Loc.Owned;
            if (buyButton != null)
                buyButton.interactable = unlocked && canBuy && !maxed;
            if (buyLabel == null)
                return;
            if (!unlocked)
            {
                buyLabel.fontSize = 13;
                buyLabel.text = Loc.Locked;
            }
            else if (maxed)
            {
                buyLabel.fontSize = 16;
                buyLabel.text = Loc.Maxed;
            }
            else
            {
                buyLabel.fontSize = 16;
                buyLabel.text = Loc.Buy + "\n" + NumberFormatter.Format(cost);
            }
        }
    }
}
