using DreamCafe.Core.Services;
using DreamCafe.Data;

namespace DreamCafe.Services.Order
{
    /// <summary>
    /// Order lifecycle: creation, crafting acknowledgement, serving, payment.
    /// Raises OrderPlaced, OrderServed, PaymentReceived events.
    /// </summary>
    public interface IOrderService : IService
    {
        int OpenOrderCount { get; }
        string PlaceOrder(string customerId, string itemId, float basePrice, CustomerType customerType);
        void MarkCrafted(string orderId);
        void ServeOrder(string orderId);
        bool TryGetOrder(string orderId, out OrderData order);
        bool TryGetOldestPending(out OrderData order);
        bool TryGetReadyOrderForCustomer(string customerId, out OrderData order);
    }
}
