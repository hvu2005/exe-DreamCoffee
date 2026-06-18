namespace DreamCafe.Data
{
    /// <summary>
    /// Immutable runtime snapshot of a single order's state.
    /// Passed through events and stored in OrderService dictionary.
    /// </summary>
    public readonly struct OrderData
    {
        public readonly string OrderId;
        public readonly string CustomerId;
        public readonly string ItemId;
        public readonly OrderStatus Status;
        public readonly float BasePrice;

        public OrderData(string orderId, string customerId, string itemId, OrderStatus status, float basePrice = 0f)
        {
            OrderId = orderId; CustomerId = customerId; ItemId = itemId;
            Status = status; BasePrice = basePrice;
        }

        public OrderData WithStatus(OrderStatus newStatus) =>
            new(OrderId, CustomerId, ItemId, newStatus, BasePrice);
    }
}
