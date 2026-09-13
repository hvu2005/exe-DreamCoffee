using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Định nghĩa cấu hình cho một loại Tiền tệ (Currency Definition) dạng ScriptableObject.
    /// Hoạt động như một Data Model đại diện cho 1 loại tiền tệ trong game (Tiền mặt, Tiền/giây, Danh tiếng).
    /// </summary>
    [CreateAssetMenu(fileName = "currency_new", menuName = "DreamCafe/Currency Item", order = 4)]
    public sealed class CurrencyItem : ScriptableObject
    {
        [Header("Định danh")]
        [SerializeField, Tooltip("Loại tiền tệ enum.")]
        private CurrencyType _type = CurrencyType.Money;

        [SerializeField, Tooltip("ID duy nhất của loại tiền tệ.")]
        private string _id;

        [SerializeField, Tooltip("Tên hiển thị của loại tiền tệ (vd: Tiền Mặt, Tiền/giây, Danh Tiếng).")]
        private string _displayName = "Tiền Mới";

        [SerializeField, Tooltip("Ký hiệu đơn vị (vd: đ, đ/s, Rep).")]
        private string _unitSymbol = "đ";

        [SerializeField, TextArea(2, 4), Tooltip("Mô tả chi tiết về cách dùng của loại tiền tệ.")]
        private string _description;

        [Header("Hình ảnh")]
        [SerializeField, Tooltip("Icon đại diện cho loại tiền tệ này trên UI.")]
        private Sprite _icon;

        [Header("Giá trị mặc định")]
        [SerializeField, Min(0f), Tooltip("Số lượng khởi đầu khi người chơi mới mở quán.")]
        private float _startingValue = 0f;

        [SerializeField, Min(0f), Tooltip("Giới hạn giá trị tối thiểu.")]
        private float _minValue = 0f;

        [SerializeField, Tooltip("Giới hạn giá trị tối đa (mặc định không giới hạn).")]
        private float _maxValue = 999_999_999f;

        // =====================================================================
        // Properties (Read-Only)
        // =====================================================================

        /// <summary>Loại tiền tệ theo Enum.</summary>
        public CurrencyType Type => _type;

        /// <summary>ID duy nhất (mặc định lấy theo tên asset nếu trống).</summary>
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        /// <summary>Tên hiển thị.</summary>
        public string DisplayName => _displayName;

        /// <summary>Ký hiệu đơn vị.</summary>
        public string UnitSymbol => _unitSymbol;

        /// <summary>Mô tả chi tiết.</summary>
        public string Description => _description;

        /// <summary>Icon đại diện.</summary>
        public Sprite Icon => _icon;

        /// <summary>Giá trị khởi đầu.</summary>
        public float StartingValue => _startingValue;

        /// <summary>Giá trị tối thiểu.</summary>
        public float MinValue => _minValue;

        /// <summary>Giá trị tối đa.</summary>
        public float MaxValue => _maxValue;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name;
            }

            if (_maxValue < _minValue)
            {
                _maxValue = _minValue;
            }
        }
    }
}
