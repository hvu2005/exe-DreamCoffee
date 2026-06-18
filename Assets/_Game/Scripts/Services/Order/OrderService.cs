using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Services.Order
{
    /// <summary>
    /// Creates, tracks, and serves orders. On PlaceOrder → OrderPlaced; on ServeOrder → OrderServed → PaymentReceived.
    /// Phase 0: functional but tip is always 0; Phase 3 integrates ITipStrategy and reputation.
    /// TODO Phase 2: hook ItemCrafted to auto-match open orders.
    /// </summary>
    public sealed class OrderService : IOrderService
    {
        private IEventBus _events;
        private readonly Dictionary<string, OrderData> _orders = new();
        private int _orderCounter;

        public int OpenOrderCount { get; private set; }

        public void Init(ServiceContext ctx)
        {
            _events = ctx.Events;
            Debug.Log("[OrderService] Initialized.");
        }

        public void Shutdown()
        {
            _orders.Clear();
            OpenOrderCount = 0;
            Debug.Log("[OrderService] Shutdown.");
        }

        public string PlaceOrder(string customerId, string itemId)
        {
            var orderId = $"order_{++_orderCounter}";
            var order = new OrderData(orderId, customerId, itemId, OrderStatus.Pending);
            _orders[orderId] = order;
            OpenOrderCount++;
            _events.Publish(new OrderPlaced(orderId, customerId, itemId));
            Debug.Log($"[OrderService] Order placed: {orderId} ({itemId})");
            return orderId;
        }

        public void ServeOrder(string orderId)
        {
            if (!_orders.TryGetValue(orderId, out var order)) return;
            _orders[orderId] = order.WithStatus(OrderStatus.Served);
            OpenOrderCount = Mathf.Max(0, OpenOrderCount - 1);

            _events.Publish(new OrderServed(orderId, order.CustomerId));
            _events.Publish(new PaymentReceived(order.CustomerId, order.BasePrice, 0f));
            Debug.Log($"[OrderService] Order served: {orderId}");
        }

        public bool TryGetOrder(string orderId, out OrderData order) =>
            _orders.TryGetValue(orderId, out order);
    }
}
