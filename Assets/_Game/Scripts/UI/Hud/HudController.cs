using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Services;
using DreamCafe.Services.Customer;
using DreamCafe.Services.Economy;
using DreamCafe.Services.Time;
using UnityEngine;

namespace DreamCafe.UI.Hud
{
    /// <summary>
    /// Drives the HUD. Subscribes to economy/customer/time events and polls DayProgress each frame.
    /// Attach alongside HudView on the HUD Canvas root GameObject.
    /// Bound by GameBootstrap.BindSceneControllers via FindObjectsByType.
    /// </summary>
    [RequireComponent(typeof(HudView))]
    public sealed class HudController : ControllerBase
    {
        public HudModel Model { get; private set; }
        public HudView  View  { get; private set; }

        private ITimeService     _timeService;
        private ICustomerService _customerService;
        private bool             _bound;

        public override void Bind(ServiceContext ctx)
        {
            base.Bind(ctx);
            View  = GetComponent<HudView>();
            Model = new HudModel();

            _timeService     = ctx.Services.Resolve<ITimeService>();
            _customerService = ctx.Services.Resolve<ICustomerService>();

            var econ = ctx.Services.Resolve<IEconomyService>();
            Model.Balance     = econ.Balance;
            Model.MaxCustomers = _customerService.MaxCustomers;
            Model.Day         = _timeService.CurrentDay;

            Subscribe<PaymentReceived>(OnPaymentReceived);
            Subscribe<DayStarted>(OnDayStarted);
            Subscribe<DayEnded>(OnDayEnded);
            Subscribe<CustomerSpawned>(_ => Model.ActiveCustomers++);
            Subscribe<CustomerLeft>(_ =>
                Model.ActiveCustomers = Mathf.Max(0, Model.ActiveCustomers - 1));

            View.Init(OnNextDayClicked);
            View.Render(Model);
            _bound = true;
        }

        public override void Unbind()
        {
            _bound = false;
            base.Unbind();
        }

        private void Update()
        {
            if (!_bound) return;
            Model.DayProgress01 = _timeService?.DayProgress ?? 0f;
            View.Render(Model);
        }

        private void OnPaymentReceived(PaymentReceived evt)
        {
            Model.Balance += evt.BaseAmount + evt.Tip;
        }

        private void OnDayStarted(DayStarted evt)
        {
            Model.Day         = evt.DayNumber;
            Model.ShowSummary = false;
            View.ShowSummary(false);
        }

        private void OnDayEnded(DayEnded evt)
        {
            Model.ShowSummary  = true;
            Model.SummaryDay     = evt.DayNumber;
            Model.SummaryRevenue = evt.TotalRevenue;
            Model.SummaryServed  = evt.CustomersServed;
            Model.SummaryLost    = evt.CustomersLost;
            View.RenderSummary(Model);
            View.ShowSummary(true);
        }

        private void OnNextDayClicked()
        {
            Model.ShowSummary    = false;
            Model.ActiveCustomers = 0;
            View.ShowSummary(false);
            Ctx?.Services.Resolve<ITimeService>().StartDay();
        }
    }
}
