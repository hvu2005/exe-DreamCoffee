using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Economy
{
    /// <summary>
    /// Wallet and revenue tracking. Auto-accumulates from PaymentReceived.
    /// DayRevenue resets on DayStarted so the daily summary in DayEnded is accurate.
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private IEventBus _events;

        public float Balance      { get; private set; } = 500_000f;
        public float TotalRevenue { get; private set; }
        public float DayRevenue   { get; private set; }

        public void Init(ServiceContext ctx)
        {
            _events = ctx.Events;
            _events.Subscribe<PaymentReceived>(OnPaymentReceived);
            _events.Subscribe<DayStarted>(OnDayStarted);
            Debug.Log($"[EconomyService] Initialized. Starting balance: {Balance:N0}đ");
        }

        public void Shutdown()
        {
            _events?.Unsubscribe<PaymentReceived>(OnPaymentReceived);
            _events?.Unsubscribe<DayStarted>(OnDayStarted);
            Debug.Log("[EconomyService] Shutdown.");
        }

        public void AddRevenue(float baseAmount, float tip = 0f)
        {
            var total   = baseAmount + tip;
            Balance     += total;
            TotalRevenue += total;
            DayRevenue  += total;
        }

        public bool SpendMoney(float amount)
        {
            if (Balance < amount) return false;
            Balance -= amount;
            return true;
        }

        private void OnPaymentReceived(PaymentReceived evt)
        {
            var total    = evt.BaseAmount + evt.Tip;
            Balance      += total;
            TotalRevenue += total;
            DayRevenue   += total;
            Debug.Log($"[EconomyService] +{total:N0}đ (tip {evt.Tip:N0}đ). Balance: {Balance:N0}đ | Day: {DayRevenue:N0}đ");
        }

        private void OnDayStarted(DayStarted _) => DayRevenue = 0f;
    }
}
