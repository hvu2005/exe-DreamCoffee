using UnityEngine;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Khách đi tới chỗ ngồi đang giữ. Nếu chỗ đó hỏng giữa chừng — bàn bị người chơi dỡ đi, hoặc
    /// bị kê đồ chắn mất lối — thì xin chỗ khác thay vì đứng chôn chân; hết chỗ thì bỏ về.
    ///
    /// Khách chỉ ĐI BỘ tới ô đi được sát bên ghế. Ô ghế bị đồ đạc chiếm nên tìm đường không được
    /// phép đi xuyên qua nó — khách phải vòng tới cạnh ghế như người thật. Tới nơi là ngồi ngay:
    /// quãng cuối từ ô bên cạnh vào mặt ghế do <see cref="CustomerController.SitIntoSeat"/> lo,
    /// đặt thân sang ô ghế và cho phần hình bay nốt vào.
    ///
    /// Trước đây quãng cuối đó cũng đi bộ. Nhưng nó không phải quãng đi được — đi bộ vào giữa một
    /// ô bị đồ đạc chiếm, ở tốc độ đi bộ đã cố tình chỉnh chậm cho thong thả, trông như trượt vào
    /// ghế chứ không như ngồi xuống.
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

            if (!ctx.HasArrived) return;

            // Đã tới ô sát ghế — ngồi luôn, không đi bộ thêm bước nào.
            ctx.ChangeState(new DiningState());
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
                // Chặng 1: chỉ tới ô sát bên, không đi xuyên ô ghế.
                if (seatUsable && ctx.MoveTo(ctx.SeatPosition, enterExactly: false)) return true;

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
