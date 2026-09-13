using DreamCafe.Core.MVC;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>Dữ liệu hiển thị cho thanh đếm ngược (patience bar) phía trên đầu khách hàng.</summary>
    public readonly struct CustomerViewModel : IModel
    {
        public readonly bool ShowTimer;
        public readonly float TimerProgress01;

        public CustomerViewModel(bool showTimer, float timerProgress01)
        {
            ShowTimer = showTimer;
            TimerProgress01 = timerProgress01;
        }
    }
}
