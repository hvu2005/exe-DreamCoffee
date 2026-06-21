using DreamCafe.Core.EventBus;
using DreamCafe.Data;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer ordered and is waiting for the player to deliver the item.
    /// Patience continues to drain.
    /// ItemCrafted: station/ticket turn green (visual only — handled elsewhere).
    /// OrderServed matching PendingOrderId: player delivered food → EatingState.
    /// </summary>
    public sealed class WaitingForOrderState : ICustomerState
    {
        public static readonly WaitingForOrderState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.WaitingForOrder;
            owner.View.ShowPatienceBar(true);

            var pendingId = owner.Model.PendingOrderId;
            owner.AddSubscription<OrderServed>(evt =>
            {
                if (evt.OrderId == pendingId)
                    owner.FSM.Enter(EatingState.Instance);
            });
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
