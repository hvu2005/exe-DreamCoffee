using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub reputation service. Score starts at 1.0 and changes via AddReputation.
    /// ReputationLevel is a simple floor(score) integer for gate-keeping upgrades.
    /// TODO: Implement level thresholds, spawn-rate multipliers, VIP unlock gates.
    /// </summary>
    public sealed class ReputationService : IReputationService
    {
        public float ReputationScore { get; private set; } = 1f;
        public int ReputationLevel => Mathf.FloorToInt(ReputationScore);

        public void Init(ServiceContext ctx) => Debug.Log("[ReputationService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[ReputationService] Shutdown.");

        public void AddReputation(float delta)
        {
            ReputationScore = Mathf.Max(0f, ReputationScore + delta);
            Debug.Log($"[ReputationService] Reputation: {ReputationScore:F2} (level {ReputationLevel})");
        }
    }
}
