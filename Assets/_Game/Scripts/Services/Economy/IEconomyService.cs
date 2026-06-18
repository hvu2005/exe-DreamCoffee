using DreamCafe.Core.Services;

namespace DreamCafe.Services.Economy
{
    /// <summary>
    /// Player wallet and revenue tracking. Subscribes to PaymentReceived to auto-add funds.
    /// TODO: Add Opex/Capex categorisation, transaction history, inflation curve config.
    /// </summary>
    public interface IEconomyService : IService
    {
        float Balance { get; }
        float TotalRevenue { get; }
        void AddRevenue(float baseAmount, float tip = 0f);
        bool SpendMoney(float amount);
    }
}
