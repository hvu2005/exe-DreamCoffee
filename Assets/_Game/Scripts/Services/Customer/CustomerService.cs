using System.Collections.Generic;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Services.Customer
{
    /// <summary>
    /// Timer-based customer spawning; dine-in/takeaway routing; pool lifecycle management.
    /// Enforces 10-customer active cap. Raises CustomerSpawned / CustomerLeft.
    /// Seats are assigned from the registered ITable list; takeaway uses a world-space queue offset.
    /// </summary>
    public sealed class CustomerService : ICustomerService
    {
        public int ActiveCustomerCount { get; private set; }
        public int MaxCustomers => 10;
        public float SpawnIntervalSeconds { get; set; } = 8f;

        private ServiceContext _ctx;
        private IEventBus _events;
        private float _spawnTimer;
        private int _customerCounter;
        private Vector3 _spawnPoint;
        private CustomerRoster _roster;
        private readonly List<ITable> _tables = new();

        private const float TakeawayChance = 0.2f;
        private const float TakeawayQueueSpacing = 0.7f;

        public void Init(ServiceContext ctx)
        {
            _ctx = ctx;
            _events = ctx.Events;
            Debug.Log("[CustomerService] Initialized. Spawn interval: 8s.");
        }

        public void Shutdown()
        {
            _tables.Clear();
            ActiveCustomerCount = 0;
            Debug.Log("[CustomerService] Shutdown.");
        }

        public void SetSpawnPoint(Vector3 worldPos) => _spawnPoint = worldPos;

        public void RegisterTable(ITable table)
        {
            if (!_tables.Contains(table)) _tables.Add(table);
        }

        public void UnregisterTable(ITable table) => _tables.Remove(table);

        public void OnCustomerDespawning(string customerId, int tableIndex)
        {
            if (tableIndex >= 0)
            {
                for (int i = 0; i < _tables.Count; i++)
                    if (_tables[i].TableIndex == tableIndex) { _tables[i].Vacate(); break; }
            }
            ActiveCustomerCount = Mathf.Max(0, ActiveCustomerCount - 1);
        }

        public void Tick(float dt)
        {
            _spawnTimer += dt;
            if (_spawnTimer >= SpawnIntervalSeconds && ActiveCustomerCount < MaxCustomers)
            {
                _spawnTimer = 0f;
                TrySpawnCustomer();
            }
        }

        private void TrySpawnCustomer()
        {
            if (_roster == null)
                _roster = Resources.Load<CustomerRoster>("CustomerRoster");

            if (_roster == null || _roster.customers.Length == 0)
            {
                Debug.LogWarning("[CustomerService] CustomerRoster not found at Resources/CustomerRoster.asset.");
                return;
            }

            var data = _roster.customers[Random.Range(0, _roster.customers.Length)];
            bool isTakeaway = Random.value < TakeawayChance;

            if (!isTakeaway)
            {
                var table = FindFreeTable();
                if (table == null) return;

                SpawnCustomer(data, false, table, null);
            }
            else
            {
                SpawnCustomer(data, true, null, null);
            }
        }

        private void SpawnCustomer(CustomerData data, bool isTakeaway, ITable table, Transform parent)
        {
            // Spawn as ControllerBase (Core) to avoid Services→Gameplay dependency;
            // cast to ICustomerSpawnable (Services) for initialization.
            var baseCtrl = _ctx.Pool.Spawn<ControllerBase>(PoolKey.Customer, _spawnPoint, Quaternion.identity, parent);
            if (baseCtrl == null) return;

            if (baseCtrl is not ICustomerSpawnable spawnable)
            {
                Debug.LogError("[CustomerService] CustomerPrefab root component does not implement ICustomerSpawnable.");
                return;
            }

            var customerId = $"cus_{++_customerCounter}";
            spawnable.Initialize(_ctx, data, isTakeaway, customerId);

            if (!isTakeaway && table != null)
            {
                table.Occupy(customerId);
                spawnable.AssignSeat(table.SeatTransform.position, table.TableIndex);
            }
            else
            {
                var queuePos = _spawnPoint + Vector3.right * (ActiveCustomerCount * TakeawayQueueSpacing);
                spawnable.AssignTakeawayQueue(queuePos);
            }

            ActiveCustomerCount++;
            _events.Publish(new CustomerSpawned(customerId, data.customerType, isTakeaway));
        }

        private ITable FindFreeTable()
        {
            for (int i = 0; i < _tables.Count; i++)
                if (!_tables[i].IsOccupied) return _tables[i];
            return null;
        }
    }
}
