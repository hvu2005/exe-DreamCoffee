using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Config for a single menu item (drink or food). All tuning lives here — no recompile needed.
    /// Create via Assets > Create > DreamCafe > Data > Item.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "DreamCafe/Data/Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public ItemType itemType;
        public ItemCategory category;
        [Min(0)] public float basePrice;
        [Min(0)] public float craftTimeSeconds;
        [TextArea] public string description;
        public Sprite icon;
    }
}
