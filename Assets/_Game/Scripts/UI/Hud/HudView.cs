using System;
using DreamCafe.Core.MVC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.UI.Hud
{
    /// <summary>
    /// Canvas-based HUD presenter. Pure presentation — no EventBus or service access.
    /// Sits on the same GameObject as HudController.
    /// All fields are wired via the Phase4Setup editor tool or manually in the Inspector.
    /// </summary>
    public sealed class HudView : ViewBase
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private TMP_Text customersLabel;

        [Header("Day Progress Bar")]
        [SerializeField] private Image dayProgressFill;

        [Header("End-of-Day Summary")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text   summaryTitleLabel;
        [SerializeField] private TMP_Text   summaryRevenueLabel;
        [SerializeField] private TMP_Text   summaryServedLabel;
        [SerializeField] private TMP_Text   summaryLostLabel;
        [SerializeField] private Button     nextDayButton;

        public override void Render(IModel model)
        {
            var m = (HudModel)model;
            if (balanceLabel   != null) balanceLabel.text   = $"{m.Balance:N0}đ";
            if (dayLabel       != null) dayLabel.text       = $"Day {m.Day}";
            if (customersLabel != null) customersLabel.text = $"{m.ActiveCustomers}/{m.MaxCustomers}";
            if (dayProgressFill != null) dayProgressFill.fillAmount = m.DayProgress01;
        }

        public void RenderSummary(HudModel m)
        {
            if (summaryTitleLabel   != null) summaryTitleLabel.text   = $"Day {m.SummaryDay} Done!";
            if (summaryRevenueLabel != null) summaryRevenueLabel.text = $"Revenue  {m.SummaryRevenue:N0}đ";
            if (summaryServedLabel  != null) summaryServedLabel.text  = $"Served   {m.SummaryServed}";
            if (summaryLostLabel    != null) summaryLostLabel.text    = $"Lost     {m.SummaryLost}";
        }

        public void ShowSummary(bool show) => summaryPanel?.SetActive(show);

        /// <summary>Wires the Next Day button. Called once by HudController.Bind().</summary>
        public void Init(Action onNextDay)
        {
            if (nextDayButton != null)
                nextDayButton.onClick.AddListener(() => onNextDay?.Invoke());
            summaryPanel?.SetActive(false);
        }
    }
}
