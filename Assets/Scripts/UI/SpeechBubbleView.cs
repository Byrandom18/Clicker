using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class SpeechBubbleView : MonoBehaviour
    {
        public event Action ContinueClicked;

        [SerializeField] TMP_Text body;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [SerializeField] RectTransform rect;
        [SerializeField] RectTransform canvasRect;
        [SerializeField] Camera worldCamera;

        Transform _follow;
        Canvas _canvas;

        void Awake()
        {
            if (rect == null)
                rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinue);
        }

        void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinue);
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
                body.text = text;
            if (continueLabel != null)
                continueLabel.text = Loc.Continue;
            gameObject.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            _follow = null;
            gameObject.SetActive(false);
        }

        void HandleContinue()
        {
            ContinueClicked?.Invoke();
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
            rect.anchoredPosition = local + new Vector2(0f, 90f);
        }
    }
}
