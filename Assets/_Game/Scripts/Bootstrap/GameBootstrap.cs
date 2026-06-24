using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using DreamCafe.Gameplay.CraftingStation;
using DreamCafe.Gameplay.Order;
using DreamCafe.Gameplay.Table;
using DreamCafe.Services.Customer;
using DreamCafe.Services.Stubs;
using DreamCafe.Services.Time;
using DreamCafe.UI.Hud;
using UnityEngine;

namespace DreamCafe.App
{
    /// <summary>
    /// Scene entry point and composition root owner. One per scene.
    /// Creates all infrastructure, registers services, wires cross-cutting hooks, starts Day 1.
    /// Ticks time-sensitive services in Update with deterministic order.
    /// TODO Phase 4: support scene transition with context hand-off.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform  poolRoot;
        [SerializeField] private Transform  customerSpawnPoint;
        [SerializeField] private CafeConfig cafeConfig;

        private CompositionRoot     _root;
        private ITimeService        _timeService;
        private ICustomerService    _customerService;

        private void Awake()
        {
            if (poolRoot == null)
            {
                var go = new GameObject("[PoolRoot]");
                go.transform.SetParent(transform);
                poolRoot = go.transform;
            }

            // 1. Build composition root
            _root = new CompositionRoot(poolRoot, new ResourcesPrefabLoader());

            // 2. Register all services
            ServiceInstaller.Install(_root.Services);

            // 3. Build ServiceContext and init all services
            _root.Build();
            _root.Services.InitAll(_root.Context);

            var ctx = _root.Context;

            // 4. Wire global analytics
            var analytics = ctx.Services.Resolve<IAnalyticsService>();
            ctx.Events.SetGlobalListener(analytics.Track);

            // 5. Wire sound hooks
            var sound = ctx.Services.Resolve<ISoundService>();
            ctx.Events.Subscribe<OrderServed>(evt => sound.Play("serve_ding"));
            ctx.Events.Subscribe<ItemCrafted>(_ => sound.Play("craft_complete"));

            // 6. Prewarm object pools
            ctx.Pool.Prewarm(PoolKey.Customer,     10);
            ctx.Pool.Prewarm(PoolKey.OrderTicket,  10);

            // 7. Bind scene controllers
            BindSceneControllers(ctx);

            // 8. Wire customer spawn point
            _customerService = ctx.Services.Resolve<ICustomerService>();
            _customerService.SetSpawnPoint(
                customerSpawnPoint != null ? customerSpawnPoint.position : Vector3.zero);

            // 9. Cache per-frame services and apply balance tuning
            _timeService = ctx.Services.Resolve<ITimeService>();

            if (cafeConfig == null)
                cafeConfig = Resources.Load<CafeConfig>("CafeConfig");

            if (cafeConfig != null)
            {
                _customerService.SpawnIntervalSeconds = cafeConfig.spawnIntervalSeconds;
                _timeService.DayLengthSeconds         = cafeConfig.dayLengthSeconds;
                Debug.Log($"[GameBootstrap] CafeConfig applied — spawn: {cafeConfig.spawnIntervalSeconds}s, day: {cafeConfig.dayLengthSeconds}s.");
            }

            // 10. Start Day 1
            _timeService.StartDay();

            Debug.Log("[GameBootstrap] ✓ Startup complete.");
        }

        private void Update()
        {
            _timeService?.Tick(Time.deltaTime);
            _customerService?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            foreach (var ctrl in FindObjectsByType<ControllerBase>(FindObjectsSortMode.None))
                ctrl.Unbind();
            _root?.Dispose();
            Debug.Log("[GameBootstrap] Shutdown complete.");
        }

        private static void BindSceneControllers(ServiceContext ctx)
        {
            // Tables register with ICustomerService
            foreach (var t in FindObjectsByType<TableController>(FindObjectsSortMode.None))
                t.Bind(ctx);

            // Crafting stations register with ICraftingService
            foreach (var s in FindObjectsByType<CraftingStationController>(FindObjectsSortMode.None))
                s.Bind(ctx);

            // Order ticket spawner subscribes to OrderPlaced
            foreach (var sp in FindObjectsByType<OrderTicketSpawner>(FindObjectsSortMode.None))
                sp.Bind(ctx);

            // HUD subscribes to economy/customer/time events
            foreach (var h in FindObjectsByType<HudController>(FindObjectsSortMode.None))
                h.Bind(ctx);
        }
    }
}
