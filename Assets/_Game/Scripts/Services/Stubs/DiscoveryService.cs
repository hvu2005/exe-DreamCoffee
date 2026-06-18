using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub discovery service. Always fails to discover (returns false).
    /// TODO: Implement ingredient combination matching, research log, discovery event.
    /// </summary>
    public sealed class DiscoveryService : IDiscoveryService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[DiscoveryService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[DiscoveryService] Shutdown.");
        public bool TryDiscover(string[] ingredientIds, out string discoveredItemId)
        {
            discoveredItemId = null;
            return false;
        }
    }
}
