using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Ingredient stock management: restocking, spoilage, out-of-stock detection. Stub — implement post-prototype.
    /// TODO: Implement daily market, expiry countdown, out-of-stock reputation penalty.
    /// </summary>
    public interface IInventoryService : IService
    {
        int GetStock(string ingredientId);
        bool ConsumeIngredients(string[] ingredientIds);
        bool HasIngredients(string[] ingredientIds);
    }
}
