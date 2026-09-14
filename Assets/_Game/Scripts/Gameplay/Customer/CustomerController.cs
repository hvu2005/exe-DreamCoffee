using System;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.DataControl;
using DreamCafe.Gameplay.Customer.States;
using DreamCafe.Gameplay.Order;
using DreamCafe.SystemControl.Customer;
using DreamCafe.SystemControl.Decor;
using UnityEngine;
using UnityEngine.AI;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Điều khiển vòng đời AI của một khách hàng: đi tới quầy order, gọi món, đi tới bàn, ngồi
    /// đếm ngược rồi rời quán. Bản thân controller chỉ giữ tham chiếu và chuyển tiếp Tick tới
    /// <see cref="ICustomerState"/> đang active — toàn bộ hành vi nằm trong các state.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CustomerController : ControllerBase
    {
        [Header("Tham chiếu")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private CustomerView _view;
        [SerializeField, Tooltip("Điểm neo để bong bóng order xuất hiện phía trên đầu")]
        private Transform _ticketAnchor;

        private ICustomerState _state;
        private OrderTicketController _ticket;

        public NavMeshAgent Agent => _agent;
        public CustomerView View => _view;

        public CustomerItem Definition { get; private set; }
        public RecipeItem Order { get; private set; }

        public OrderCounterPoint AssignedCounter { get; private set; }
        public int CounterStandIndex { get; private set; }
        public Vector3 CounterStandPosition { get; private set; }

        public DecorSlot AssignedSeat { get; private set; }
        public int SeatIndex { get; private set; }
        public Vector3 SeatPosition { get; private set; }

        public Vector3 ExitPosition { get; private set; }

        private Action<CustomerController> _onFinished;

        private void Reset()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>Nạp toàn bộ dữ liệu spawn và bắt đầu FSM. Gọi ngay sau <see cref="Bind"/>.</summary>
        public void Configure(CustomerSpawnContext spawnCtx)
        {
            // Mesh NavMesh được bake nghiêng -90° quanh X để nằm phẳng trên mặt phẳng XY (xem
            // NavMeshFloor trong scene) — NavMeshAgent sẽ tự xoay Transform theo pháp tuyến bề mặt đó
            // nếu để mặc định. Tắt updatePosition/updateRotation và tự đồng bộ ở Update() để sprite
            // luôn đứng thẳng, đúng mặt phẳng Z=0.
            _agent.updateRotation = false;
            _agent.updatePosition = false;

            Definition = spawnCtx.Definition;
            Order = spawnCtx.Order;

            AssignedCounter = spawnCtx.Counter;
            CounterStandIndex = spawnCtx.StandIndex;
            CounterStandPosition = spawnCtx.StandPoint.position;

            AssignedSeat = spawnCtx.Seat;
            SeatIndex = spawnCtx.SeatIndex;
            SeatPosition = spawnCtx.SeatPoint.position;

            ExitPosition = spawnCtx.ExitPoint.position;
            _onFinished = spawnCtx.OnFinished;

            ChangeState(new MovingToCounterState());
        }

        private void Update()
        {
            _state?.Tick(this, Time.deltaTime);

            Vector3 next = _agent.nextPosition;
            transform.SetPositionAndRotation(new Vector3(next.x, next.y, 0f), Quaternion.identity);
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
            _onFinished = null;

            base.OnDespawned();
        }
    }
}
