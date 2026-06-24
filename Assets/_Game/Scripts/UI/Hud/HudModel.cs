using DreamCafe.Core.MVC;

namespace DreamCafe.UI.Hud
{
    /// <summary>
    /// Runtime state for the HUD. Driven by HudController via EventBus subscriptions.
    /// </summary>
    public sealed class HudModel : IModel
    {
        public float Balance;
        public int   Day;
        public int   ActiveCustomers;
        public int   MaxCustomers;
        public float DayProgress01;

        // End-of-day summary
        public bool  ShowSummary;
        public int   SummaryDay;
        public float SummaryRevenue;
        public int   SummaryServed;
        public int   SummaryLost;
    }
}
