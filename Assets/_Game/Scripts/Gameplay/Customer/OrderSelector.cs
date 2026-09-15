using System.Collections.Generic;
using DreamCafe.DataControl;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Chọn công thức khách hàng sẽ gọi. Tách riêng khỏi state machine để sau này có thể thay bằng
    /// luật phức tạp hơn (budget, gọi nhiều món, ưu tiên theo nhóm khách) mà không đụng tới các state.
    /// </summary>
    public static class OrderSelector
    {
        /// <summary>
        /// Ưu tiên chọn một món yêu thích đã mở khóa của khách; nếu không có, chọn ngẫu nhiên
        /// một món bất kỳ đã mở khóa. Trả về null nếu hệ thống chưa mở khóa món nào.
        /// </summary>
        public static RecipeItem PickOrder(CustomerItem definition, RecipeController recipeController)
        {
            if (definition == null || recipeController == null) return null;

            var unlocked = recipeController.GetUnlocked();
            if (unlocked.Count == 0) return null;

            foreach (var favoriteId in definition.FavoriteRecipeIds)
            {
                var favorite = recipeController.GetRecipe(favoriteId);
                if (favorite != null && recipeController.IsUnlocked(favoriteId))
                {
                    return favorite;
                }
            }

            return unlocked[Random.Range(0, unlocked.Count)];
        }
    }
}
