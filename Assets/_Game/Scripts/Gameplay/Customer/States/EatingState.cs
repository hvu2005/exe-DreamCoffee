using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer received food and is eating. Patience no longer drains.
    /// TODO Phase 2: set eatTimer, transition to LeavingState when done.
    /// </summary>
    public sealed class EatingState : ICustomerState
    {
        public static readonly EatingState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.Eating;
            owner.View.ShowPatienceBar(false);
        }

        public void Tick(CustomerController owner, float dt) { }
        public void Exit(CustomerController owner) { }
    }
}
