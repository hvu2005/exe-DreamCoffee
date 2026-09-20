using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.DataControl;
using DreamCafe.Gameplay.Customer;
using DreamCafe.SystemControl.Decor;
using DreamCafe.SystemControl.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;
using DataCustomerController = DreamCafe.DataControl.CustomerController;
using GameplayCustomerController = DreamCafe.Gameplay.Customer.CustomerController;

namespace DreamCafe.SystemControl.Customer
{
    /// <summary>
    /// Điều phối viên spawn khách hàng trong Scene: chọn định nghĩa khách + món, giữ chỗ quầy/ghế trống,
    /// rồi spawn một <see cref="GameplayCustomerController"/> từ pool. Không queueing — nếu quầy hoặc bàn
    /// đang đầy, đơn giản là bỏ qua lượt spawn đó (đúng theo phạm vi đã chốt cho iteration này).
    /// </summary>
    public sealed class CustomerSceneManager : MonoBehaviour
    {
        [Header("Điểm neo trong Scene")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private List<OrderCounterPoint> _counters = new();

        [Header("Cấu hình Spawn")]
        [SerializeField, Tooltip("Tự động spawn khách theo chu kỳ. Tắt đi nếu chỉ muốn gọi khách bằng phím debug.")]
        private bool _autoSpawn = false;

        [SerializeField, Min(0.5f)] private float _spawnIntervalSeconds = 5f;
        [SerializeField, Min(1)] private int _maxActiveCustomers = 4;
        [SerializeField, Min(0)] private int _prewarmCount = 4;

        [Header("Chỗ ngồi")]
        [SerializeField, Tooltip("Cho phép khách ngồi chung bàn với người lạ khi hết bàn trống. Tắt = mỗi bàn một nhóm khách.")]
        private bool _allowTableSharing = false;

        [Header("Debug")]
        [SerializeField, Tooltip("Phím gọi tay 1 khách vào quán. Đặt None để tắt.")]
        private Key _spawnKey = Key.C;

        private DataCustomerController _customerData;
        private RecipeController _recipeController;
        private PoolManager _pool;
        private ServiceManager _services;
        private ServiceContext _serviceContext;
        private readonly List<GameplayCustomerController> _activeCustomers = new();
        private float _spawnTimer;

        private void Start()
        {
            var systems = GameSystemsProvider.Instance;
            if (systems == null)
            {
                Debug.LogError("[CustomerSceneManager] Không tìm thấy GameSystemsProvider trong scene.");
                return;
            }

            _customerData = systems.Customer;
            _recipeController = systems.Recipe;

            _pool = new PoolManager(new ResourcesPrefabLoader(), _poolRoot);

            // Đăng ký theo interface để khách chỉ phụ thuộc vào hợp đồng tìm đường, không phụ thuộc
            // vào việc hôm nay nó chạy A* trên lưới ô.
            _services = new ServiceManager();
            _services.Register<IPathfindingService>(new GridPathfindingService());

            _serviceContext = new ServiceContext(new EventBus(), _services, _pool);
            _services.InitAll(_serviceContext);

            _pool.Prewarm(PoolKey.Customer, _prewarmCount);
            _pool.Prewarm(PoolKey.OrderTicket, _prewarmCount);
        }

        private void Update()
        {
            HandleDebugInput();

            if (!_autoSpawn) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < _spawnIntervalSeconds) return;

            _spawnTimer = 0f;
            TrySpawnCustomer();
        }

