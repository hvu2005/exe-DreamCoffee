using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Config for a crafting recipe. Links required ingredient IDs to an output ItemData.
    /// Create via Assets > Create > DreamCafe > Data > Recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeData", menuName = "DreamCafe/Data/Recipe")]
    public sealed class RecipeData : ScriptableObject
    {
        public string recipeId;
        public ItemData outputItem;
        public string[] requiredIngredientIds;
        public bool isUnlockedByDefault = true;
    }
}
