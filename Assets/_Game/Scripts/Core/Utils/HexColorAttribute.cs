using UnityEngine;

namespace DreamCafe.Core.Utils
{
    /// <summary>
    /// Đánh dấu một field <c>string</c> đang chứa mã màu hex (vd "#6F4E37") để Inspector vẽ nó thành
    /// ô chọn màu y hệt một field <see cref="Color"/>, thay vì bắt gõ tay từng ký tự.
    ///
    /// Dữ liệu vẫn lưu dưới dạng chuỗi hex chứ không phải Color: designer copy/dán mã màu giữa các
    /// asset được, và diff trong git đọc ra là "#6F4E37" chứ không phải bốn số thực.
    ///
    /// Phần vẽ nằm ở HexColorDrawer bên Editor.
    /// </summary>
    public sealed class HexColorAttribute : PropertyAttribute
    {
        /// <summary>Cho chỉnh cả độ trong suốt — bật thì mã lưu thành 8 ký tự (#RRGGBBAA).</summary>
        public bool ShowAlpha { get; }

        public HexColorAttribute(bool showAlpha = false)
        {
            ShowAlpha = showAlpha;
        }
    }
}
