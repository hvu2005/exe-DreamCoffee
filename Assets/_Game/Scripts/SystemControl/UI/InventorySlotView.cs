using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Một ô trong lưới kho. Prefab dùng chung cho mọi nguyên liệu — nội dung gán runtime qua Bind().
    /// Chữ để tiếng Anh vì font mặc định (LiberationSans SDF) không có glyph tiếng Việt.
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image categoryTag;
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private TMP_Text quantityLabel;

        /// <summary>Hiển thị theo số lượng ghi trong chính item (bản runtime của kho, hoặc asset định nghĩa).</summary>
        public void Bind(InventoryItem item) => Bind(item, item != null ? item.Quantity : 0);

        /// <summary>
        /// Hiển thị với số lượng truyền vào — dùng khi số thật nằm ở InventoryController chứ không
        /// phải trên asset định nghĩa (vd: sau khi pha chế đã trừ kho).
        /// </summary>
        public void Bind(InventoryItem item, int quantity)
        {
            nameLabel.text = item.DisplayName;

            icon.sprite = item.Icon;
            icon.color = item.Icon != null ? item.Tint : Color.clear;

            categoryLabel.text = Label(item.Category);
            categoryTag.color = Tint(item.Category);

            infoLabel.text = item.IsPerishable
                ? $"Expires in {item.ShelfLifeDays} days"
                : "Never expires";

            quantityLabel.text = $"{quantity} {item.UnitLabel}";
        }

        private static string Label(ItemCategory category) => category switch
        {
            ItemCategory.Base => "BASE",
            ItemCategory.Flavor => "FLAVOR",
            ItemCategory.Topping => "TOPPING",
            ItemCategory.Fresh => "FRESH",
            _ => category.ToString().ToUpperInvariant()
        };

        private static Color Tint(ItemCategory category) => category switch
        {
            ItemCategory.Base => new Color(0.42f, 0.55f, 0.25f),
            ItemCategory.Flavor => new Color(0.60f, 0.42f, 0.75f),
            ItemCategory.Topping => new Color(0.93f, 0.62f, 0.28f),
            ItemCategory.Fresh => new Color(0.36f, 0.70f, 0.45f),
            _ => Color.gray
        };
    }
}
