using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Customer
{
    /// <summary>
    /// Customer lifecycle: timer-based spawning, seat/queue assignment, despawn.
    /// Respects pool cap of 10. Raises CustomerSpawned / CustomerLeft events.
    /// </summary>
    public interface ICustomerService : IService
    {
        int ActiveCustomerCount { get; }
        int MaxCustomers { get; }
        float SpawnIntervalSeconds { get; set; }
        void Tick(float dt);
        void SetSpawnPoint(Vector3 worldPos);
        void RegisterTable(ITable table);
        void UnregisterTable(ITable table);
        void OnCustomerDespawning(string customerId, int tableIndex);
    }
}
