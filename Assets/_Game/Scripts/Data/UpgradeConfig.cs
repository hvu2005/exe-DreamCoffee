using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Lists all available upgrades. Place one at Resources/UpgradeConfig.asset.
    /// Loaded by UpgradeService on Init; drives the UpgradeShop UI.
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeConfig", menuName = "DreamCafé/Upgrade Config")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        public UpgradeData[] upgrades = System.Array.Empty<UpgradeData>();
    }
}
