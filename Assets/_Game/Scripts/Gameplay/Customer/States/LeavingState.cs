using DreamCafe.Gameplay.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng trả lại ghế rồi rời quán về điểm exit/spawn để despawn.</summary>
    public sealed class LeavingState : ICustomerState
    {
        public void Enter(CustomerController ctx)
        {
            ctx.AssignedSeat?.ReleaseSeat(ctx.SeatIndex);

            // Không tìm nổi đường ra (bị quây kín) thì biến mất luôn, còn hơn đứng chôn chân giữa quán.
            if (!ctx.MoveTo(ctx.ExitPosition)) ctx.Finish();
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            if (ctx.HasArrived)
            {
                ctx.Finish();
            }
        }

        public void Exit(CustomerController ctx)
        {
        }
    }
}
