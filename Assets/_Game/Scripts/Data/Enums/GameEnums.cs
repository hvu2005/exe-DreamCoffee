namespace DreamCafe.Data
{
    /// <summary>
    /// All game-wide enumerations. Declared fully even if not all values are used in prototype.
    /// TODO: Extend CustomerType, CafeStyle, StaffRole as new content is added.
    /// </summary>

    public enum CustomerType    { Student, Worker, Tourist, VIP, Influencer }
    public enum CustomerEmotion { Neutral, Happy, Disappointed, Angry }
    public enum CustomerState   { Idle, WaitingForSeat, Seated, WaitingForOrder, Ordering, Eating, Leaving }
    public enum QueueType       { DineIn, Takeaway }
    public enum OrderStatus     { Pending, Crafting, Ready, Served, Cancelled }
    public enum ItemType        { Drink, Food }
    public enum ItemCategory    { Coffee, Tea, Juice, Pastry, Bread, Snack }
    public enum CraftingStationStatus { Idle, Ready }
    public enum StaffRole       { Cashier, Barista, Baker, Waiter, Cleaner }
    public enum CafeStyle       { Vintage, Modern, Study, Instagram, Movie, CatCafe, Cyberpunk, Acoustic }
    public enum EventType
    {
        CustomerSpawned, OrderPlaced, ItemCrafted, OrderServed,
        PaymentReceived, CustomerLeft, DayStarted, DayEnded
    }
}
