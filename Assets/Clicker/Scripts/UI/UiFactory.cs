using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Clicker
{
    public static class UiFactory
    {
        public static Font Font { get; private set; }
        public static Sprite White { get; private set; }

        public static readonly Color Bg = Hex("1A1428");
        public static readonly Color PanelColor = Hex("2A2140");
        public static readonly Color PanelAlt = Hex("352A52");
        public static readonly Color Accent = Hex("F0C14A");
        public static readonly Color Text = Hex("F5F0E6");
        public static readonly Color Muted = Hex("8A8299");
        public static readonly Color Hp = Hex("E74C3C");
        public static readonly Color HpBg = Hex("3A2030");
        public static readonly Color ButtonColor = Hex("5B4A9A");
        public static readonly Color ButtonDown = Hex("47367A");
        public static readonly Color Green = Hex("3ECF8E");
        public static readonly Color Danger = Hex("D4576A");

        public static void Init()
        {
            if (White == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.Apply();
                White = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }

            if (Font == null)
            {
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (Font == null)
                    Font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (Font == null)
                    Font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 16);
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var go = new GameObject("EventSystem");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Root(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var rt = Root(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = White;
            image.color = color;
            image.raycastTarget = true;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = White;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(string name, Transform parent, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, string caption, int fontSize = 22)
        {
            var image = Image(name, parent, ButtonColor);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            button.colors = colors;
            button.targetGraphic = image;

            var label = Label("Label", image.transform, fontSize, Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.text = caption;
            label.raycastTarget = false;
            image.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.35f);
            return button;
        }

        public static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static Sprite BodySprite(Color color, int stage, int width = 180, int height = 240)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float dark = 1f - stage * 0.16f;
            Color fill = Color.Lerp(color, Color.black, 0.12f + stage * 0.12f) * dark;
            fill.a = 1f;
            Color outline = Color.Lerp(fill, Color.black, 0.45f);
            Color eye = new Color(0.12f, 0.08f, 0.1f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - width * 0.5f) / (width * 0.36f);
                    float ny = (y - height * 0.38f) / (height * 0.40f);
                    float body = nx * nx + ny * ny;
                    float hx = (x - width * 0.5f) / (width * 0.22f);
                    float hy = (y - height * 0.72f) / (height * 0.16f);
                    float head = hx * hx + hy * hy;
                    Color pixel = Color.clear;
                    if (body < 1.08f || head < 1.08f)
                        pixel = outline;
                    if (body < 1f || head < 1f)
                        pixel = fill;
                    float ex = (x - width * 0.42f) / 8f;
                    float ey = (y - height * 0.74f) / 8f;
                    if (ex * ex + ey * ey < 1f)
                        pixel = eye;
                    ex = (x - width * 0.58f) / 8f;
                    if (ex * ex + ey * ey < 1f)
                        pixel = eye;
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.12f), 100f);
        }
    }
}
