using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clicker
{
    public class UiButtonScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] bool breath;
        [SerializeField] float breathStrength = 0.08f;
        [SerializeField] float breathIntensity = 1.2f;
        [SerializeField] float hoverScale = 1.06f;
        [SerializeField] float pressScale = 0.94f;
        [SerializeField] float tweenSpeed = 12f;

        Button _button;
        Vector3 _rest = Vector3.one;
        bool _hover;
        bool _press;
        float _hoverT;
        float _pressT;

        public void SetBreath(bool enabled, float strength, float intensity)
        {
            breath = enabled;
            breathStrength = Mathf.Max(0f, strength);
            breathIntensity = Mathf.Max(0f, intensity);
        }

        public static UiButtonScaleFeedback Ensure(Button button)
        {
            if (button == null)
                return null;
            var feedback = button.GetComponent<UiButtonScaleFeedback>();
            if (feedback == null)
                feedback = button.gameObject.AddComponent<UiButtonScaleFeedback>();
            return feedback;
        }

        public static void EnsureAll()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
                Ensure(buttons[i]);
        }

        void Awake()
        {
            _button = GetComponent<Button>();
            _rest = transform.localScale;
            if (_rest.sqrMagnitude < 0.0001f)
                _rest = Vector3.one;
        }

        void OnDisable()
        {
            _hover = false;
            _press = false;
            _hoverT = 0f;
            _pressT = 0f;
            transform.localScale = _rest;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float step = tweenSpeed * dt;
            _hoverT = Mathf.MoveTowards(_hoverT, _hover ? 1f : 0f, step);
            _pressT = Mathf.MoveTowards(_pressT, _press ? 1f : 0f, step);

            float breathMul = 1f;
            if (breath && breathStrength > 0f && isActiveAndEnabled)
                breathMul = 1f + breathStrength * Mathf.Sin(Time.unscaledTime * breathIntensity * Mathf.PI * 2f);

            float hoverMul = Mathf.Lerp(1f, hoverScale, _hoverT);
            float pressMul = Mathf.Lerp(1f, pressScale, _pressT);
            transform.localScale = _rest * (breathMul * hoverMul * pressMul);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanReact())
                return;
            _hover = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            _press = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanReact())
                return;
            _press = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _press = false;
        }

        bool CanReact()
        {
            return _button == null || _button.interactable;
        }
    }
}
