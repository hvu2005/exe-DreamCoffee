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
        [SerializeField, Tooltip("Bộ làm hình khách nhúc nhích và đổi hình theo hướng đi. Bỏ trống thì tự tìm trên chính nó.")]
        private CustomerStickerAnimator _sticker;

        /// <summary>Ô khách đứng lúc ngồi xuống, để lúc bật dậy trả về đúng chỗ đó.</summary>
        private Vector3? _standUpPoint;

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

        /// <summary>Tâm ô ghế. Khách dừng ở ô đi được sát bên rồi ngồi, không bước vào ô này.</summary>
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
            _sticker = GetComponent<CustomerStickerAnimator>();
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
            _standUpPoint = null;

            Definition = spawnCtx.Definition;
            Order = spawnCtx.Order;

            // Giao hình cho bộ animation ngay khi lấy khách ra khỏi pool: nó còn phải xoá tư thế
            // mà khách lượt trước để lại (đang nhún dở, hoặc đang lật ngược).
            if (_sticker == null) _sticker = GetComponent<CustomerStickerAnimator>();
            if (_sticker != null) _sticker.SetDefinition(Definition);

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

        /// <summary>
        /// Đi tới một điểm. Trả về false nếu không có đường — nơi gọi tự xử.
        ///
        /// <paramref name="enterExactly"/> = false thì dừng ở ô đi được sát đích, không bước vào
        /// đích. Dùng cho ghế: ô ghế bị đồ đạc chiếm nên khách đứng cạnh mà ngồi xuống.
        /// </summary>
        public bool MoveTo(Vector3 worldTarget, bool enterExactly = true) =>
            _mover.SetDestination(worldTarget, enterExactly);

        /// <summary>Đứng lại tại chỗ.</summary>
        public void StopMoving() => _mover.Stop();

        /// <summary>Đã tới đích của lần <see cref="MoveTo"/> gần nhất chưa.</summary>
        public bool HasArrived => _mover.HasArrived;

        /// <summary>
        /// Ngồi vào ghế từ ô đang đứng cạnh nó. Thân khách được ĐẶT thẳng sang ô ghế chứ không đi
        /// bộ nốt quãng cuối: ô ghế bị đồ đạc chiếm nên đó không phải quãng đi được, mà lết vào
        /// mặt ghế ở tốc độ đi bộ thì trông như trượt băng. Chỉ phần hình bay theo sau một cung
        /// ngắn, nên mắt vẫn thấy liền mạch.
        ///
        /// Đặt thân vào đúng tâm ô ghế cũng là thứ cả hệ thống còn lại trông vào: quy tắc
        /// <see cref="SystemControl.Rendering.IsoDepth"/> xếp lớp theo ô cộng slot, khách slot 3
        /// tự nổi trên cái ghế slot 1 cùng ô, còn chìm dưới bàn hay nổi trên bàn là hệ quả của
        /// việc ô ghế nằm sau hay trước ô bàn. Không cần luật xếp lớp riêng nào.
        /// </summary>
        public void SitIntoSeat()
        {
            Vector3 approach = transform.position;
            _standUpPoint = approach;
            _mover.Teleport(SeatPosition);

            if (_sticker == null) return;

            // Nhích hình lên mặt ghế, và quay mặt về phía cái bàn. Đo sau khi đã đặt thân vào ô
            // ghế nên hai số này chỉ phụ thuộc bộ bàn ghế, không phụ thuộc khách đi tới từ phía nào.
            Vector2 offset = Vector2.zero;
            Vector2 face = Vector2.zero;
            if (AssignedSeat != null && SeatIndex >= 0)
            {
                offset = (Vector2)(AssignedSeat.SeatSitPosition(SeatIndex) - transform.position);
                face = (Vector2)(AssignedSeat.AnchorTransform.position - transform.position);
            }

            _sticker.SetSeated(true, offset, face);
            _sticker.FlyFrom(approach - (transform.position + (Vector3)offset));
        }

        /// <summary>
        /// Bật dậy về đúng ô đã đứng lúc ngồi xuống rồi mới tìm đường ra. Không trả về ô đó thì
        /// khách khởi hành từ giữa ô ghế — một ô không đi được — và bước đầu tiên trông như chui
        /// xuyên qua cái ghế.
        /// </summary>
        public void StandUpFromSeat()
        {
            Vector3 fromVisual = transform.position;
            if (_sticker != null && AssignedSeat != null && SeatIndex >= 0)
            {
                fromVisual = AssignedSeat.SeatSitPosition(SeatIndex);
            }

            if (_standUpPoint.HasValue) _mover.Teleport(_standUpPoint.Value);
            _standUpPoint = null;

            if (_sticker == null) return;
            _sticker.SetSeated(false, Vector2.zero, Vector2.zero);
            _sticker.FlyFrom(fromVisual - transform.position);
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
