using System;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using DreamCafe.Core.Utils;
using UnityEngine;

namespace DreamCafe.Core.MVC
{
    /// <summary>
    /// Base for all prefab Controller MonoBehaviours.
    /// Manages EventBus subscription lifecycle via DisposableBag — all subs auto-cleared on Unbind.
    /// TODO: Add context validation in debug builds.
    /// </summary>
    public abstract class ControllerBase : MonoBehaviour, IController
    {
        protected ServiceContext Ctx { get; private set; }
        protected readonly DisposableBag Bag = new();

        public virtual void Bind(ServiceContext ctx) => Ctx = ctx;

        public virtual void Unbind()
        {
            Bag.Dispose();
            Ctx = null;
        }

        /// <summary>Convenience wrapper: subscribes and registers auto-unsubscribe in the Bag.</summary>
        protected void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (Ctx == null) return;
            var events = Ctx.Events;
            events.Subscribe(handler);
            Bag.Add(() => events.Unsubscribe(handler));
        }

        public virtual void OnSpawned() { }

        public virtual void OnDespawned() => Unbind();
    }
}
