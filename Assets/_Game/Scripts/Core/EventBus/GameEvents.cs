using DreamCafe.Data;

namespace DreamCafe.Core.EventBus
{
    /// <summary>
    /// All game event payload structs. Readonly structs = GC-free dispatch on the hot loop.
    /// Each carries an EventType tag for analytics filtering.
    /// TODO: Add sequence number / timestamp for replay and debugging.
    /// </summary>

    public readonly struct CustomerSpawned : IEvent
    {
        public readonly string CustomerId;
        public readonly CustomerType Type;
        public readonly bool IsTakeaway;
        public EventType Kind => EventType.CustomerSpawned;
        public CustomerSpawned(string customerId, CustomerType type, bool isTakeaway)
        { CustomerId = customerId; Type = type; IsTakeaway = isTakeaway; }
    }

    public readonly struct OrderPlaced : IEvent
    {
        public readonly string OrderId;
        public readonly string CustomerId;
        public readonly string ItemId;
        public EventType Kind => EventType.OrderPlaced;
        public OrderPlaced(string orderId, string customerId, string itemId)
        { OrderId = orderId; CustomerId = customerId; ItemId = itemId; }
    }

    public readonly struct ItemCrafted : IEvent
    {
        public readonly string ItemId;
        public readonly string CraftingStationId;
        public readonly string OrderId;
        public EventType Kind => EventType.ItemCrafted;
        public ItemCrafted(string itemId, string craftingStationId, string orderId)
        { ItemId = itemId; CraftingStationId = craftingStationId; OrderId = orderId; }
    }

    public readonly struct OrderServed : IEvent
    {
        public readonly string OrderId;
        public readonly string CustomerId;
        public EventType Kind => EventType.OrderServed;
        public OrderServed(string orderId, string customerId)
        { OrderId = orderId; CustomerId = customerId; }
    }

    public readonly struct PaymentReceived : IEvent
    {
        public readonly string CustomerId;
        public readonly float BaseAmount;
        public readonly float Tip;
        public EventType Kind => EventType.PaymentReceived;
        public PaymentReceived(string customerId, float baseAmount, float tip)
        { CustomerId = customerId; BaseAmount = baseAmount; Tip = tip; }
    }

    public readonly struct CustomerLeft : IEvent
    {
        public readonly string CustomerId;
        public readonly bool WasSatisfied;
        public readonly CustomerEmotion FinalEmotion;
        public EventType Kind => EventType.CustomerLeft;
        public CustomerLeft(string customerId, bool wasSatisfied, CustomerEmotion emotion)
        { CustomerId = customerId; WasSatisfied = wasSatisfied; FinalEmotion = emotion; }
    }

    public readonly struct DayStarted : IEvent
    {
        public readonly int DayNumber;
        public EventType Kind => EventType.DayStarted;
        public DayStarted(int dayNumber) { DayNumber = dayNumber; }
    }

    public readonly struct DayEnded : IEvent
    {
        public readonly int DayNumber;
        public readonly float TotalRevenue;
        public readonly int CustomersServed;
        public readonly int CustomersLost;
        public EventType Kind => EventType.DayEnded;
        public DayEnded(int dayNumber, float totalRevenue, int customersServed, int customersLost)
        { DayNumber = dayNumber; TotalRevenue = totalRevenue; CustomersServed = customersServed; CustomersLost = customersLost; }
    }

    public readonly struct UpgradePurchased : IEvent
    {
        public readonly string UpgradeId;
        public readonly int    NewLevel;
        public EventType Kind => EventType.UpgradePurchased;
        public UpgradePurchased(string upgradeId, int newLevel)
        { UpgradeId = upgradeId; NewLevel = newLevel; }
    }
}
