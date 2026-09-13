using DreamCafe.Gameplay.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng di chuyển tới ghế đã được giữ chỗ sẵn.</summary>
    public sealed class MovingToSeatState : ICustomerState
    {
        public void Enter(CustomerController ctx)
        {
            ctx.Agent.SetDestination(ctx.SeatPosition);
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            if (NavMeshArrivalUtility.HasArrived(ctx.Agent))
            {
                ctx.ChangeState(new DiningState());
            }
        }

        public void Exit(CustomerController ctx)
        {
        }
    }
}
