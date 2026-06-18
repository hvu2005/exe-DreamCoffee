using DreamCafe.Core.MVC;
using DreamCafe.Core.Services;
using DreamCafe.Services.Customer;
using UnityEngine;

namespace DreamCafe.Gameplay.Table
{
    /// <summary>
    /// Scene object representing a single cafe table seat.
    /// Implements ITable so CustomerService can query seat availability and assign customers
    /// without a Gameplay→Services dependency inversion.
    /// Place one per chair in the scene; set TableIndex to a unique int per table.
    /// </summary>
    public sealed class TableController : ControllerBase, ITable
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

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(SeatTransform.position, Vector3.one * 0.4f);
        }
    }
}
