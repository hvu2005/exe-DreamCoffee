using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Scene MonoBehaviour that listens for OrderPlaced and spawns OrderTicketControllers from pool.
    /// Tickets are laid out horizontally from the anchor Transform.
    /// Bind this in GameBootstrap after services are initialized.
    /// </summary>
    public sealed class OrderTicketSpawner : ControllerBase
    {
        [SerializeField] private Transform ticketAnchor;
        [SerializeField] private float ticketSpacing = 0.7f;

        private int _activeCount;

        public override void Bind(ServiceContext ctx)
        {
            base.Bind(ctx);
            Subscribe<OrderPlaced>(OnOrderPlaced);
        }

        private void OnOrderPlaced(OrderPlaced evt)
        {
            var anchor = ticketAnchor != null ? ticketAnchor.position : Vector3.zero;
            var pos    = anchor + Vector3.right * (_activeCount * ticketSpacing);

            var ctrl = Ctx.Pool.Spawn<OrderTicketController>(PoolKey.OrderTicket, pos, Quaternion.identity);
            if (ctrl == null) return;

            ctrl.Initialize(Ctx, evt.OrderId, evt.CustomerId, evt.ItemId);
            _activeCount++;
        }

        // OrderTicketController handles its own despawn on OrderServed.
        // Spawner doesn't track individual tickets — count resets on new day.
        public override void OnSpawned() { }
        public override void OnDespawned() { Unbind(); }
    }
}
