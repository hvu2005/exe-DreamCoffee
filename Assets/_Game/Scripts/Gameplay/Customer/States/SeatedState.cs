using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer is seated. Patience drains until order is placed (Phase 2) or patience runs out.
    /// TODO Phase 2: transition to OrderingState when player interacts.
    /// </summary>
    public sealed class SeatedState : ICustomerState
    {
        public static readonly SeatedState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.Seated;
            owner.View.ShowPatienceBar(true);
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
