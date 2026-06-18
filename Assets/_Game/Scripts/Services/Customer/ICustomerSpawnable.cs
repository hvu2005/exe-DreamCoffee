using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Services.Customer
{
    /// <summary>
    /// Implemented by CustomerController (Gameplay layer) so CustomerService can initialize
    /// a freshly pooled customer without a Services→Gameplay dependency.
    /// Defined here (Services layer) — CustomerController reaches up to implement it.
    /// </summary>
    public interface ICustomerSpawnable
    {
        void Initialize(ServiceContext ctx, CustomerData data, bool isTakeaway, string customerId);
        void AssignSeat(Vector3 worldPos, int tableIndex);
        void AssignTakeawayQueue(Vector3 worldPos);
    }
}
