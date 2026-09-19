using UnityEngine;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Khách đi tới chỗ ngồi đang giữ. Nếu chỗ đó hỏng giữa chừng — bàn bị người chơi dỡ đi, hoặc
    /// bị kê đồ chắn mất lối — thì xin chỗ khác thay vì đứng chôn chân; hết chỗ thì bỏ về.
    /// </summary>
    public sealed class MovingToSeatState : ICustomerState
    {
        /// <summary>Số lần chấp nhận đổi bàn trong một lượt, chặn vòng lặp xin–hỏng–xin vô tận.</summary>
        private const int MaxReassign = 3;

        private int _reassignCount;

        public void Enter(CustomerController ctx)
        {
            if (!TryHeadToSeat(ctx)) ctx.ChangeState(new LeavingState());
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            // Bàn bị dỡ ngay lúc khách đang đi tới: đổi bàn luôn, không thì khách ngồi giữa sàn trống.
            if (ctx.AssignedSeat == null || !ctx.AssignedSeat.isActiveAndEnabled)
            {
                if (!TryHeadToSeat(ctx)) ctx.ChangeState(new LeavingState());
                return;
            }

            if (ctx.HasArrived) ctx.ChangeState(new DiningState());
        }

        public void Exit(CustomerController ctx)
        {
        }

        /// <summary>
        /// Nhắm tới ghế hiện tại; chỗ hỏng hoặc bít đường thì xin chỗ mới rồi thử lại.
        /// </summary>
        private bool TryHeadToSeat(CustomerController ctx)
        {
            while (_reassignCount <= MaxReassign)
            {
                bool seatUsable = ctx.AssignedSeat != null && ctx.AssignedSeat.isActiveAndEnabled;
                if (seatUsable && ctx.MoveTo(ctx.SeatPosition)) return true;

                _reassignCount++;
                if (ctx.SeatReassignRequest == null || !ctx.SeatReassignRequest(ctx))
                {
                    Debug.Log("[Customer] Quán hết chỗ ngồi dùng được — khách bỏ về.");
                    return false;
                }
            }

            return false;
        }
    }
}
