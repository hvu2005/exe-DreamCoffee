using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Crafting
{
    /// <summary>
    /// Tap-to-complete crafting. A single player tap = item instantly crafted (no progress bar or energy).
    /// Publishes ItemCrafted; OrderService listens to match open orders.
    /// TODO Phase 2: inject IRecipeRepository to validate recipes; inject IInventoryService to consume ingredients.
    /// </summary>
    public sealed class CraftingService : ICraftingService
    {
        private IEventBus _events;

        public void Init(ServiceContext ctx)
        {
            _events = ctx.Events;
            Debug.Log("[CraftingService] Initialized.");
        }

        public void Shutdown()
        {
            Debug.Log("[CraftingService] Shutdown.");
        }

        public bool TryCraft(string stationId, string itemId, string orderId)
        {
            _events.Publish(new ItemCrafted(itemId, stationId, orderId));
            Debug.Log($"[CraftingService] Crafted '{itemId}' at station '{stationId}'");
            return true;
        }
    }
}
