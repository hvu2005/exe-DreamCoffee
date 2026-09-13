using DreamCafe.Core.Services;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Giao diện kho dữ liệu định nghĩa tĩnh cho Khách Hàng (Customer Definitions).
    /// Kế thừa IService để tham gia vòng đời hệ thống ServiceManager.
    /// </summary>
    public interface ICustomerRepository : IService
    {
        /// <summary>Lấy thông tin định nghĩa khách theo ID.</summary>
        CustomerItem GetCustomer(string id);

        /// <summary>Lấy toàn bộ danh sách khách hàng có trong game.</summary>
        CustomerItem[] GetAllCustomers();

        /// <summary>Lấy danh sách khách hàng theo phân nhóm.</summary>
        CustomerItem[] GetCustomersByType(CustomerType type);
    }
}
