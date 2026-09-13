using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject chứa danh mục toàn bộ CustomerItem definition — cấu hình trên Inspector.
    /// Lưu trữ trong DatabaseManager để các hệ thống khác truy xuất.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerRepository", menuName = "DreamCafe/Data/CustomerRepository")]
    public sealed class ScriptableCustomerRepository : ScriptableObject, ICustomerRepository
    {
        [SerializeField]
        private CustomerItem[] _customers = Array.Empty<CustomerItem>();

        /// <summary>Khởi tạo repository trong ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[CustomerRepository] Initialized — {_customers.Length} customers.");
        }

        /// <summary>Dọn dẹp tài nguyên.</summary>
        public void Shutdown()
        {
        }

        /// <summary>Tìm khách hàng theo ID.</summary>
        public CustomerItem GetCustomer(string id) =>
            _customers.FirstOrDefault(c => c != null && c.Id == id);

        /// <summary>Lấy toàn bộ danh sách khách hàng.</summary>
        public CustomerItem[] GetAllCustomers() => _customers;

        /// <summary>Lọc khách hàng theo phân nhóm.</summary>
        public CustomerItem[] GetCustomersByType(CustomerType type) =>
            _customers.Where(c => c != null && c.CustomerType == type).ToArray();
    }
}
