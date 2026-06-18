using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.Core.Pooling
{
    /// <summary>
    /// Generic object pool keyed by PoolKey. Plain-C# — not a service, not a singleton.
    /// Spawn returns a component implementing IPoolable; Despawn deactivates and returns it.
    /// Active-count caps are enforced by the calling service (e.g. CustomerService), not here.
    /// TODO: Add async prewarm for Addressables migration in Phase 4+.
    /// </summary>
    public sealed class PoolManager
    {
        private readonly IPrefabLoader _loader;
        private readonly Transform _poolRoot;
        private readonly Dictionary<PoolKey, Stack<GameObject>> _pools = new();

        public PoolManager(IPrefabLoader loader, Transform poolRoot)
        {
            _loader = loader;
            _poolRoot = poolRoot;
        }

        public void Prewarm(PoolKey key, int count)
        {
            var prefab = _loader.Load(key);
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] Prewarm skipped — prefab not found: '{key.ResourcePath}'");
                return;
            }

            if (!_pools.ContainsKey(key))
                _pools[key] = new Stack<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var go = Object.Instantiate(prefab, _poolRoot);
                go.SetActive(false);
                _pools[key].Push(go);
            }
            Debug.Log($"[PoolManager] Prewarmed {count}x '{key.ResourcePath}'");
        }

        public T Spawn<T>(PoolKey key, Vector3 pos, Quaternion rot, Transform parent = null)
            where T : Component, IPoolable
        {
            if (!_pools.TryGetValue(key, out var pool))
                pool = _pools[key] = new Stack<GameObject>();

            GameObject go;
            if (pool.Count > 0)
            {
                go = pool.Pop();
            }
            else
            {
                var prefab = _loader.Load(key);
                if (prefab == null)
                {
                    Debug.LogError($"[PoolManager] Cannot spawn — prefab not found: '{key.ResourcePath}'");
                    return null;
                }
                go = Object.Instantiate(prefab, _poolRoot);
            }

            go.transform.SetParent(parent != null ? parent : null);
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);

            var component = go.GetComponent<T>();
            component?.OnSpawned();
            return component;
        }

        public void Despawn<T>(PoolKey key, T instance) where T : Component, IPoolable
        {
            if (instance == null) return;
            instance.OnDespawned();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_poolRoot);

            if (!_pools.TryGetValue(key, out var pool))
                pool = _pools[key] = new Stack<GameObject>();
            pool.Push(instance.gameObject);
        }

        public void Clear(PoolKey key)
        {
            if (!_pools.TryGetValue(key, out var pool)) return;
            while (pool.Count > 0)
                Object.Destroy(pool.Pop());
            _pools.Remove(key);
        }
    }
}
