using DreamCafe.Core.Services;
using DreamCafe.Data;

namespace DreamCafe.Services.Order
{
    /// <summary>
    /// Order lifecycle: creation, tracking, serving, tip calculation.
    /// Raises OrderPlaced, OrderServed, PaymentReceived events.
    /// TODO Phase 2: hook into CraftingService for ItemCrafted matching.
    /// </summary>
    public interface IOrderService : IService
    {
        string PlaceOrder(string customerId, string itemId);
        void ServeOrder(string orderId);
        bool TryGetOrder(string orderId, out OrderData order);
        int OpenOrderCount { get; }
    }
}
