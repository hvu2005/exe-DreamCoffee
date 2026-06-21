using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// ScriptableObject-backed IRecipeRepository. Assign recipe assets in the Inspector.
    /// Create via Assets > Create > DreamCafe > Data > RecipeRepository.
    /// TODO: Add runtime unlock state persistence via SaveService.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeRepository", menuName = "DreamCafe/Data/RecipeRepository")]
    public sealed class ScriptableRecipeRepository : ScriptableObject, IRecipeRepository
    {
        [SerializeField] private RecipeData[] recipes = System.Array.Empty<RecipeData>();

        // IService — data only, no lifecycle work needed
        public void Init(ServiceContext ctx) => Debug.Log($"[RecipeRepository] Initialized — {recipes.Length} recipes.");
        public void Shutdown() { }

        /// <summary>Editor-only helper called by DreamCafeSetup to populate recipes without reflection.</summary>
        public void SetRecipes(RecipeData[] data) => recipes = data ?? System.Array.Empty<RecipeData>();

        public RecipeData GetRecipe(string itemId) =>
            recipes.FirstOrDefault(r => r != null && r.recipeId == itemId);

        public RecipeData[] GetAllRecipes() => recipes;

        public RecipeData[] GetUnlockedRecipes() =>
            recipes.Where(r => r != null && r.isUnlockedByDefault).ToArray();

        public bool IsUnlocked(string itemId) =>
            recipes.Any(r => r != null && r.recipeId == itemId && r.isUnlockedByDefault);
    }
}
