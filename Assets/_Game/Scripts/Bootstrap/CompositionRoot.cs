using DreamCafe.Core.EventBus;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.App
{
    /// <summary>
    /// Constructs and owns all core infrastructure: EventBus, PoolManager, ServiceManager.
    /// The only place every concrete type is known. Inject everything via ServiceContext — never expose as singletons.
    /// GameBootstrap holds the single reference to this; nothing else does.
    /// TODO: Support async Build() for remote-config or network-bound initialization.
    /// </summary>
    public sealed class CompositionRoot
    {
        private readonly EventBus _eventBus;
        private readonly ServiceManager _serviceManager;
        private readonly PoolManager _poolManager;

        public ServiceContext Context { get; private set; }

        public CompositionRoot(Transform poolRoot, IPrefabLoader prefabLoader)
        {
            _eventBus = new EventBus();
            _poolManager = new PoolManager(prefabLoader, poolRoot);
            _serviceManager = new ServiceManager();
            Debug.Log("[CompositionRoot] Infrastructure created.");
        }

        public void Build()
        {
            Context = new ServiceContext(_eventBus, _serviceManager, _poolManager);
            Debug.Log("[CompositionRoot] ServiceContext built.");
        }

        public ServiceManager Services => _serviceManager;
        public EventBus EventBus => _eventBus;
        public PoolManager Pool => _poolManager;

        public void Dispose()
        {
            _serviceManager.ShutdownAll();
            _eventBus.Clear();
            Debug.Log("[CompositionRoot] Disposed.");
        }
    }
}
