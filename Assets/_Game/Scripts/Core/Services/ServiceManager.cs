using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.Core.Services
{
    /// <summary>
    /// Ordered service registry. Registration order determines Init order; Shutdown runs in reverse.
    /// Register by interface type so consumers depend on abstractions (DIP).
    /// TODO: Support configuration-driven service overrides for testing.
    /// </summary>
    public sealed class ServiceManager : IServiceResolver
    {
        private readonly Dictionary<Type, IService> _registry = new();
        private readonly List<IService> _ordered = new();

        public void Register<T>(T service) where T : class, IService
        {
            _registry[typeof(T)] = service;
            _ordered.Add(service);
        }

        public T Resolve<T>() where T : class, IService
        {
            if (_registry.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"[ServiceManager] Service not registered: {typeof(T).Name}");
        }

        public bool TryResolve<T>(out T service) where T : class, IService
        {
            if (_registry.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            service = null;
            return false;
        }

        public void Unregister<T>() where T : class, IService => _registry.Remove(typeof(T));

        public void InitAll(ServiceContext ctx)
        {
            foreach (var service in _ordered)
            {
                service.Init(ctx);
            }
            Debug.Log($"[ServiceManager] InitAll complete — {_ordered.Count} services.");
        }

        public void ShutdownAll()
        {
            for (int i = _ordered.Count - 1; i >= 0; i--)
                _ordered[i].Shutdown();
            Debug.Log("[ServiceManager] ShutdownAll complete.");
        }
    }
}
