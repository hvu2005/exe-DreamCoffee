using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Top-level balance-tuning ScriptableObject. Place one at Resources/CafeConfig.asset.
    /// GameBootstrap applies these values to services after Init so no recompile is needed.
    /// </summary>
    [CreateAssetMenu(fileName = "CafeConfig", menuName = "DreamCafé/Cafe Config")]
    public sealed class CafeConfig : ScriptableObject
    {
        [Header("Customer Spawning")]
        [Tooltip("Seconds between spawn attempts.")]
        public float spawnIntervalSeconds = 8f;

        [Header("Day Cycle")]
        [Tooltip("Real-time seconds per in-game day (180 = 3 minutes).")]
        public float dayLengthSeconds = 180f;

        [Header("Patience")]
        [Tooltip("Override global patience in seconds for all customers. 0 = use per-type defaults.")]
        public float defaultPatienceSeconds = 0f;

        [Header("Eating Duration")]
        [Tooltip("Seconds a customer spends eating after being served.")]
        public float eatDurationSeconds = 5f;
    }
}