        private void HandleDebugInput()
        {
            if (_spawnKey == Key.None) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_spawnKey].wasPressedThisFrame)
            {
                SpawnOneCustomer();
            }
        }

        /// <summary>
        /// Gọi đúng 1 khách vào quán ngay lập tức — dùng cho phím debug hoặc script test.
        /// Trả về false kèm log lý do nếu chưa spawn được (hết ghế, hết chỗ đứng quầy, quá tải...).
        /// </summary>
        public bool SpawnOneCustomer() => TrySpawnCustomer();

        private bool TrySpawnCustomer()
        {
            if (_customerData == null || _recipeController == null)
            {
                Debug.LogWarning("[CustomerSceneManager] Chưa bind được dữ liệu — GameSystemsProvider có trong scene không?");
                return false;
            }

            if (_activeCustomers.Count >= _maxActiveCustomers)
            {
                Debug.Log($"[CustomerSceneManager] Quán đang đủ {_maxActiveCustomers} khách, bỏ qua lượt gọi khách.");
                return false;
            }

            var unlocked = _customerData.GetUnlocked();
            if (unlocked.Count == 0)
            {
                Debug.LogWarning("[CustomerSceneManager] Chưa mở khóa loại khách nào trong CustomerRepository.");
                return false;
            }

            if (_spawnPoint == null)
            {
                Debug.LogWarning("[CustomerSceneManager] Chưa gán Spawn Point.");
                return false;
            }

            if (!TryFindFreeCounter(out var counter, out var standPoint, out int standIndex))
            {
                Debug.Log("[CustomerSceneManager] Quầy order hết chỗ đứng trống.");
                return false;
            }

            if (!TryFindFreeSeat(null, out var seat, out var seatIndex))
            {
                Debug.Log(_allowTableSharing
                    ? "[CustomerSceneManager] Hết ghế trống — mua thêm bàn ghế đi đã."
                    : "[CustomerSceneManager] Không còn bàn nào trống hẳn — khách không vào.");
                return false;
            }

            counter.SetStandOccupied(standIndex, true);

            var definition = unlocked[Random.Range(0, unlocked.Count)];
            var order = OrderSelector.PickOrder(definition, _recipeController);

            var customer = _pool.Spawn<GameplayCustomerController>(PoolKey.Customer, _spawnPoint.position, Quaternion.identity);
            if (customer == null)
            {
                counter.SetStandOccupied(standIndex, false);
                seat.ReleaseSeat(seatIndex);
                Debug.LogWarning($"[CustomerSceneManager] Pool không tạo được khách từ '{PoolKey.Customer}'.");
                return false;
            }

            customer.Bind(_serviceContext);
            customer.Configure(new CustomerSpawnContext
            {
                Definition = definition,
                Order = order,
                Counter = counter,
                StandIndex = standIndex,
                StandPoint = standPoint,
                Seat = seat,
                SeatIndex = seatIndex,
                SeatPoint = seat.SeatWorldPosition(seatIndex),
                ExitPoint = _spawnPoint,
                OnFinished = ReleaseCustomer,
                SeatReassignRequest = TryReassignSeat
            });

            _activeCustomers.Add(customer);
            Debug.Log($"[CustomerSceneManager] Khách '{definition.DisplayName}' vào quán, gọi món '{order?.DisplayName ?? "(chưa có món nào mở khóa)"}'.");
            return true;
        }

        private bool TryFindFreeCounter(out OrderCounterPoint counter, out Transform standPoint, out int standIndex)
        {
            foreach (var candidate in _counters)
            {
                if (candidate == null) continue;

                var point = candidate.GetFreeStandPoint(out int idx);
                if (point != null)
                {
                    counter = candidate;
                    standPoint = point;
                    standIndex = idx;
                    return true;
                }
            }

            counter = null;
            standPoint = null;
            standIndex = -1;
            return false;
        }

        /// <summary>
        /// Tìm chỗ ngồi cho khách trong bản đồ ô: duyệt các món có khai báo ghế
        /// (<see cref="GridOccupant"/>), ưu tiên món chưa có ai ngồi. Chỉ khi bật
        /// <see cref="_allowTableSharing"/> mới chấp nhận ngồi ghép bàn đã có người.
        /// Ghế được giữ chỗ ngay tại đây nên hai khách không bao giờ nhắm cùng một ô.
        /// </summary>
        /// <param name="exclude">Món cần bỏ qua (bàn vừa hỏng / vừa bị bít đường).</param>
        private bool TryFindFreeSeat(GridOccupant exclude, out GridOccupant seat, out int seatIndex)
        {
            var grid = ShopGrid.Instance;
            seat = null;
            seatIndex = -1;
            if (grid == null) return false;

            // Vòng 1: bàn còn trống hẳn. Vòng 2 (nếu cho ngồi ghép): bàn nào còn ghế cũng được.
            int passes = _allowTableSharing ? 2 : 1;

            for (int pass = 0; pass < passes; pass++)
            {
                bool emptyTablesOnly = pass == 0;

                foreach (var occupant in grid.Occupants)
                {
                    if (occupant == null || occupant == exclude || occupant.SeatCount == 0) continue;
                    if (emptyTablesOnly && occupant.HasOccupiedSeat()) continue;

                    if (occupant.TryReserveSeat(out int index))
                    {
                        seat = occupant;
                        seatIndex = index;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Khách đang đi mà chỗ ngồi hỏng (bàn bị dỡ, bị kê đồ chắn lối) thì xin chỗ khác qua đây.
        /// Trả ghế cũ về rồi giữ một ghế mới ở món khác; false = quán thật sự hết chỗ.
        /// </summary>
        private bool TryReassignSeat(GameplayCustomerController customer)
        {
            if (customer == null) return false;

            var oldSeat = customer.AssignedSeat;
            oldSeat?.ReleaseSeat(customer.SeatIndex);

            if (!TryFindFreeSeat(oldSeat, out var seat, out int seatIndex))
            {
                customer.AssignSeat(null, -1, customer.transform.position);
                return false;
            }

            customer.AssignSeat(seat, seatIndex, seat.SeatWorldPosition(seatIndex));
            Debug.Log($"[CustomerSceneManager] Đổi chỗ cho khách sang '{seat.name}' (ghế {seatIndex}).");
            return true;
        }

        /// <summary>Callback gọi bởi CustomerController khi khách hàng đã rời quán xong.</summary>
        private void ReleaseCustomer(GameplayCustomerController customer)
        {
            _activeCustomers.Remove(customer);
            _pool.Despawn(PoolKey.Customer, customer);
        }
    }
}
