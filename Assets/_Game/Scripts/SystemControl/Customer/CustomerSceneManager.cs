using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.DataControl;
using DreamCafe.Gameplay.Customer;
using DreamCafe.SystemControl.Decor;
using UnityEngine;
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
        [SerializeField] private List<DecorSlot> _seats = new();

        [Header("Cấu hình Spawn")]
        [SerializeField, Min(0.5f)] private float _spawnIntervalSeconds = 5f;
        [SerializeField, Min(1)] private int _maxActiveCustomers = 4;
        [SerializeField, Min(0)] private int _prewarmCount = 4;

        private DataCustomerController _customerData;
        private RecipeController _recipeController;
        private PoolManager _pool;
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
            _serviceContext = new ServiceContext(new EventBus(), new ServiceManager(), _pool);

            _pool.Prewarm(PoolKey.Customer, _prewarmCount);
            _pool.Prewarm(PoolKey.OrderTicket, _prewarmCount);
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < _spawnIntervalSeconds) return;

            _spawnTimer = 0f;
            TrySpawnCustomer();
        }

        private void TrySpawnCustomer()
        {
            if (_customerData == null || _recipeController == null) return;
            if (_activeCustomers.Count >= _maxActiveCustomers) return;

            var unlocked = _customerData.GetUnlocked();
            if (unlocked.Count == 0) return;

            if (!TryFindFreeCounter(out var counter, out var standPoint, out int standIndex)) return;
            if (!TryFindFreeSeat(out var seat, out var seatPoint, out int seatIndex)) return;

            counter.SetStandOccupied(standIndex, true);
            seat.SetSeatOccupied(seatIndex, true);

            var definition = unlocked[Random.Range(0, unlocked.Count)];
            var order = OrderSelector.PickOrder(definition, _recipeController);

            var customer = _pool.Spawn<GameplayCustomerController>(PoolKey.Customer, _spawnPoint.position, Quaternion.identity);
            if (customer == null)
            {
                counter.SetStandOccupied(standIndex, false);
                seat.SetSeatOccupied(seatIndex, false);
                return;
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
                SeatPoint = seatPoint,
                ExitPoint = _spawnPoint,
                OnFinished = ReleaseCustomer
            });

            _activeCustomers.Add(customer);
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

        private bool TryFindFreeSeat(out DecorSlot seat, out Transform seatPoint, out int seatIndex)
        {
            foreach (var candidate in _seats)
            {
                if (candidate == null) continue;

                var point = candidate.GetFreeSeat(out int idx);
                if (point != null)
                {
                    seat = candidate;
                    seatPoint = point;
                    seatIndex = idx;
                    return true;
                }
            }

            seat = null;
            seatPoint = null;
            seatIndex = -1;
            return false;
        }

        /// <summary>Callback gọi bởi CustomerController khi khách hàng đã rời quán xong.</summary>
        private void ReleaseCustomer(GameplayCustomerController customer)
        {
            _activeCustomers.Remove(customer);
            _pool.Despawn(PoolKey.Customer, customer);
        }
    }
}
