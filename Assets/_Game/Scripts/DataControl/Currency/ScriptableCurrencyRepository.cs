using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject chứa danh mục toàn bộ định nghĩa Tiền Tệ (Currency Definitions) — cấu hình trên Inspector.
    /// Lưu trữ trong DatabaseManager để các hệ thống khác truy xuất, tương tự CustomerRepository và RecipeRepository.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyRepository", menuName = "DreamCafe/Data/CurrencyRepository", order = 4)]
    public sealed class ScriptableCurrencyRepository : ScriptableObject, ICurrencyRepository
    {
        [SerializeField, Tooltip("Danh sách các định nghĩa tiền tệ.")]
        private CurrencyItem[] _currencies = Array.Empty<CurrencyItem>();

        [Header("Tùy chọn lưu trữ")]
        [SerializeField, Tooltip("Tự động lưu số dư vào PlayerPrefs khi có biến động.")]
        private bool _enableAutoSave = true;

        public bool EnableAutoSave => _enableAutoSave;

        /// <summary>Khởi tạo repository trong ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[CurrencyRepository] Initialized — {_currencies.Length} currency definitions.");
        }

        /// <summary>Dọn dẹp tài nguyên.</summary>
        public void Shutdown()
        {
        }

        /// <summary>Lấy định nghĩa tiền tệ theo loại enum.</summary>
        public CurrencyItem GetCurrency(CurrencyType type) =>
            _currencies.FirstOrDefault(c => c != null && c.Type == type);

        /// <summary>Lấy định nghĩa tiền tệ theo ID.</summary>
        public CurrencyItem GetCurrency(string id) =>
            _currencies.FirstOrDefault(c => c != null && c.Id == id);

        /// <summary>Lấy toàn bộ danh sách định nghĩa tiền tệ.</summary>
        public CurrencyItem[] GetAllCurrencies() => _currencies;

        /// <summary>Lấy giá trị khởi đầu mặc định.</summary>
        public float GetStartingValue(CurrencyType type)
        {
            var item = GetCurrency(type);
            return item != null ? item.StartingValue : 0f;
        }
    }
}
