using DreamCafe.Core.Services;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Giao diện kho dữ liệu định nghĩa tĩnh cho Tiền Tệ (Currency Definitions & Starting Config).
    /// Kế thừa IService để tham gia vòng đời hệ thống ServiceManager.
    /// </summary>
    public interface ICurrencyRepository : IService
    {
        /// <summary>Lấy định nghĩa tiền tệ theo loại Enum.</summary>
        CurrencyItem GetCurrency(CurrencyType type);

        /// <summary>Lấy định nghĩa tiền tệ theo ID.</summary>
        CurrencyItem GetCurrency(string id);

        /// <summary>Lấy toàn bộ danh sách định nghĩa tiền tệ có trong game.</summary>
        CurrencyItem[] GetAllCurrencies();

        /// <summary>Lấy giá trị khởi đầu mặc định của loại tiền tệ.</summary>
        float GetStartingValue(CurrencyType type);

        /// <summary>Cờ cho phép tự động lưu trữ dữ liệu (Auto Save) hay không.</summary>
        bool EnableAutoSave { get; }
    }
}
