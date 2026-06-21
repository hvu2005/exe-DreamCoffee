using DreamCafe.Core.EventBus;
using DreamCafe.Data;
using DreamCafe.Services.Order;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Customer auto-selects a random unlocked recipe and places the order.
    /// Immediately transitions to WaitingForOrderState after order is placed.
    /// Enter() is instant — no Tick logic needed.
    /// </summary>
    public sealed class OrderingState : ICustomerState
    {
        public static readonly OrderingState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.Ordering;

            var orderService = owner.Context.Services.Resolve<IOrderService>();
            var recipeRepo   = owner.Context.Services.Resolve<IRecipeRepository>();
            var recipes      = recipeRepo.GetUnlockedRecipes();

            if (recipes == null || recipes.Length == 0)
            {
                Debug.LogWarning($"[OrderingState] No unlocked recipes — customer {owner.Model.CustomerId} leaving.");
                owner.FSM.Enter(LeavingState.Instance);
                return;
            }

            var recipe  = recipes[Random.Range(0, recipes.Length)];
            var itemId  = recipe.outputItem != null ? recipe.outputItem.itemId : "unknown";
            var price   = recipe.outputItem != null ? recipe.outputItem.basePrice : 0f;

            owner.Model.OrderItemId    = itemId;
            owner.Model.PendingOrderId = orderService.PlaceOrder(owner.Model.CustomerId, itemId, price, owner.Model.Type);

            owner.FSM.Enter(WaitingForOrderState.Instance);
        }

        public void Tick(CustomerController owner, float dt) { }
        public void Exit(CustomerController owner) { }
    }
}
