using System.Collections.Generic;
using CartoonFX;
using UnityEngine;

namespace Clicker
{
    public class HitVfxPool : MonoBehaviour
    {
        public const string SortingLayer = "VFX";
        public const int SortingOrder = 12;

        [SerializeField] GameObject prefab;
        [SerializeField] int prewarm = 8;
        [SerializeField] int maxActive = 16;
        [SerializeField] float defaultScale = 0.45f;
        [SerializeField] bool allowCameraShake;
        [SerializeField] bool randomizeRotation = true;

        readonly Stack<PooledHitVfx> _inactive = new Stack<PooledHitVfx>();
        readonly List<PooledHitVfx> _active = new List<PooledHitVfx>();
        bool _warmed;

        public void Play(Vector3 worldPos, float scale)
        {
            if (prefab == null)
                return;
            EnsureReady();

            if (_inactive.Count == 0 && _active.Count >= Mathf.Max(1, maxActive))
                _active[0].ForceStop();

            var item = _inactive.Count > 0 ? _inactive.Pop() : CreateItem();
            if (item == null)
                return;

            _active.Add(item);
            float s = scale > 0f ? scale : defaultScale;
            item.Play(worldPos, s, randomizeRotation);
        }

        public void Release(PooledHitVfx item)
        {
            if (item == null)
                return;
            _active.Remove(item);
            _inactive.Push(item);
        }

        public static HitVfxPool Create(string name, Transform parent, GameObject prefab, int prewarm, float scale, bool allowCameraShake, bool randomizeRotation)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            var pool = go.AddComponent<HitVfxPool>();
            pool.prefab = prefab;
            pool.prewarm = prewarm;
            pool.maxActive = Mathf.Max(prewarm, 4);
            pool.defaultScale = scale;
            pool.allowCameraShake = allowCameraShake;
            pool.randomizeRotation = randomizeRotation;
            return pool;
        }

        void Awake()
        {
            EnsureReady();
        }

        void EnsureReady()
        {
            if (_warmed || prefab == null)
                return;
            _warmed = true;
            int n = Mathf.Max(0, prewarm);
            for (int i = 0; i < n; i++)
                _inactive.Push(CreateItem());
        }

        PooledHitVfx CreateItem()
        {
            var go = Instantiate(prefab, new Vector3(0f, -2000f, 0f), Quaternion.identity, transform);
            go.name = prefab.name;
            var item = PooledHitVfx.Attach(go, this, allowCameraShake);
            go.SetActive(false);
            return item;
        }
    }

    public class PooledHitVfx : MonoBehaviour
    {
        HitVfxPool _pool;
        ParticleSystem[] _systems;
        CFXR_Effect[] _effects;
        bool _rented;

        public static PooledHitVfx Attach(GameObject go, HitVfxPool pool, bool allowCameraShake)
        {
            var item = go.GetComponent<PooledHitVfx>();
            if (item == null)
                item = go.AddComponent<PooledHitVfx>();
            item._pool = pool;
            item._systems = go.GetComponentsInChildren<ParticleSystem>(true);
            item._effects = go.GetComponentsInChildren<CFXR_Effect>(true);

            for (int i = 0; i < item._effects.Length; i++)
            {
                var fx = item._effects[i];
                if (fx == null)
                    continue;
                fx.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
                if (!allowCameraShake && fx.cameraShake != null)
                    fx.cameraShake.enabled = false;
            }

            VfxPresentation.Prepare(go, HitVfxPool.SortingLayer, HitVfxPool.SortingOrder);

            return item;
        }

        public void Play(Vector3 worldPos, float scale, bool randomSpin)
        {
            _rented = true;
            Quaternion rot = randomSpin
                ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                : Quaternion.identity;
            transform.SetPositionAndRotation(VfxPresentation.Place(worldPos), rot);
            transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            gameObject.SetActive(true);

            if (_effects != null)
            {
                for (int i = 0; i < _effects.Length; i++)
                {
                    if (_effects[i] != null)
                        _effects[i].ResetState();
                }
            }

            if (_systems == null)
                return;
            for (int i = 0; i < _systems.Length; i++)
            {
                var ps = _systems[i];
                if (ps == null)
                    continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        public void ForceStop()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (!_rented)
                return;
            _rented = false;
            if (_pool != null)
                _pool.Release(this);
        }
    }
}
