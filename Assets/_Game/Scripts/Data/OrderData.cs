namespace DreamCafe.Data
{
    /// <summary>
    /// Immutable runtime snapshot of a single order's state.
    /// Passed through events and stored in OrderService dictionary.
    /// CustomerType is stored at placement so ServeOrder can compute tip without querying the live customer.
    /// </summary>
    public readonly struct OrderData
    {
        public readonly string OrderId;
        public readonly string CustomerId;
        public readonly string ItemId;
        public readonly OrderStatus Status;
        public readonly float BasePrice;
        public readonly CustomerType CustomerType;

        public OrderData(string orderId, string customerId, string itemId,
                         OrderStatus status, float basePrice = 0f,
                         CustomerType customerType = CustomerType.Worker)
        {
            OrderId      = orderId;
            CustomerId   = customerId;
            ItemId       = itemId;
            Status       = status;
            BasePrice    = basePrice;
            CustomerType = customerType;
        }

        public OrderData WithStatus(OrderStatus newStatus) =>
            new(OrderId, CustomerId, ItemId, newStatus, BasePrice, CustomerType);
    }
}
