using DreamCafe.Core.MVC;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>Dữ liệu hiển thị cho bong bóng order phía trên đầu khách hàng.</summary>
    public readonly struct OrderTicketViewModel : IModel
    {
        public readonly string DisplayName;
        public readonly bool IsReady;

        public OrderTicketViewModel(string displayName, bool isReady)
        {
            DisplayName = displayName;
            IsReady = isReady;
        }
    }
}
