using System;

namespace DreamCafe.Core.Pooling
{
    /// <summary>
    /// Typed key for the PoolManager. Wraps the Resources path so call sites use constants, not strings.
    /// Add new pool entries here when new pooled prefabs are introduced.
    /// </summary>
    public readonly struct PoolKey : IEquatable<PoolKey>
    {
        public readonly string ResourcePath;

        public static readonly PoolKey Customer       = new("Prefabs/CustomerPrefab");
        public static readonly PoolKey OrderTicket    = new("Prefabs/OrderTicketPrefab");
        public static readonly PoolKey CraftingStation = new("Prefabs/CraftingStationPrefab");

        public PoolKey(string resourcePath) => ResourcePath = resourcePath;

        public bool Equals(PoolKey other) => ResourcePath == other.ResourcePath;
        public override bool Equals(object obj) => obj is PoolKey key && Equals(key);
        public override int GetHashCode() => ResourcePath?.GetHashCode() ?? 0;
        public static bool operator ==(PoolKey l, PoolKey r) => l.Equals(r);
        public static bool operator !=(PoolKey l, PoolKey r) => !l.Equals(r);
        public override string ToString() => ResourcePath;
    }
}
