using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Services;
using DreamCafe.Services.Economy;
using DreamCafe.Services.Time;
using DreamCafe.Services.Upgrade;
using System;
using UnityEngine;

namespace DreamCafe.UI.Shop
{
    /// <summary>
    /// Controls the end-of-day upgrade shop overlay.
    /// Appears on DayEnded (sits above the HUD summary on sortingOrder=20).
    /// "Start Day ▶" starts the next day, closing the shop;
    ///   DayStarted fires → HudController hides its summary automatically.
    /// Bound by GameBootstrap.BindSceneControllers via FindObjectsByType.
    /// </summary>
    [RequireComponent(typeof(UpgradeShopView))]
    public sealed class UpgradeShopController : ControllerBase
    {
        private UpgradeShopView  _view;
        private IUpgradeService  _upgrades;
        private IEconomyService  _economy;
        private ITimeService     _time;

        public override void Bind(ServiceContext ctx)
        {
            base.Bind(ctx);
            _view     = GetComponent<UpgradeShopView>();
            _upgrades = ctx.Services.Resolve<IUpgradeService>();
            _economy  = ctx.Services.Resolve<IEconomyService>();
            _time     = ctx.Services.Resolve<ITimeService>();

            // Build one buy-callback per card slot — UpgradeShopView matches by index.
            // We pass delegates; Init wires them to the Buy button onClick events.
            Action<string>[] buyCbs = new Action<string>[4];
            for (int i = 0; i < buyCbs.Length; i++)
                buyCbs[i] = OnBuyClicked;

            _view.Init(OnStartDayClicked, buyCbs);

            Subscribe<DayEnded>(OnDayEnded);
            Subscribe<UpgradePurchased>(_ => RefreshCards());
        }

        private void OnDayEnded(DayEnded evt)
        {
            _view.RenderSummary(evt.DayNumber, evt.TotalRevenue, evt.CustomersServed, evt.CustomersLost);
            RefreshCards();
            _view.ShowShop(true);
        }

        private void OnBuyClicked(string upgradeId)
        {
            _upgrades.Purchase(upgradeId);
            // RefreshCards is driven by UpgradePurchased subscription above.
        }

        private void OnStartDayClicked()
        {
            _view.ShowShop(false);
            _time.StartDay();
        }

        private void RefreshCards()
        {
            _view.RenderCards(
                _upgrades.GetLevel,
                _upgrades.NextCost,
                _economy.Balance);
        }
    }
}
