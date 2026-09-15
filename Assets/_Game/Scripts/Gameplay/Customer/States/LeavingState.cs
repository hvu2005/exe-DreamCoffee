using DreamCafe.Gameplay.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng trả lại ghế rồi rời quán về điểm exit/spawn để despawn.</summary>
    public sealed class LeavingState : ICustomerState
    {
        public void Enter(CustomerController ctx)
        {
            ctx.AssignedSeat?.SetSeatOccupied(ctx.SeatIndex, false);
            ctx.Agent.SetDestination(ctx.ExitPosition);
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            if (NavMeshArrivalUtility.HasArrived(ctx.Agent))
            {
                ctx.Finish();
            }
        }

        public void Exit(CustomerController ctx)
        {
        }
    }
}
