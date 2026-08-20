using UnityEngine;

namespace Clicker
{
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Transform headAnchor;
        [SerializeField] EnemyDef definition;

        Texture2D _placeholderTex;
        Sprite _placeholderSprite;

        public EnemyDef Definition => definition;
        public Transform HeadAnchor => headAnchor != null ? headAnchor : transform;
        public SpriteRenderer Renderer => spriteRenderer;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sortingLayerName = "Characters";
        }

        void OnDestroy()
        {
            if (_placeholderSprite != null)
                Destroy(_placeholderSprite);
            if (_placeholderTex != null)
                Destroy(_placeholderTex);
        }

        public void SetDefinition(EnemyDef def)
        {
            definition = def;
        }

        public void ApplyStage(int stageIndex)
        {
            if (spriteRenderer == null)
                return;

            Sprite sprite = definition != null ? definition.GetSprite(stageIndex) : null;
            spriteRenderer.sprite = sprite != null ? sprite : GetPlaceholder();
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
