using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub analytics. Receives every event via the EventBus global listener set in GameBootstrap.
    /// Logs event type names to console. Replace with real SDK calls for production.
    /// TODO: Connect to Firebase Analytics / GameAnalytics, add event property serialisation.
    /// </summary>
    public sealed class AnalyticsService : IAnalyticsService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[AnalyticsService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[AnalyticsService] Shutdown.");
        public void Track(IEvent evt) => Debug.Log($"[Analytics] {evt.GetType().Name}");
    }
}
