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
        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _barLeft;
        RectTransform _barRight;
        RectTransform _barTop;
        RectTransform _barBottom;
        float _baseOrthoSize = 5f;

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
            _baseOrthoSize = _cam.orthographicSize > 0.01f ? _cam.orthographicSize : 5f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Fill;
            _cam.rect = new Rect(0f, 0f, 1f, 1f);
            _cam.depthTextureMode = DepthTextureMode.None;
#if UNITY_WEBGL && !UNITY_EDITOR
            _cam.allowHDR = false;
#endif
            DestroyNamedChild(transform, "LetterboxBackground");
            FitUi();
            Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        void OnDisable()
        {
            if (_cam == null)
                return;
            _cam.rect = new Rect(0f, 0f, 1f, 1f);
            _cam.orthographicSize = _baseOrthoSize;
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

        static void DestroyNamedChild(Transform parent, string name)
        {
            Transform old = parent.Find(name);
            if (old != null)
                Destroy(old.gameObject);
        }

        void FitUi()
        {
            if (_canvas == null)
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
            DestroyNamedChild(root, "LetterboxFill");

            Transform content = root.Find("LetterboxContent");
            if (content == null)
            {
                var contentGo = new GameObject("LetterboxContent", typeof(RectTransform), typeof(AspectRatioFitter));
                var contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.SetParent(root, false);
                Stretch(contentRt);

                var fitter = contentGo.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = TargetAspect;

                var toMove = new List<Transform>();
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child != contentRt && !IsBar(child.name))
                        toMove.Add(child);
                }

                for (int i = 0; i < toMove.Count; i++)
                    toMove[i].SetParent(contentRt, false);

                content = contentRt;
            }

            _barLeft = EnsureBar(root, "LetterboxLeft");
            _barRight = EnsureBar(root, "LetterboxRight");
            _barTop = EnsureBar(root, "LetterboxTop");
            _barBottom = EnsureBar(root, "LetterboxBottom");
            content.SetAsLastSibling();
        }

        static bool IsBar(string name)
        {
            return name == "LetterboxLeft" || name == "LetterboxRight"
                || name == "LetterboxTop" || name == "LetterboxBottom";
        }

        static RectTransform EnsureBar(Transform root, string name)
        {
            Transform existing = root.Find(name);
            if (existing != null)
                return existing as RectTransform;

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.SetAsFirstSibling();

            var image = go.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = Fill;
            image.raycastTarget = false;
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetBar(RectTransform rt, float xMin, float yMin, float xMax, float yMax, bool on)
        {
            if (rt == null)
                return;
            rt.gameObject.SetActive(on);
            if (!on)
                return;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            if (w < 1 || h < 1 || _cam == null)
                return;

            float windowAspect = (float)w / h;
            float letterboxH;
            if (windowAspect >= TargetAspect)
            {
                _cam.orthographicSize = _baseOrthoSize;
                letterboxH = h;
                float bar = (1f - TargetAspect / windowAspect) * 0.5f;
                SetBar(_barLeft, 0f, 0f, bar, 1f, bar > 0.0001f);
                SetBar(_barRight, 1f - bar, 0f, 1f, 1f, bar > 0.0001f);
                SetBar(_barTop, 0f, 0f, 1f, 1f, false);
                SetBar(_barBottom, 0f, 0f, 1f, 1f, false);
            }
            else
            {
                _cam.orthographicSize = _baseOrthoSize * TargetAspect / windowAspect;
                letterboxH = w / TargetAspect;
                float bar = (1f - windowAspect / TargetAspect) * 0.5f;
                SetBar(_barTop, 0f, 1f - bar, 1f, 1f, bar > 0.0001f);
                SetBar(_barBottom, 0f, 0f, 1f, bar, bar > 0.0001f);
                SetBar(_barLeft, 0f, 0f, 1f, 1f, false);
                SetBar(_barRight, 0f, 0f, 1f, 1f, false);
            }

            _cam.rect = new Rect(0f, 0f, 1f, 1f);
            _cam.backgroundColor = Fill;

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
