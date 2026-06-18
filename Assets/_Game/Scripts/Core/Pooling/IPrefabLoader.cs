using UnityEngine;

namespace DreamCafe.Core.Pooling
{
    /// <summary>
    /// Abstraction over prefab loading strategy.
    /// Swap ResourcesPrefabLoader for AddressablesPrefabLoader without changing callers.
    /// </summary>
    public interface IPrefabLoader
    {
        GameObject Load(PoolKey key);
    }
}
