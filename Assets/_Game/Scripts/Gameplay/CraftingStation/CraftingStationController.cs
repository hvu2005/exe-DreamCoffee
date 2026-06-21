using DreamCafe.Core.EventBus;
using DreamCafe.Core.Interfaces;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using DreamCafe.Services.Crafting;
using UnityEngine;

namespace DreamCafe.Gameplay.CraftingStation
{
    /// <summary>
    /// Scene object for the crafting counter. Implements ICraftingStation (registers with
    /// CraftingService) and ITappable (called by PlayerInputRouter on player click/tap).
    /// Requires a Collider2D on the same GameObject for raycasting.
    /// </summary>
    [RequireComponent(typeof(CraftingStationView))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CraftingStationController : ControllerBase, ICraftingStation, ITappable
    {
        [SerializeField] private string stationId = "station_0";

        public string StationId => stationId;

        private CraftingStationModel _model;
        private CraftingStationView  _view;

        private void Awake()
        {
            _view  = GetComponent<CraftingStationView>();
            _model = new CraftingStationModel { StationId = stationId };
        }

        public override void Bind(ServiceContext ctx)
        {
            base.Bind(ctx);
            _model.StationId = stationId;
            ctx.Services.Resolve<ICraftingService>().RegisterStation(this);

            Subscribe<ItemCrafted>(OnItemCrafted);
            Subscribe<OrderServed>(OnOrderServed);

            _view.Render(_model);
        }

        public override void Unbind()
        {
            Ctx?.Services.Resolve<ICraftingService>().UnregisterStation(this);
            base.Unbind();
        }

        public void OnTap()
        {
            if (Ctx == null) return;
            var crafted = Ctx.Services.Resolve<ICraftingService>().TryCraft(stationId);
            if (!crafted)
                Debug.Log("[CraftingStation] No pending orders.");
        }

        private void OnItemCrafted(ItemCrafted evt)
        {
            if (evt.CraftingStationId != stationId) return;
            _model.Status = CraftingStationStatus.Ready;
            _model.ReadyOrderId = evt.OrderId;
            _view.Render(_model);
        }

        private void OnOrderServed(OrderServed evt)
        {
            if (_model.ReadyOrderId != evt.OrderId) return;
            _model.Reset();
            _view.Render(_model);
        }

        public override void OnSpawned() { }
        public override void OnDespawned() { Unbind(); _model.Reset(); }
    }
}
