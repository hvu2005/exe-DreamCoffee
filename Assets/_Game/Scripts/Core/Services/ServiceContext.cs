using DreamCafe.Core.EventBus;
using DreamCafe.Core.Pooling;

namespace DreamCafe.Core.Services
{
    /// <summary>
    /// Dependency bag passed to every IService.Init() and IController.Bind().
    /// The only sanctioned way to access cross-cutting infrastructure — no static globals.
    /// TODO: Add cancellation token for async shutdown coordination.
    /// </summary>
    public sealed class ServiceContext
    {
        public IEventBus Events { get; }
        public IServiceResolver Services { get; }
        public PoolManager Pool { get; }

        public ServiceContext(IEventBus events, IServiceResolver services, PoolManager pool)
        {
            Events = events;
            Services = services;
            Pool = pool;
        }
    }
}
