using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clicker
{
    public class BattleZoneClick : MonoBehaviour, IPointerDownHandler
    {
        public static event Action<Vector2> Pressed;

        public bool ClicksEnabled { get; private set; } = true;

        Graphic _graphic;

        void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        public void SetClicksEnabled(bool on)
        {
            ClicksEnabled = on;
            if (_graphic != null)
                _graphic.raycastTarget = on;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!ClicksEnabled)
                return;
            Pressed?.Invoke(eventData.position);
        }
    }
}
