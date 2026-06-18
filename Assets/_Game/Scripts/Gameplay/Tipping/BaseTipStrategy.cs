using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.Tipping
{
    /// <summary>
    /// Baseline tip: 20% of base price × patience remaining, doubled for VIP customers.
    /// No tip if patience is below 0.3 (customer left annoyed).
    /// TODO: Factor in reputation level, combo streak, and cafe style bonuses.
    /// </summary>
    public sealed class BaseTipStrategy : ITipStrategy
    {
        public float ComputeTip(float basePrice, float patienceRemaining, CustomerType type)
        {
            if (patienceRemaining < 0.3f) return 0f;
            var tip = basePrice * patienceRemaining * 0.2f;
            if (type == CustomerType.VIP) tip *= 2f;
            return Mathf.Max(0f, tip);
        }
    }
}
