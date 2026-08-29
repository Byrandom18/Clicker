using DG.Tweening;
using UnityEngine;

namespace Clicker
{
    public class EnemyView : MonoBehaviour
    {
        static readonly Vector3 RestScale = Vector3.one;
        const float PunchPeak = 1.08f;
        const float PunchUp = 0.05f;
        const float PunchDown = 0.08f;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Transform headAnchor;
        [SerializeField] EnemyDef definition;

        Texture2D _placeholderTex;
        Sprite _placeholderSprite;
        Tween _clickPunch;

        public EnemyDef Definition => definition;
        public Transform HeadAnchor => headAnchor != null ? headAnchor : transform;
        public SpriteRenderer Renderer => spriteRenderer;

        public Vector3 WorldCenter
        {
            get
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    return spriteRenderer.bounds.center;
                return transform.position + Vector3.up;
            }
        }

        public Vector3 WorldSize
        {
            get
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    return spriteRenderer.bounds.size;
                return new Vector3(2.2f, 3.2f, 0f);
            }
        }

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sortingLayerName = "Characters";
        }

        void OnDestroy()
        {
            KillClickPunch();
            if (_placeholderSprite != null)
                Destroy(_placeholderSprite);
            if (_placeholderTex != null)
                Destroy(_placeholderTex);
        }

        public void SetDefinition(EnemyDef def)
        {
            definition = def;
        }

        public void BindRenderer(SpriteRenderer renderer, Transform head)
        {
            spriteRenderer = renderer;
            headAnchor = head;
        }

        public void ApplyStage(int stageIndex)
        {
            if (spriteRenderer == null)
                return;

            Sprite sprite = definition != null ? definition.GetSprite(stageIndex) : null;
            spriteRenderer.sprite = sprite != null ? sprite : GetPlaceholder();
        }

        public void SetSpriteVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
        }

        public void PlayClickPunch()
        {
            KillClickPunch();
            transform.localScale = RestScale;
            _clickPunch = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject)
                .Append(transform.DOScale(RestScale * PunchPeak, PunchUp))
                .Append(transform.DOScale(RestScale, PunchDown));
        }

        public void KillClickPunch()
        {
            if (_clickPunch != null && _clickPunch.IsActive())
                _clickPunch.Kill();
            _clickPunch = null;
            if (this != null)
                transform.localScale = RestScale;
        }

        Sprite GetPlaceholder()
        {
            if (_placeholderSprite != null)
                return _placeholderSprite;

            _placeholderTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color color = definition != null ? definition.placeholderColor : Color.magenta;
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            _placeholderTex.SetPixels(pixels);
            _placeholderTex.Apply();
            _placeholderTex.filterMode = FilterMode.Point;
            _placeholderSprite = Sprite.Create(_placeholderTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f);
            return _placeholderSprite;
        }
    }
}
