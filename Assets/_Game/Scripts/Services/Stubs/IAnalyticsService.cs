using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Game event analytics. Wired as the EventBus global listener — receives every published event.
    /// Stub logs to console; replace with real SDK calls in production.
    /// TODO: Connect to Firebase Analytics / GameAnalytics / custom backend.
    /// </summary>
    public interface IAnalyticsService : IService
    {
        void Track(IEvent evt);
    }
}
