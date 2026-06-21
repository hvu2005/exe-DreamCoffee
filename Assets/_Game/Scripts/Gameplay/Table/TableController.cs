using DreamCafe.Core.Interfaces;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Services;
using DreamCafe.Services.Customer;
using DreamCafe.Services.Order;
using UnityEngine;

namespace DreamCafe.Gameplay.Table
{
    /// <summary>
    /// Scene object representing a single cafe table seat.
    /// Implements ITable (seat state) and ITappable (serve interaction).
    /// OnTap: if occupied and a Ready order exists for the seated customer, calls ServeOrder.
    /// Requires a BoxCollider2D (on the Tappable layer) for Physics2D.OverlapPoint detection.
    /// Place one per chair in the scene; set TableIndex to a unique int per table.
    /// </summary>
    public sealed class TableController : ControllerBase, ITable, ITappable
    {
        [SerializeField] private int tableIndex;
        [SerializeField] private Transform seatTransform;

        public int TableIndex => tableIndex;
        public bool IsOccupied { get; private set; }
        public Transform SeatTransform => seatTransform != null ? seatTransform : transform;

        private string _occupyingCustomerId;

        private void Awake()
        {
            if (seatTransform == null)
                seatTransform = transform;
        }

        public override void Bind(ServiceContext ctx)
        {
            base.Bind(ctx);
            ctx.Services.Resolve<ICustomerService>().RegisterTable(this);
        }

        public override void Unbind()
        {
            Ctx?.Services.Resolve<ICustomerService>().UnregisterTable(this);
            base.Unbind();
        }

        public void Occupy(string customerId)
        {
            IsOccupied = true;
            _occupyingCustomerId = customerId;
        }

        public void Vacate()
        {
            IsOccupied = false;
            _occupyingCustomerId = null;
        }

        public void OnTap()
        {
            if (Ctx == null || !IsOccupied || string.IsNullOrEmpty(_occupyingCustomerId)) return;
            var orderService = Ctx.Services.Resolve<IOrderService>();
            if (orderService.TryGetReadyOrderForCustomer(_occupyingCustomerId, out var order))
                orderService.ServeOrder(order.OrderId);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(SeatTransform.position, Vector3.one * 0.4f);
        }
    }
}
