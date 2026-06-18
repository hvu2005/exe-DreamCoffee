using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer is choosing what to order. Patience continues to drain.
    /// TODO Phase 2: display order UI, transition to EatingState after order confirmed.
    /// </summary>
    public sealed class OrderingState : ICustomerState
    {
        public static readonly OrderingState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.Ordering;
        }

        public void Tick(CustomerController owner, float dt)
        {
            owner.Model.TickPatience(dt);
            owner.View.Render(owner.Model);

            if (owner.Model.PatienceDepleted)
                owner.FSM.Enter(LeavingState.Instance);
        }

        public void Exit(CustomerController owner) { }
    }
}
