using DreamCafe.Core.Services;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Giao diện kho lưu trữ danh mục công thức định nghĩa tĩnh.
    /// Kế thừa IService để tham gia vòng đời ServiceManager.
    /// </summary>
    public interface IRecipeRepository : IService
    {
        /// <summary>Lấy công thức theo ID.</summary>
        RecipeItem GetRecipe(string id);

        /// <summary>Lấy toàn bộ công thức trong game.</summary>
        RecipeItem[] GetAllRecipes();

        /// <summary>Tìm công thức dựa theo Pair Value của nguyên liệu (vd: "coffee_bean+milk").</summary>
        RecipeItem FindByPairKey(string pairKey);
    }
}
