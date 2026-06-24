using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using DreamCafe.Services.Customer;
using DreamCafe.Services.Economy;
using DreamCafe.Services.Time;
using UnityEngine;

namespace DreamCafe.Services.Upgrade
{
    /// <summary>
    /// Tracks upgrade purchase levels and applies their effects to other services.
    /// Loads UpgradeConfig from Resources/UpgradeConfig.asset on Init.
    /// Effects are re-applied on every DayStarted so they survive day cycling.
    /// Purchase deducts from IEconomyService; TipMultiplier + PatienceMultiplier are
    /// read lazily by OrderService and CustomerController respectively.
    /// </summary>
    public sealed class UpgradeService : IUpgradeService
    {
        private ServiceContext                  _ctx;
        private IEventBus                       _events;
        private UpgradeConfig                   _config;
        private readonly Dictionary<string,int> _levels = new();

        // ── IService ─────────────────────────────────────────────────────────

        public void Init(ServiceContext ctx)
        {
            _ctx    = ctx;
            _events = ctx.Events;
            _config = Resources.Load<UpgradeConfig>("UpgradeConfig");
            if (_config == null)
                Debug.LogWarning("[UpgradeService] Resources/UpgradeConfig.asset not found — run Phase 6 setup.");
            _events.Subscribe<DayStarted>(OnDayStarted);
            Debug.Log("[UpgradeService] Initialized.");
        }

        public void Shutdown()
        {
            _events?.Unsubscribe<DayStarted>(OnDayStarted);
            _levels.Clear();
            Debug.Log("[UpgradeService] Shutdown.");
        }

        // ── IUpgradeService ──────────────────────────────────────────────────

        public int GetLevel(string id) => _levels.TryGetValue(id, out var l) ? l : 0;

        public bool IsMaxed(string id)
        {
            var data = GetData(id);
            return data != null && GetLevel(id) >= data.maxLevel;
        }

        public float NextCost(string id)
        {
            var data = GetData(id);
            if (data == null || IsMaxed(id)) return -1f;
            return data.CostAtLevel(GetLevel(id));
        }

        public bool Purchase(string id)
        {
            var data = GetData(id);
            if (data == null || IsMaxed(id)) return false;

            var cost = data.CostAtLevel(GetLevel(id));
            var econ = _ctx.Services.Resolve<IEconomyService>();
            if (!econ.SpendMoney(cost))
            {
                Debug.Log($"[UpgradeService] Purchase {id} failed — insufficient balance.");
                return false;
            }

            _levels[id] = GetLevel(id) + 1;
            _events.Publish(new UpgradePurchased(id, _levels[id]));
            ApplyOverrides();
            Debug.Log($"[UpgradeService] {data.displayName} → Level {_levels[id]}.");
            return true;
        }

        // ── Effect properties ────────────────────────────────────────────────

        public float TipMultiplier        => GetEffectValue("quality_roast", 1f);
        public float PatienceMultiplier   => GetEffectValue("cozy_vibes",    1f);
        public float SpawnIntervalOverride => GetEffectValue("rush_hour",    0f);
        public float DayLengthOverride     => GetEffectValue("long_day",     0f);

        // ── Private ──────────────────────────────────────────────────────────

        private void OnDayStarted(DayStarted _) => ApplyOverrides();

        private void ApplyOverrides()
        {
            var spawnOverride = SpawnIntervalOverride;
            if (spawnOverride > 0f && _ctx.Services.TryResolve<ICustomerService>(out var cust))
                cust.SpawnIntervalSeconds = spawnOverride;

            var dayOverride = DayLengthOverride;
            if (dayOverride > 0f && _ctx.Services.TryResolve<ITimeService>(out var time))
                time.DayLengthSeconds = dayOverride;
        }

        private float GetEffectValue(string id, float defaultValue)
        {
            var level = GetLevel(id);
            if (level == 0) return defaultValue;
            var data = GetData(id);
            if (data?.effectValues == null || level > data.effectValues.Length) return defaultValue;
            return data.effectValues[level - 1];
        }

        private UpgradeData GetData(string id)
        {
            if (_config == null) return null;
            foreach (var u in _config.upgrades)
                if (u != null && u.upgradeId == id) return u;
            return null;
        }
    }
}
