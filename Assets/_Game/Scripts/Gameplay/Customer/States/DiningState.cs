using DreamCafe.Gameplay.Customer;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Khách hàng ngồi tại bàn, đếm ngược thời gian dùng bữa (patience bar hiển thị timer).</summary>
    public sealed class DiningState : ICustomerState
    {
        private float _elapsed;

        public void Enter(CustomerController ctx)
        {
            ctx.Agent.isStopped = true;
            _elapsed = 0f;
            ctx.View?.Render(new CustomerViewModel(showTimer: true, timerProgress01: 1f));
        }

        public void Tick(CustomerController ctx, float deltaTime)
        {
            _elapsed += deltaTime;
            float total = Mathf.Max(0.01f, ctx.Definition.DiningTimeSeconds);
            float remaining01 = Mathf.Clamp01(1f - _elapsed / total);
            ctx.View?.Render(new CustomerViewModel(showTimer: true, timerProgress01: remaining01));

            if (_elapsed >= total)
            {
                ctx.ChangeState(new LeavingState());
            }
        }

        public void Exit(CustomerController ctx)
        {
            ctx.View?.Render(new CustomerViewModel(showTimer: false, timerProgress01: 0f));
            ctx.Agent.isStopped = false;
        }
    }
}
