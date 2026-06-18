using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer entered the cafe but has not yet been assigned a seat or takeaway queue.
    /// CustomerService drives the assignment; this state just waits and drains patience.
    /// </summary>
    public sealed class WaitingForSeatState : ICustomerState
    {
        public static readonly WaitingForSeatState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.WaitingForSeat;
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
