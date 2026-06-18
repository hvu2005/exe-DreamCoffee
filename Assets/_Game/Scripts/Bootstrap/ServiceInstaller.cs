using DreamCafe.Core.Services;
using DreamCafe.Services.Crafting;
using DreamCafe.Services.Customer;
using DreamCafe.Services.Economy;
using DreamCafe.Services.Order;
using DreamCafe.Services.Stubs;
using DreamCafe.Services.Time;

namespace DreamCafe.App
{
    /// <summary>
    /// Registers all services in dependency/initialization order. Registration order = Init order.
    /// This is the only place new service bindings are added — never register elsewhere.
    /// TODO: Support build-configuration overrides (test doubles, platform-specific impls).
    /// </summary>
    public static class ServiceInstaller
    {
        public static void Install(ServiceManager manager)
        {
            // Core services — must init before dependents
            manager.Register<ITimeService>(new TimeService());
            manager.Register<IEconomyService>(new EconomyService());
            manager.Register<ICustomerService>(new CustomerService());
            manager.Register<IOrderService>(new OrderService());
            manager.Register<ICraftingService>(new CraftingService());

            // Stub services — registered after core; implement in later phases
            manager.Register<IStaffService>(new StaffService());
            manager.Register<IInventoryService>(new InventoryService());
            manager.Register<IDiscoveryService>(new DiscoveryService());
            manager.Register<IReputationService>(new ReputationService());
            manager.Register<ISoundService>(new SoundService());
            manager.Register<IAnalyticsService>(new AnalyticsService());
            manager.Register<ISaveService>(new SaveService());
        }
    }
}
