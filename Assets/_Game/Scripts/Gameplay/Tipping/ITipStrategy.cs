using DreamCafe.Data;

namespace DreamCafe.Gameplay.Tipping
{
    /// <summary>
    /// Strategy for computing tip amount based on service quality and customer type.
    /// Swap implementations to test different reward curves without changing callers.
    /// TODO: Add reputation-level multiplier and combo-streak bonus variant.
    /// </summary>
    public interface ITipStrategy
    {
        /// <param name="basePrice">Item base price</param>
        /// <param name="patienceRemaining">0..1 — higher = happier = better tip</param>
        /// <param name="type">Customer archetype (VIP tips more)</param>
        float ComputeTip(float basePrice, float patienceRemaining, CustomerType type);
    }
}
