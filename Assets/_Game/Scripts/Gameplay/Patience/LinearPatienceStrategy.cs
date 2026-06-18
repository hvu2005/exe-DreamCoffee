using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.Patience
{
    /// <summary>
    /// Drains patience at a constant rate per second, slowed by reputation.
    /// VIP customers drain slowest; Students drain fastest.
    /// Rates are tunable — consider moving to a ScriptableObject in Phase 4.
    /// </summary>
    public sealed class LinearPatienceStrategy : IPatienceStrategy
    {
        private readonly float _dataRate; // 0 = use type defaults

        /// <param name="patienceSeconds">From CustomerData.patienceSeconds. 0 = fall back to type defaults.</param>
        public LinearPatienceStrategy(float patienceSeconds = 0f)
        {
            _dataRate = patienceSeconds > 0f ? 1f / patienceSeconds : 0f;
        }

        public float Drain(float current, float dt, CustomerType type, float reputationFactor)
        {
            var rate = (_dataRate > 0f ? _dataRate : GetTypeDrainRate(type))
                       / Mathf.Max(0.1f, reputationFactor);
            return Mathf.Clamp01(current - rate * dt);
        }

        private static float GetTypeDrainRate(CustomerType type) => type switch
        {
            CustomerType.VIP        => 1f / 60f,
            CustomerType.Influencer => 1f / 45f,
            CustomerType.Tourist    => 1f / 35f,
            CustomerType.Worker     => 1f / 30f,
            CustomerType.Student    => 1f / 25f,
            _                       => 1f / 30f
        };
    }
}
