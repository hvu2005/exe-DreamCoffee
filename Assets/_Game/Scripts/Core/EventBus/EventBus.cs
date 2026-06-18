using System;
using System.Collections.Generic;
using DreamCafe.Core.Utils;

namespace DreamCafe.Core.EventBus
{
    /// <summary>
    /// Type-keyed, re-entrant-safe event dispatcher. Instance-owned — never static.
    /// Per-handler exception isolation via SafeInvoke. Global listener fires on every Publish.
    /// TODO: Add deferred/queued dispatch and filter predicates.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();
        private Action<IEvent> _globalListener;

        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing)) return;
            var updated = Delegate.Remove(existing, handler);
            if (updated == null) _handlers.Remove(type);
            else _handlers[type] = updated;
        }

        public void Publish<T>(T evt) where T : IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var combined))
            {
                var snapshot = combined.GetInvocationList();
                foreach (var d in snapshot)
                    SafeInvoke.Call((Action<T>)d, evt);
            }
            _globalListener?.Invoke(evt);
        }

        public void SetGlobalListener(Action<IEvent> listener) => _globalListener = listener;

        public void Clear()
        {
            _handlers.Clear();
            _globalListener = null;
        }
    }
}
