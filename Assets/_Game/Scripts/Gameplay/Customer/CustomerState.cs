namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Các trạng thái trong vòng đời AI của một khách hàng.
    /// </summary>
    public enum CustomerState
    {
        Spawning,
        MovingToCounter,
        Ordering,
        MovingToSeat,
        Dining,
        Leaving
    }
}
