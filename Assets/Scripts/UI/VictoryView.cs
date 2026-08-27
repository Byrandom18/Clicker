using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class VictoryView : MonoBehaviour
    {
        public event Action ContinueClicked;

        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;

        bool _wired;

        void Awake()
        {
            EnsureContinue();
        }

        void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinue);
        }

        public void Show(string bodyText)
        {
            EnsureContinue();
            if (title != null)
                title.text = Loc.VictoryTitle;
            if (body != null)
                body.text = string.IsNullOrEmpty(bodyText) ? Loc.VictoryBody : bodyText;
            if (continueLabel != null)
                continueLabel.text = Loc.ContinueEndless;
            gameObject.SetActive(true);
            if (continueButton != null)
                continueButton.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void HandleContinue()
        {
            ContinueClicked?.Invoke();
        }

        void EnsureContinue()
        {
            if (continueButton == null)
            {
                var found = transform.Find("ContinueButton");
                if (found != null)
                    continueButton = found.GetComponent<Button>();
            }

            if (continueButton == null)
                CreateContinueButton();

            if (continueLabel == null && continueButton != null)
                continueLabel = continueButton.GetComponentInChildren<TMP_Text>(true);

            if (_wired || continueButton == null)
                return;

            continueButton.onClick.AddListener(HandleContinue);
            _wired = true;
        }

        void CreateContinueButton()
        {
            var go = new GameObject("ContinueButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(420f, 84f);
            rt.anchoredPosition = new Vector2(0f, 48f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.83f, 1f, 0f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            continueButton = button;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.layer = 5;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 42f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            tmp.raycastTarget = false;
            tmp.text = Loc.ContinueEndless;
            continueLabel = tmp;
        }
    }
}
