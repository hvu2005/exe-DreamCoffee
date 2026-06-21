using DreamCafe.Core.Services;

namespace DreamCafe.Data
{
    /// <summary>
    /// Read-only access to known recipes. Backed by ScriptableObject now; swappable to remote config later.
    /// Extends IService so it participates in ServiceManager lifecycle (Init/Shutdown are no-ops).
    /// TODO: Add GetRecipesByCategory(), UnlockRecipe(), and persistence integration.
    /// </summary>
    public interface IRecipeRepository : IService
    {
        RecipeData GetRecipe(string itemId);
        RecipeData[] GetAllRecipes();
        RecipeData[] GetUnlockedRecipes();
        bool IsUnlocked(string itemId);
    }
}
