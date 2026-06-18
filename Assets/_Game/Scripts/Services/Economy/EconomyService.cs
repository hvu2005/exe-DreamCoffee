using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Economy
{
    /// <summary>
    /// Wallet and revenue tracking. Subscribes to PaymentReceived to accumulate balance automatically.
    /// Starting balance 500 VND * 1000 = 500k for prototype tuning.
    /// TODO Phase 3: add Opex deductions (salary, restock), transaction log, soft-lock safety net.
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private IEventBus _events;

        public float Balance { get; private set; } = 500_000f;
        public float TotalRevenue { get; private set; }

        public void Init(ServiceContext ctx)
        {
            _events = ctx.Events;
            _events.Subscribe<PaymentReceived>(OnPaymentReceived);
            Debug.Log($"[EconomyService] Initialized. Starting balance: {Balance:N0}đ");
        }

        public void Shutdown()
        {
            _events?.Unsubscribe<PaymentReceived>(OnPaymentReceived);
            Debug.Log("[EconomyService] Shutdown.");
        }

        public void AddRevenue(float baseAmount, float tip = 0f)
        {
            Balance += baseAmount + tip;
            TotalRevenue += baseAmount + tip;
        }

        public bool SpendMoney(float amount)
        {
            if (Balance < amount) return false;
            Balance -= amount;
            return true;
        }

        private void OnPaymentReceived(PaymentReceived evt)
        {
            var total = evt.BaseAmount + evt.Tip;
            Balance += total;
            TotalRevenue += total;
            Debug.Log($"[EconomyService] +{total:N0}đ (tip:{evt.Tip:N0}). Balance: {Balance:N0}đ");
        }
    }
}
