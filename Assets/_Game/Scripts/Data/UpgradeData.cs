using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Config for one purchasable upgrade. Create via Assets > Create > DreamCafé > Upgrade.
    /// effectValues[level-1] holds the numeric effect at each level (length must equal maxLevel).
    /// Cost at level L = baseCost * costMultiplier^L  (exponential scaling).
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "DreamCafé/Upgrade")]
    public sealed class UpgradeData : ScriptableObject
    {
        public string        upgradeId;
        public string        displayName;
        [TextArea]
        public string        description;
        public UpgradeEffect effectType;
        [Min(1)]
        public float         baseCost      = 500f;
        [Min(1f)]
        public float         costMultiplier = 1.5f;
        [Min(1)]
        public int           maxLevel      = 2;
        [Tooltip("One value per level. Array length must equal maxLevel.")]
        public float[]       effectValues;

        public float CostAtLevel(int currentLevel) =>
            baseCost * Mathf.Pow(costMultiplier, currentLevel);
    }
}
