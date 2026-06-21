using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer received their item and is eating/drinking.
    /// Patience no longer drains. After EatDuration seconds → LeavingState (satisfied).
    /// </summary>
    public sealed class EatingState : ICustomerState
    {
        public static readonly EatingState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State       = CustomerState.Eating;
            owner.Model.WasSatisfied = true;
            owner.Model.EatTimer    = owner.Model.EatDuration;
            owner.View.ShowPatienceBar(false);
        }

        public void Tick(CustomerController owner, float dt)
        {
            owner.Model.EatTimer -= dt;
            if (owner.Model.EatTimer <= 0f)
                owner.FSM.Enter(LeavingState.Instance);
        }

        public void Exit(CustomerController owner) { }
    }
}
