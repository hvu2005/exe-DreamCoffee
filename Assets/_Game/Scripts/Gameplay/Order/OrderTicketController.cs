using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Manages a single order ticket. Spawned by OrderTicketSpawner when an order is placed.
    /// Turns green when the matching ItemCrafted fires; despawns on OrderServed.
    /// </summary>
    [RequireComponent(typeof(OrderTicketView))]
    public sealed class OrderTicketController : ControllerBase
    {
        public OrderTicketModel Model { get; private set; }
        public OrderTicketView  View  { get; private set; }

        private void Awake()
        {
            View  = GetComponent<OrderTicketView>();
            Model = new OrderTicketModel();
        }

        public void Initialize(ServiceContext ctx, string orderId, string customerId, string itemId)
        {
            Bind(ctx);
            Model.Reset();
            Model.OrderId    = orderId;
            Model.CustomerId = customerId;
            Model.ItemId     = itemId;
            Model.Status     = OrderStatus.Pending;

            // Resolve display name from recipe repository (null-safe: falls back to itemId).
            var repo = ctx.Services.Resolve<IRecipeRepository>();
            var recipe = repo?.GetRecipe(itemId);
            Model.ItemName = recipe?.outputItem?.displayName;

            Subscribe<ItemCrafted>(OnItemCrafted);
            Subscribe<OrderServed>(OnOrderServed);

            View.Render(Model);
        }

        private void OnItemCrafted(ItemCrafted evt)
        {
            if (evt.OrderId != Model.OrderId) return;
            Model.Status = OrderStatus.Ready;
            View.Render(Model);
        }

        private void OnOrderServed(OrderServed evt)
        {
            if (evt.OrderId != Model.OrderId) return;
            Ctx.Pool.Despawn(PoolKey.OrderTicket, this);
        }

        public override void OnSpawned() { }
        public override void OnDespawned() { Unbind(); Model.Reset(); }
    }
}
