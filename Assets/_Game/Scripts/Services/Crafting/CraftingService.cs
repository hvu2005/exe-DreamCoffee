using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using DreamCafe.Services.Order;
using UnityEngine;

namespace DreamCafe.Services.Crafting
{
    /// <summary>
    /// Instant tap-to-craft. TryCraft pops the oldest Pending order, marks it Crafted,
    /// and publishes ItemCrafted. CraftingStationController calls TryCraft on player tap.
    /// </summary>
    public sealed class CraftingService : ICraftingService
    {
        private IEventBus _events;
        private ServiceContext _ctx;
        private readonly List<ICraftingStation> _stations = new();

        public void Init(ServiceContext ctx)
        {
            _ctx    = ctx;
            _events = ctx.Events;
            Debug.Log("[CraftingService] Initialized.");
        }

        public void Shutdown()
        {
            _stations.Clear();
            Debug.Log("[CraftingService] Shutdown.");
        }

        public void RegisterStation(ICraftingStation station)
        {
            if (!_stations.Contains(station)) _stations.Add(station);
        }

        public void UnregisterStation(ICraftingStation station) => _stations.Remove(station);

        public bool TryCraft(string stationId)
        {
            var orderService = _ctx.Services.Resolve<IOrderService>();
            if (!orderService.TryGetOldestPending(out var order))
            {
                Debug.Log($"[CraftingService] No pending orders for station '{stationId}'.");
                return false;
            }

            orderService.MarkCrafted(order.OrderId);
            _events.Publish(new ItemCrafted(order.ItemId, stationId, order.OrderId));
            Debug.Log($"[CraftingService] Crafted '{order.ItemId}' at '{stationId}' → order {order.OrderId}");
            return true;
        }
    }
}
