using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Customer
{
    /// <summary>
    /// Customer lifecycle: timer-based spawning, seat/queue assignment, despawn.
    /// Tracks daily served/lost counts for DayEnded summary.
    /// </summary>
    public interface ICustomerService : IService
    {
        int ActiveCustomerCount  { get; }
        int MaxCustomers         { get; }
        float SpawnIntervalSeconds { get; set; }
        int DayCustomersServed   { get; }
        int DayCustomersLost     { get; }
        void Tick(float dt);
        void SetSpawnPoint(Vector3 worldPos);
        void RegisterTable(ITable table);
        void UnregisterTable(ITable table);
        void OnCustomerDespawning(string customerId, int tableIndex);
    }
}
