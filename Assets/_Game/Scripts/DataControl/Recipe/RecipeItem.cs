using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Định nghĩa thông tin một Công Thức Pha Chế (Recipe Definition) dạng ScriptableObject.
    /// Hỗ trợ thuộc tính Pair Value để tìm kiếm và so khớp tổ hợp nguyên liệu.
    /// </summary>
    [CreateAssetMenu(fileName = "recipe_new", menuName = "DreamCafe/Recipe Item", order = 2)]
    public sealed class RecipeItem : ScriptableObject
    {
        [Header("Định danh")]
        [SerializeField, Tooltip("ID công thức duy nhất.")]
        private string _id;

        [SerializeField, Tooltip("Tên hiển thị của món ăn/thức uống.")]
        private string _displayName = "Công thức mới";

        [SerializeField, TextArea(2, 4), Tooltip("Mô tả công thức.")]
        private string _description;

        [SerializeField, Tooltip("Icon của món thành phẩm.")]
        private Sprite _icon;

        [Header("Thành phẩm & Nguyên liệu")]
        [SerializeField, Tooltip("ID của món/nguyên liệu thành phẩm tạo ra trong kho.")]
        private string _outputItemId;

        [SerializeField, Tooltip("Danh sách ID các nguyên liệu bắt buộc phải có.")]
        private string[] _requiredIngredientIds = Array.Empty<string>();

        [Header("Thuộc tính pha chế")]
        [SerializeField, Min(0.5f), Tooltip("Thời gian nhân viên cần để pha chế/làm xong món (giây).")]
        private float _craftTimeSeconds = 3f;

        [SerializeField, Min(0), Tooltip("Giá bán gốc thu về khi phục vụ món này (VNĐ).")]
        private int _basePrice = 25000;

        [SerializeField, Min(0f), Tooltip("Tiền/giây cộng thêm vĩnh viễn vào MPS của quán khi công thức này được mở khóa (VNĐ/s).")]
        private float _moneyPerSecondBonus = 0f;

        [SerializeField, Tooltip("Công thức này có được mở khóa mặc định từ đầu game không (hay phải mò)?")]
        private bool _isDefaultUnlocked = true;

        // Cache cho Pair Value
        private string _cachedPairKey;

        // =====================================================================
        // Properties (Read-Only)
        // =====================================================================

        /// <summary>ID của công thức (lấy tên asset nếu trống).</summary>
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        /// <summary>Tên hiển thị món.</summary>
        public string DisplayName => _displayName;

        /// <summary>Mô tả món.</summary>
        public string Description => _description;

        /// <summary>Icon thành phẩm.</summary>
        public Sprite Icon => _icon;

        /// <summary>ID của món thành phẩm đầu ra.</summary>
        public string OutputItemId => _outputItemId;

        /// <summary>Danh sách ID các nguyên liệu cần thiết.</summary>
        public IReadOnlyList<string> RequiredIngredientIds => _requiredIngredientIds;

        /// <summary>Thời gian pha chế (giây).</summary>
        public float CraftTimeSeconds => _craftTimeSeconds;

        /// <summary>Giá bán cơ bản.</summary>
        public int BasePrice => _basePrice;

        /// <summary>Tiền/giây cộng thêm vào MPS của quán khi công thức này đã mở khóa (VNĐ/s).</summary>
        public float MoneyPerSecondBonus => _moneyPerSecondBonus;

        /// <summary>Có được mở khóa sẵn từ đầu game hay không.</summary>
        public bool IsDefaultUnlocked => _isDefaultUnlocked;

        /// <summary>
        /// Pair Value (Composite Key) chuẩn hóa từ các nguyên liệu yêu cầu.
        /// Ví dụ: "coffee_bean+milk" cho phép tra cứu công thức nhanh chóng.
        /// </summary>
        public string PairValue
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedPairKey))
                {
                    _cachedPairKey = RecipeKeyUtility.ComputePairKey(_requiredIngredientIds);
                }
                return _cachedPairKey;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name;
            }
            _cachedPairKey = RecipeKeyUtility.ComputePairKey(_requiredIngredientIds);
        }
    }
}
