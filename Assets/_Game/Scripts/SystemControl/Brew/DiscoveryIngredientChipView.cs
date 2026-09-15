using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Một ô nguyên liệu trong dải "BLUEPRINT" của <see cref="RecipeDiscoveryPopupView"/>:
    /// ảnh nguyên liệu + tên + số lượng cần. Dấu "+" nằm sẵn trong prefab, chỉ bật cho các ô từ
    /// thứ hai trở đi để thành chuỗi "A + B + C".
    /// </summary>
    public sealed class DiscoveryIngredientChipView : MonoBehaviour
    {
        [SerializeField, Tooltip("Dấu + bên trái ô — tắt ở ô đầu tiên.")]
        private GameObject plusSign;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text countLabel;

        /// <summary>
        /// Hiển thị một nguyên liệu của công thức.
        /// </summary>
        /// <param name="item">Định nghĩa nguyên liệu — null thì rơi về <paramref name="fallbackName"/>.</param>
        /// <param name="fallbackName">Tên thay thế khi không tra được item (thường là chính ID).</param>
        /// <param name="count">Số lượng cần của nguyên liệu này.</param>
        /// <param name="showPlus">Có hiện dấu + phía trước hay không.</param>
        public void Bind(InventoryItem item, string fallbackName, int count, bool showPlus)
        {
            if (plusSign != null) plusSign.SetActive(showPlus);

            if (nameLabel != null) nameLabel.text = item != null ? item.DisplayName : fallbackName;
            if (countLabel != null) countLabel.text = $"x{count}";

            if (icon == null) return;
            icon.sprite = item != null ? item.Icon : null;
            // Không có ảnh thì ẩn hẳn, tránh hiện ô vuông trắng mặc định của Image.
            icon.enabled = icon.sprite != null;
            if (icon.sprite != null) icon.color = item.Tint;
        }
    }
}
