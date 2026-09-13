using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Định nghĩa thông tin cấu hình cho một đối tượng Khách hàng (Customer Definition).
    /// Hoạt động như một Data Model / ScriptableObject template.
    /// </summary>
    [CreateAssetMenu(fileName = "customer_new", menuName = "DreamCafe/Customer Item", order = 1)]
    public sealed class CustomerItem : ScriptableObject
    {
        [Header("Định danh")]
        [SerializeField, Tooltip("ID duy nhất của khách hàng.")]
        private string _id;

        [SerializeField, Tooltip("Tên hiển thị của khách hàng.")]
        private string _displayName = "Khách Hàng Mới";

        [SerializeField, TextArea(2, 4), Tooltip("Mô tả ngắn hoặc tiểu sử của khách hàng.")]
        private string _description;

        [Header("Hình ảnh & Phân loại")]
        [SerializeField, Tooltip("Ảnh đại diện hoặc sprite của khách hàng.")]
        private Sprite _avatar;

        [SerializeField, Tooltip("Nhóm đối tượng khách hàng.")]
        private CustomerType _customerType = CustomerType.Student;

        [Header("Chỉ số hành vi")]
        [SerializeField, Min(5f), Tooltip("Độ kiên nhẫn cơ bản (tính bằng giây).")]
        private float _basePatienceSeconds = 30f;

        [SerializeField, Min(0), Tooltip("Ngân sách chi tiêu tối thiểu (VNĐ).")]
        private int _budgetMin = 20000;

        [SerializeField, Min(0), Tooltip("Ngân sách chi tiêu tối đa (VNĐ).")]
        private int _budgetMax = 50000;

        [SerializeField, Range(0f, 1f), Tooltip("Tỉ lệ đưa tiền boa (0 - 1).")]
        private float _tipChance = 0.5f;

        [SerializeField, Min(1f), Tooltip("Thời gian ngồi thưởng thức món tại bàn (giây).")]
        private float _diningTimeSeconds = 10f;

        [Header("Mở khóa & Món yêu thích")]
        [SerializeField, Tooltip("Danh sách ID các công thức món ăn/thức uống yêu thích (tối đa 6 món).")]
        private string[] _favoriteRecipeIds = Array.Empty<string>();

        [SerializeField, Min(1), Tooltip("Số lượng món yêu thích cần mở khóa để mở khóa khách này.")]
        private int _unlockThreshold = 3;

        [SerializeField, Tooltip("Khách hàng này có được mở khóa mặc định ngay từ đầu game không?")]
        private bool _isDefaultUnlocked = false;

        // =====================================================================
        // Properties (Read-Only)
        // =====================================================================

        /// <summary>ID duy nhất của khách hàng (mặc định lấy theo tên asset nếu trống).</summary>
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        /// <summary>Tên hiển thị.</summary>
        public string DisplayName => _displayName;

        /// <summary>Mô tả chi tiết.</summary>
        public string Description => _description;

        /// <summary>Ảnh đại diện.</summary>
        public Sprite Avatar => _avatar;

        /// <summary>Phân loại khách.</summary>
        public CustomerType CustomerType => _customerType;

        /// <summary>Độ kiên nhẫn cơ bản (giây).</summary>
        public float BasePatienceSeconds => _basePatienceSeconds;

        /// <summary>Ngân sách tối thiểu.</summary>
        public int BudgetMin => _budgetMin;

        /// <summary>Ngân sách tối đa.</summary>
        public int BudgetMax => _budgetMax;

        /// <summary>Tỉ lệ tiền tip.</summary>
        public float TipChance => _tipChance;

        /// <summary>Thời gian ngồi ăn uống (giây).</summary>
        public float DiningTimeSeconds => _diningTimeSeconds;

        /// <summary>Danh sách ID các món yêu thích.</summary>
        public IReadOnlyList<string> FavoriteRecipeIds => _favoriteRecipeIds;

        /// <summary>Ngưỡng món mở khóa yêu cầu.</summary>
        public int UnlockThreshold => _unlockThreshold;

        /// <summary>Có được mở khóa sẵn từ đầu hay không.</summary>
        public bool IsDefaultUnlocked => _isDefaultUnlocked;

        /// <summary>
        /// Tạo bản sao runtime để tránh can thiệp vào asset gốc lúc chạy game.
        /// </summary>
        public CustomerItem CreateRuntimeInstance()
        {
            var clone = Instantiate(this);
            clone.name = name;
            return clone;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name;
            }

            if (_budgetMax < _budgetMin)
            {
                _budgetMax = _budgetMin;
            }
        }
    }
}
