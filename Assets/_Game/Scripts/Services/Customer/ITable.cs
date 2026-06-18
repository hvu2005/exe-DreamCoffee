using UnityEngine;

namespace DreamCafe.Services.Customer
{
    /// <summary>
    /// Scene-side table abstraction consumed by CustomerService.
    /// Defined in the Services layer so CustomerService has no Gameplay dependency.
    /// TableController (Gameplay layer) implements this interface.
    /// </summary>
    public interface ITable
    {
        int TableIndex { get; }
        bool IsOccupied { get; }
        Transform SeatTransform { get; }
        void Occupy(string customerId);
        void Vacate();
    }
}
