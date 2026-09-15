using System;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Một ô nguyên liệu trong kệ bên trái panel pha chế: icon + badge "x{số lượng}".
    /// Bấm vào là bỏ nguyên liệu đó vào cốc. Hết hàng thì mờ đi và không bấm được.
    /// Prefab dùng chung cho mọi nguyên liệu — nội dung gán runtime qua <see cref="Bind"/>.
    /// </summary>
    public sealed class BrewIngredientSlotView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityLabel;
        [SerializeField] private Button button;

        private InventoryItem _item;
        private Action<InventoryItem> _onClick;
        private Color _baseIconColor = Color.white;

        /// <summary>Nguyên liệu mà ô này đang đại diện.</summary>
        public InventoryItem Item => _item;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>Gắn nguyên liệu vào ô và đăng ký callback khi người chơi bấm.</summary>
        public void Bind(InventoryItem item, Action<InventoryItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            // Chưa có sprite riêng thì để ô vuông màu theo nhóm làm placeholder tạm.
            _baseIconColor = item == null ? Color.clear
                : item.Icon != null ? item.Tint
                : CategoryColor(item.Category);

            if (icon != null)
            {
                icon.sprite = item != null ? item.Icon : null;
                icon.color = _baseIconColor;
            }

            SetAvailable(item != null ? item.Quantity : 0);
        }

        /// <summary>
        /// Cập nhật số lượng còn có thể bỏ vào cốc (tồn kho trừ phần đã nằm trong cốc).
        /// </summary>
        public void SetAvailable(int available)
        {
            if (quantityLabel != null) quantityLabel.text = $"x{available}";

            bool usable = available > 0;
            if (button != null) button.interactable = usable;
            if (frame != null) frame.color = usable ? ActiveFrame : DimmedFrame;
            if (icon != null)
            {
                var c = _baseIconColor;
                icon.color = usable ? c : new Color(c.r, c.g, c.b, c.a * 0.35f);
            }
        }

        private void HandleClick() => _onClick?.Invoke(_item);

        // Khay gỗ đã là sprite nên màu chỉ dùng để làm mờ khi hết hàng, không tô màu đè lên.
        private static readonly Color ActiveFrame = Color.white;
        private static readonly Color DimmedFrame = new(0.62f, 0.58f, 0.54f, 0.75f);

        /// <summary>Màu placeholder khi nguyên liệu chưa có icon riêng — phân biệt bằng nhóm.</summary>
        private static Color CategoryColor(ItemCategory category) => category switch
        {
            ItemCategory.Base => new Color(0.42f, 0.29f, 0.18f),
            ItemCategory.Flavor => new Color(0.85f, 0.66f, 0.30f),
            ItemCategory.Topping => new Color(0.93f, 0.62f, 0.28f),
            ItemCategory.Fresh => new Color(0.36f, 0.70f, 0.45f),
            _ => Color.gray
        };
    }
}
