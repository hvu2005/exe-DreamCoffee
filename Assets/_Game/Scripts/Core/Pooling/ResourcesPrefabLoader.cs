using UnityEngine;

namespace DreamCafe.Core.Pooling
{
    /// <summary>
    /// Synchronous Resources.Load-based prefab loader. Suitable for prototyping with a small number of prefabs.
    /// Prefabs must live under Assets/_Game/Resources/ matching the path in PoolKey.
    /// TODO: Phase 4+ — swap for AddressablesPrefabLoader if asset memory or load-time regresses.
    /// </summary>
    public sealed class ResourcesPrefabLoader : IPrefabLoader
    {
        public GameObject Load(PoolKey key) => Resources.Load<GameObject>(key.ResourcePath);
    }
}
