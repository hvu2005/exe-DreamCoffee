using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject chứa danh sách toàn bộ RecipeItem definition — cấu hình trên Inspector.
    /// Có thể gán vào DatabaseManager để các hệ thống khác truy xuất.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeRepository", menuName = "DreamCafe/Data/RecipeRepository")]
    public sealed class ScriptableRecipeRepository : ScriptableObject, IRecipeRepository
    {
        [SerializeField]
        private RecipeItem[] _recipes = Array.Empty<RecipeItem>();

        /// <summary>Khởi tạo repository trong ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[RecipeRepository] Initialized — {_recipes.Length} recipes.");
        }

        /// <summary>Dọn dẹp tài nguyên.</summary>
        public void Shutdown()
        {
        }

        /// <summary>Lấy công thức theo ID.</summary>
        public RecipeItem GetRecipe(string id) =>
            _recipes.FirstOrDefault(r => r != null && r.Id == id);

        /// <summary>Lấy toàn bộ công thức.</summary>
        public RecipeItem[] GetAllRecipes() => _recipes;

        /// <summary>Tìm công thức theo Pair Value nguyên liệu.</summary>
        public RecipeItem FindByPairKey(string pairKey)
        {
            if (string.IsNullOrEmpty(pairKey)) return null;
            return _recipes.FirstOrDefault(r => r != null && string.Equals(r.PairValue, pairKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
