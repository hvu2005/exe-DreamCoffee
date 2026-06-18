using System;

namespace DreamCafe.Core.EventBus
{
    /// <summary>
    /// Contract for the game-wide event bus. All inter-system communication routes through here.
    /// SetGlobalListener wires an analytics/tracking callback that fires on every Publish call.
    /// TODO: Add priority levels and deferred/batched dispatch modes.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : IEvent;
        void Unsubscribe<T>(Action<T> handler) where T : IEvent;
        void Publish<T>(T evt) where T : IEvent;
        void SetGlobalListener(Action<IEvent> listener);
        void Clear();
    }
}
