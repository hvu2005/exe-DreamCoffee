using System;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.DataControl;
using DreamCafe.Gameplay.Customer.States;
using DreamCafe.Gameplay.Order;
using DreamCafe.SystemControl.Customer;
using DreamCafe.SystemControl.Decor;
using DreamCafe.SystemControl.Navigation;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Điều khiển vòng đời AI của một khách hàng: đi tới quầy order, gọi món, đi tới bàn, ngồi
    /// đếm ngược rồi rời quán. Bản thân controller chỉ giữ tham chiếu và chuyển tiếp Tick tới
    /// <see cref="ICustomerState"/> đang active — toàn bộ hành vi nằm trong các state.
    /// </summary>
    [RequireComponent(typeof(GridPathFollower))]
    public sealed class CustomerController : ControllerBase
    {
        [Header("Tham chiếu")]
        [SerializeField] private GridPathFollower _mover;
        [SerializeField] private CustomerView _view;
        [SerializeField, Tooltip("Điểm neo để bong bóng order xuất hiện phía trên đầu")]
        private Transform _ticketAnchor;

        private ICustomerState _state;
        private OrderTicketController _ticket;

        public GridPathFollower Mover => _mover;
        public CustomerView View => _view;

        public CustomerItem Definition { get; private set; }
        public RecipeItem Order { get; private set; }

        public OrderCounterPoint AssignedCounter { get; private set; }
        public int CounterStandIndex { get; private set; }
        public Vector3 CounterStandPosition { get; private set; }

        public GridOccupant AssignedSeat { get; private set; }
        public int SeatIndex { get; private set; }
        public Vector3 SeatPosition { get; private set; }

        /// <summary>
        /// Xin chỗ ngồi khác khi chỗ đang giữ hỏng (bàn bị dỡ, hoặc bít đường không tới nổi).
        /// CustomerSceneManager cắm hàm này vào lúc spawn; trả về false nghĩa là quán hết chỗ.
        /// </summary>
        public System.Func<CustomerController, bool> SeatReassignRequest { get; set; }

        public Vector3 ExitPosition { get; private set; }

        private Action<CustomerController> _onFinished;

        private void Reset()
        {
            _mover = GetComponent<GridPathFollower>();
        }

        /// <summary>
        /// Nhận ServiceContext và tiêm dịch vụ tìm đường xuống bộ phận di chuyển — khách không tự
        /// đi tìm singleton, mọi phụ thuộc đi qua context như các controller khác trong dự án.
        /// </summary>
        public override void Bind(Core.Services.ServiceContext ctx)
        {
            base.Bind(ctx);

            if (_mover == null) _mover = GetComponent<GridPathFollower>();
            if (ctx?.Services != null && ctx.Services.TryResolve<IPathfindingService>(out var pathfinding))
            {
                _mover.SetPathfinding(pathfinding);
            }
        }

        /// <summary>Nạp toàn bộ dữ liệu spawn và bắt đầu FSM. Gọi ngay sau <see cref="Bind"/>.</summary>
        public void Configure(CustomerSpawnContext spawnCtx)
        {
            if (_mover == null) _mover = GetComponent<GridPathFollower>();
            // Lấy từ pool ra là xoá sạch đường đi của lượt trước, nếu không khách mới sinh ra sẽ
            // tiếp tục lết theo lộ trình của khách cũ.
            _mover.Teleport(transform.position);

            Definition = spawnCtx.Definition;
            Order = spawnCtx.Order;

            AssignedCounter = spawnCtx.Counter;
            CounterStandIndex = spawnCtx.StandIndex;
            CounterStandPosition = spawnCtx.StandPoint.position;

            AssignedSeat = spawnCtx.Seat;
            SeatIndex = spawnCtx.SeatIndex;
            SeatPosition = spawnCtx.SeatPoint;

            ExitPosition = spawnCtx.ExitPoint.position;
            _onFinished = spawnCtx.OnFinished;
            SeatReassignRequest = spawnCtx.SeatReassignRequest;

            ChangeState(new MovingToCounterState());
        }

        private void Update()
        {
            _state?.Tick(this, Time.deltaTime);
        }

        // =====================================================================
        // DI CHUYỂN (các state gọi qua đây, không đụng thẳng vào bộ tìm đường)
        // =====================================================================

        /// <summary>Đi tới một điểm. Trả về false nếu không có đường — nơi gọi tự xử.</summary>
        public bool MoveTo(Vector3 worldTarget) => _mover.SetDestination(worldTarget);

        /// <summary>Đứng lại tại chỗ.</summary>
        public void StopMoving() => _mover.Stop();

        /// <summary>Đã tới đích của lần <see cref="MoveTo"/> gần nhất chưa.</summary>
        public bool HasArrived => _mover.HasArrived;

        /// <summary>
        /// Ép lớp vẽ của khách theo chỗ ngồi: trên cái ghế, dưới mặt bàn. Gọi lúc ngồi xuống.
        /// </summary>
        public void ApplySeatedSorting()
        {
            if (AssignedSeat == null || SeatIndex < 0) return;

            var sorter = GetComponent<SystemControl.Rendering.IsoDepthSorter>();
            if (sorter != null) sorter.SetOrderOverride(AssignedSeat.SeatedSortingOrder(SeatIndex));
        }

        /// <summary>Bỏ ép lớp vẽ, quay lại xếp theo độ sâu (lúc đứng dậy đi).</summary>
        public void ClearSeatedSorting()
        {
            var sorter = GetComponent<SystemControl.Rendering.IsoDepthSorter>();
            if (sorter != null) sorter.ClearOrderOverride();
        }

        /// <summary>
        /// Nhận chỗ ngồi mới do CustomerSceneManager giao — dùng khi phải đổi bàn giữa chừng.
        /// </summary>
        public void AssignSeat(GridOccupant seat, int seatIndex, Vector3 seatPosition)
        {
            AssignedSeat = seat;
            SeatIndex = seatIndex;
            SeatPosition = seatPosition;
        }

        /// <summary>Chuyển trạng thái FSM hiện tại: gọi Exit trạng thái cũ rồi Enter trạng thái mới.</summary>
        public void ChangeState(ICustomerState next)
        {
            _state?.Exit(this);
            _state = next;
            _state?.Enter(this);
        }

        /// <summary>Spawn bong bóng order từ pool và hiển thị tên món.</summary>
        public void ShowOrderTicket(RecipeItem recipe)
        {
            if (Ctx?.Pool == null || _ticketAnchor == null) return;

            _ticket = Ctx.Pool.Spawn<OrderTicketController>(PoolKey.OrderTicket, _ticketAnchor.position, Quaternion.identity, _ticketAnchor);
            if (_ticket == null) return;

            _ticket.Bind(Ctx);
            _ticket.Show(recipe);
        }

        /// <summary>Trả bong bóng order về pool.</summary>
        public void HideOrderTicket()
        {
            if (_ticket == null || Ctx?.Pool == null) return;

            Ctx.Pool.Despawn(PoolKey.OrderTicket, _ticket);
            _ticket = null;
        }

        /// <summary>Báo cho CustomerSceneManager biết khách hàng đã rời quán xong, sẵn sàng despawn.</summary>
        public void Finish()
        {
            _onFinished?.Invoke(this);
        }

        public override void OnDespawned()
        {
            HideOrderTicket();
            _state?.Exit(this);
            _state = null;
            Definition = null;
            Order = null;
            AssignedCounter = null;
            AssignedSeat = null;
            SeatReassignRequest = null;
            _mover?.Stop();
            _onFinished = null;

            base.OnDespawned();
        }
    }
}
