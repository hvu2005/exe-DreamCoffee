using DreamCafe.Core.MVC;
using DreamCafe.Data;

namespace DreamCafe.Gameplay.Order
{
    public sealed class OrderTicketModel : IModel
    {
        public string OrderId;
        public string CustomerId;
        public string ItemId;
        public string ItemName;
        public OrderStatus Status;

        public void Reset()
        {
            OrderId = CustomerId = ItemId = ItemName = null;
            Status = OrderStatus.Pending;
        }
    }
}
