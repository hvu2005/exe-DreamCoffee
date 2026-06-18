using DreamCafe.Core.Services;

namespace DreamCafe.Services.Crafting
{
    /// <summary>
    /// Tap-to-complete crafting. Validates recipe via IRecipeRepository, raises ItemCrafted instantly.
    /// No progress bar — single tap = item complete (Manager Energy mechanic dropped permanently).
    /// TODO Phase 2: integrate IRecipeRepository, IInventoryService ingredient check.
    /// </summary>
    public interface ICraftingService : IService
    {
        bool TryCraft(string stationId, string itemId, string orderId);
    }
}
