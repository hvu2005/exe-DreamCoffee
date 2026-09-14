using DreamCafe.Gameplay.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng di chuyển tới điểm đứng trước quầy order.</summary>
    public sealed class MovingToCounterState : ICustomerState
    {
        public void Enter(CustomerController ctx)
        {
            ctx.View?.Render(new CustomerViewModel(showTimer: false, timerProgress01: 0f));
            ctx.Agent.isStopped = false;
            ctx.Agent.SetDestination(ctx.CounterStandPosition);
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            if (NavMeshArrivalUtility.HasArrived(ctx.Agent))
            {
                ctx.ChangeState(new OrderingState());
            }
        }

        public void Exit(CustomerController ctx)
        {
        }
    }
}
