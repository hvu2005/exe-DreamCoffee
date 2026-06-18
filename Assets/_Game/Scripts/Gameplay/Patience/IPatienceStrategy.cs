using DreamCafe.Data;

namespace DreamCafe.Gameplay.Patience
{
    /// <summary>
    /// Strategy for draining customer patience (0=gone, 1=full) over time.
    /// Swap implementations per customer type or reputation level without touching callers.
    /// TODO: Add curve-based drain variant for non-linear difficulty scaling.
    /// </summary>
    public interface IPatienceStrategy
    {
        /// <param name="current">Current patience 0..1</param>
        /// <param name="dt">Delta time in seconds</param>
        /// <param name="type">Customer archetype affecting drain rate</param>
        /// <param name="reputationFactor">Multiplier from cafe reputation (higher = slower drain)</param>
        /// <returns>New patience value clamped 0..1</returns>
        float Drain(float current, float dt, CustomerType type, float reputationFactor);
    }
}
