using DreamCafe.Gameplay.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng di chuyển tới điểm đứng trước quầy order.</summary>
    public sealed class MovingToCounterState : ICustomerState
    {
        public void Enter(CustomerController ctx)
        {
            ctx.View?.Render(new CustomerViewModel(showTimer: false, timerProgress01: 0f));

            // Bít đường tới quầy (người chơi kê đồ chắn lối) thì khỏi vào quán nữa, đi thẳng ra.
            if (!ctx.MoveTo(ctx.CounterStandPosition))
            {
                ctx.ChangeState(new LeavingState());
            }
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            if (ctx.HasArrived)
            {
                ctx.ChangeState(new OrderingState());
            }
        }

        public void Exit(CustomerController ctx)
        {
        }
    }
}
