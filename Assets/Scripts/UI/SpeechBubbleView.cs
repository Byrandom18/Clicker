using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class SpeechBubbleView : MonoBehaviour
    {
        [SerializeField] TMP_Text body;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [SerializeField] RectTransform rect;
        [SerializeField] RectTransform canvasRect;
        [SerializeField] Camera worldCamera;
        [Tooltip("Смещение пузыря от головы противника в пикселях Canvas. X > 0 — справа.")]
        [SerializeField] Vector2 followOffset = new Vector2(160f, 0f);

        Transform _follow;
        Canvas _canvas;
        Graphic _graphic;

        void Awake()
        {
            if (rect == null)
                rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _graphic = GetComponent<Graphic>();
            HideContinueButton();
        }

        void LateUpdate()
        {
            if (!gameObject.activeInHierarchy || _follow == null)
                return;
            UpdatePosition();
        }

        public void Show(string text, Transform follow)
        {
            _follow = follow;
            if (body != null)
            {
                body.text = text;
                body.raycastTarget = false;
            }
            HideContinueButton();
            if (_graphic != null)
                _graphic.raycastTarget = false;
            gameObject.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            _follow = null;
            gameObject.SetActive(false);
        }

        void HideContinueButton()
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            if (continueLabel != null)
                continueLabel.gameObject.SetActive(false);
        }

        void UpdatePosition()
        {
            if (rect == null || _follow == null)
                return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Vector3 screen = cam != null ? cam.WorldToScreenPoint(_follow.position) : Input.mousePosition;
            RectTransform parent = canvasRect != null ? canvasRect : rect.parent as RectTransform;
            if (parent == null)
                return;

            Camera overlayCam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, overlayCam, out Vector2 local);
            rect.anchoredPosition = local + followOffset;
        }
    }
}
