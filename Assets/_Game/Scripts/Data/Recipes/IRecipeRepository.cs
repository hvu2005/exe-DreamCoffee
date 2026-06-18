namespace DreamCafe.Data
{
    /// <summary>
    /// Read-only access to known recipes. Backed by ScriptableObject now; swappable to remote config later.
    /// TODO: Add GetRecipesByCategory(), UnlockRecipe(), and persistence integration.
    /// </summary>
    public interface IRecipeRepository
    {
        RecipeData GetRecipe(string itemId);
        RecipeData[] GetAllRecipes();
        RecipeData[] GetUnlockedRecipes();
        bool IsUnlocked(string itemId);
    }
}
