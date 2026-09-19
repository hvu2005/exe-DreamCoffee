using DreamCafe.Gameplay.Customer;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng đứng tại quầy, gọi món và chờ một khoảng thời gian cố định trước khi ra bàn.</summary>
    public sealed class OrderingState : ICustomerState
    {
        private const float OrderDisplaySeconds = 2f;

        private float _elapsed;

        public void Enter(CustomerController ctx)
        {
            ctx.StopMoving();
            _elapsed = 0f;
            ctx.ShowOrderTicket(ctx.Order);
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= OrderDisplaySeconds)
            {
                ctx.ChangeState(new MovingToSeatState());
            }
        }

        public void Exit(CustomerController ctx)
        {
            ctx.HideOrderTicket();
            ctx.AssignedCounter?.SetStandOccupied(ctx.CounterStandIndex, false);
        }
    }
}
