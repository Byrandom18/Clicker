using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class AspectLetterbox : MonoBehaviour
    {
        public const float TargetAspect = 16f / 9f;
        public const float ReferenceHeight = 1080f;
        public static readonly Color Fill = new Color(42f / 255f, 26f / 255f, 56f / 255f, 1f);

        Camera _cam;
        Camera _bg;
        Canvas _canvas;
        CanvasScaler _scaler;
        bool _uiFitted;

        public static void Ensure()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            if (cam.GetComponent<AspectLetterbox>() == null)
                cam.gameObject.AddComponent<AspectLetterbox>();
        }

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Fill;
            EnsureBackgroundCamera();
            FitUi();
            Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        void OnDisable()
        {
            if (_cam != null)
                _cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        void LateUpdate()
        {
            Apply();
        }

        void OnPreCull()
        {
            FitUi();
            Apply();
        }

        void EnsureBackgroundCamera()
        {
            if (_bg != null)
                return;

            var go = new GameObject("LetterboxBackground");
            go.transform.SetParent(transform, false);
            _bg = go.AddComponent<Camera>();
            _bg.CopyFrom(_cam);
            _bg.cullingMask = 0;
            _bg.clearFlags = CameraClearFlags.SolidColor;
            _bg.backgroundColor = Fill;
            _bg.depth = _cam.depth - 1;
            _bg.rect = new Rect(0f, 0f, 1f, 1f);
            _bg.allowHDR = false;
            _bg.allowMSAA = false;
        }

        void FitUi()
        {
            if (_uiFitted && _canvas != null)
                return;

            _canvas = FindRootCanvas();
            if (_canvas == null)
                return;

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = null;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 100;
            _scaler = _canvas.GetComponent<CanvasScaler>();
            if (_scaler != null)
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Transform root = _canvas.transform;
            if (root.Find("LetterboxContent") != null)
            {
                _uiFitted = true;
                return;
            }

            var content = new GameObject("LetterboxContent", typeof(RectTransform), typeof(AspectRatioFitter));
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.SetParent(root, false);
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.pivot = new Vector2(0.5f, 0.5f);

            var fitter = content.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = TargetAspect;

            var toMove = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != contentRt)
                    toMove.Add(child);
            }

            for (int i = 0; i < toMove.Count; i++)
                toMove[i].SetParent(contentRt, false);

            contentRt.SetAsLastSibling();
            _uiFitted = true;
        }

        static Canvas FindRootCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && canvas.isRootCanvas)
                    return canvas;
            }

            return null;
        }

        void Apply()
        {
            int w = Screen.width;
            int h = Screen.height;
            if (w < 1 || h < 1)
                return;

            float windowAspect = (float)w / h;
            Rect rect;
            float letterboxH;
            if (windowAspect > TargetAspect)
            {
                float inset = TargetAspect / windowAspect;
                rect = new Rect((1f - inset) * 0.5f, 0f, inset, 1f);
                letterboxH = h;
            }
            else
            {
                float inset = windowAspect / TargetAspect;
                rect = new Rect(0f, (1f - inset) * 0.5f, 1f, inset);
                letterboxH = w / TargetAspect;
            }

            if (_cam != null)
            {
                _cam.rect = rect;
                _cam.backgroundColor = Fill;
            }

            if (_bg != null)
            {
                _bg.rect = new Rect(0f, 0f, 1f, 1f);
                _bg.backgroundColor = Fill;
                _bg.depth = _cam != null ? _cam.depth - 1 : -2;
            }

            if (_canvas != null)
            {
                float scale = Mathf.Max(0.01f, letterboxH / ReferenceHeight);
                if (_scaler != null)
                    _scaler.scaleFactor = scale;
                else
                    _canvas.scaleFactor = scale;
            }
        }
    }
}
