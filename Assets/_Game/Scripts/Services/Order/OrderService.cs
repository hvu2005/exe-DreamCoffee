using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using DreamCafe.Gameplay.Tipping;
using UnityEngine;

namespace DreamCafe.Services.Order
{
    /// <summary>
    /// Creates, tracks, and closes orders. Raises OrderPlaced / OrderServed / PaymentReceived.
    /// Phase 3: ServeOrder computes tip via BaseTipStrategy (0.5f patience estimate — Phase 4 refines).
    /// </summary>
    public sealed class OrderService : IOrderService
    {
        private ServiceContext                         _ctx;
        private IEventBus                              _events;
        private readonly Dictionary<string, OrderData> _orders       = new();
        private readonly List<string>                  _pendingQueue = new();
        private int _orderCounter;

        public int OpenOrderCount { get; private set; }

        public void Init(ServiceContext ctx)
        {
            _ctx    = ctx;
            _events = ctx.Events;
            Debug.Log("[OrderService] Initialized.");
        }

        public void Shutdown()
        {
            _orders.Clear();
            _pendingQueue.Clear();
            OpenOrderCount = 0;
            Debug.Log("[OrderService] Shutdown.");
        }

        public string PlaceOrder(string customerId, string itemId, float basePrice, CustomerType customerType)
        {
            var orderId = $"order_{++_orderCounter}";
            var order   = new OrderData(orderId, customerId, itemId, OrderStatus.Pending, basePrice, customerType);
            _orders[orderId] = order;
            _pendingQueue.Add(orderId);
            OpenOrderCount++;
            _events.Publish(new OrderPlaced(orderId, customerId, itemId));
            Debug.Log($"[OrderService] Placed: {orderId} ({itemId}, {basePrice:N0}đ)");
            return orderId;
        }

        public void MarkCrafted(string orderId)
        {
            if (!_orders.TryGetValue(orderId, out var order)) return;
            _pendingQueue.Remove(orderId);
            _orders[orderId] = order.WithStatus(OrderStatus.Ready);
            Debug.Log($"[OrderService] Crafted: {orderId}");
        }

        public void ServeOrder(string orderId)
        {
            if (!_orders.TryGetValue(orderId, out var order)) return;
            if (order.Status != OrderStatus.Ready) return;

            _orders[orderId] = order.WithStatus(OrderStatus.Served);
            OpenOrderCount   = Mathf.Max(0, OpenOrderCount - 1);

            float tipMultiplier = 1f;
            if (_ctx.Services.TryResolve<DreamCafe.Services.Upgrade.IUpgradeService>(out var us))
                tipMultiplier = us.TipMultiplier;

            var tip = new BaseTipStrategy().ComputeTip(order.BasePrice, 0.5f, order.CustomerType) * tipMultiplier;
            _events.Publish(new OrderServed(orderId, order.CustomerId));
            _events.Publish(new PaymentReceived(order.CustomerId, order.BasePrice, tip));
            Debug.Log($"[OrderService] Served: {orderId} — base {order.BasePrice:N0}đ + tip {tip:N0}đ (x{tipMultiplier:F2} mult)");
        }

        public bool TryGetOrder(string orderId, out OrderData order) =>
            _orders.TryGetValue(orderId, out order);

        public bool TryGetOldestPending(out OrderData order)
        {
            while (_pendingQueue.Count > 0)
            {
                var id = _pendingQueue[0];
                if (_orders.TryGetValue(id, out order) && order.Status == OrderStatus.Pending)
                    return true;
                _pendingQueue.RemoveAt(0);
            }
            order = default;
            return false;
        }

        public bool TryGetReadyOrderForCustomer(string customerId, out OrderData order)
        {
            foreach (var kvp in _orders)
            {
                if (kvp.Value.CustomerId == customerId && kvp.Value.Status == OrderStatus.Ready)
                {
                    order = kvp.Value;
                    return true;
                }
            }
            order = default;
            return false;
        }
    }
}
