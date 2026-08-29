using DG.Tweening;
using UnityEngine;

namespace Clicker
{
    public static class StageChangeVfx
    {
        const string SortingLayer = "VFX";
        const int BurstOrder = 25;
        const int CoverOrder = 20;

        static Sprite _blob;
        static Texture2D _blobTex;

        public static float ScaleFor(Vector3 worldSize)
        {
            float size = Mathf.Max(worldSize.x, worldSize.y);
            return Mathf.Clamp(size * 0.5f, 1.15f, 2.4f);
        }

        public static GameObject SpawnBurst(GameObject prefab, Vector3 worldPos, float scale)
        {
            if (prefab == null)
                return null;

            var go = Object.Instantiate(prefab, worldPos, Quaternion.identity);
            go.transform.localScale = Vector3.one * scale;

            var renderers = go.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                renderer.sortingLayerName = SortingLayer;
                renderer.sortingOrder = BurstOrder;
            }

            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null)
                    continue;
                var main = ps.main;
                main.useUnscaledTime = true;
                if (!ps.isPlaying)
                    ps.Play(true);
            }

            return go;
        }

        public static GameObject SpawnCover(
            Vector3 worldPos, Vector3 worldSize, float fadeIn, float hold, float fadeOut,
            float sizeMul, float opacity)
        {
            float mul = Mathf.Max(0.05f, sizeMul);
            var go = new GameObject("StageCover");
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(
                Mathf.Max(1.6f, worldSize.x * 1.45f) * mul,
                Mathf.Max(2.2f, worldSize.y * 1.45f) * mul,
                1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Blob();
            sr.color = new Color(0.88f, 0.9f, 0.94f, 0f);
            sr.sortingLayerName = SortingLayer;
            sr.sortingOrder = CoverOrder;

            fadeIn = Mathf.Max(0.01f, fadeIn);
            fadeOut = Mathf.Max(0.01f, fadeOut);
            hold = Mathf.Max(0f, hold);
            opacity = Mathf.Clamp01(opacity);

            DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(go)
                .Append(sr.DOFade(opacity, fadeIn))
                .AppendInterval(hold)
                .Append(sr.DOFade(0f, fadeOut))
                .OnComplete(() =>
                {
                    if (go != null)
                        Object.Destroy(go);
                });

            return go;
        }

        static Sprite Blob()
        {
            if (_blob != null)
                return _blob;

            const int size = 64;
            _blobTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _blobTex.filterMode = FilterMode.Bilinear;
            _blobTex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((0.98f - d) / 0.72f);
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            _blobTex.SetPixels(pixels);
            _blobTex.Apply(false, false);
            _blob = Sprite.Create(_blobTex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _blob.name = "StageCoverBlob";
            return _blob;
        }
    }
}
