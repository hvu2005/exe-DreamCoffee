using DreamCafe.Core.Services;

namespace DreamCafe.Services.Economy
{
    /// <summary>
    /// Player wallet and daily revenue tracking. Subscribes to PaymentReceived to auto-add funds.
    /// DayRevenue resets each DayStarted so TimeService can populate DayEnded summary.
    /// </summary>
    public interface IEconomyService : IService
    {
        float Balance      { get; }
        float TotalRevenue { get; }
        float DayRevenue   { get; }
        void AddRevenue(float baseAmount, float tip = 0f);
        bool SpendMoney(float amount);
    }
}
