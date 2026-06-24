using DreamCafe.Core.Services;

namespace DreamCafe.Services.Upgrade
{
    /// <summary>
    /// Tracks purchased upgrade levels and exposes computed effect properties.
    /// Services that are affected (OrderService, CustomerService, TimeService) resolve this lazily.
    /// </summary>
    public interface IUpgradeService : IService
    {
        /// <summary>Current purchase level; 0 = not bought.</summary>
        int GetLevel(string upgradeId);

        /// <summary>True when level == maxLevel.</summary>
        bool IsMaxed(string upgradeId);

        /// <summary>Đồng cost to reach the next level; -1 if maxed or upgrade not found.</summary>
        float NextCost(string upgradeId);

        /// <summary>Deducts balance and increments level. Returns false if maxed or insufficient funds.</summary>
        bool Purchase(string upgradeId);

        // ── Computed effect properties ────────────────────────────────────────

        /// <summary>Tip multiplier from "quality_roast" upgrade (default 1.0).</summary>
        float TipMultiplier { get; }

        /// <summary>ReputationFactor multiplier for customer patience from "cozy_vibes" (default 1.0).</summary>
        float PatienceMultiplier { get; }

        /// <summary>SpawnIntervalSeconds override from "rush_hour"; 0 = no override.</summary>
        float SpawnIntervalOverride { get; }

        /// <summary>DayLengthSeconds override from "long_day"; 0 = no override.</summary>
        float DayLengthOverride { get; }
    }
}
