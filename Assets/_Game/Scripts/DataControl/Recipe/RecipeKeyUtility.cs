using System;
using System.Collections.Generic;
using System.Linq;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Tiện ích tạo và chuẩn hóa Pair Value (Composite Key) cho các công thức pha chế.
    /// Giúp so khớp nguyên liệu bất kể thứ tự bỏ vào (ví dụ: 'milk' + 'coffee_bean' == 'coffee_bean' + 'milk').
    /// </summary>
    public static class RecipeKeyUtility
    {
        public const char Separator = '+';

        /// <summary>
        /// Tạo ra Pair Value chuẩn hóa từ tập hợp các ID nguyên liệu.
        /// Danh sách ID sẽ được loại bỏ khoảng trắng, sắp xếp theo thứ tự bảng chữ cái và ghép nối bằng dấu '+'.
        /// Ví dụ: ["milk", "coffee_bean"] => "coffee_bean+milk".
        /// </summary>
        public static string ComputePairKey(IEnumerable<string> ingredientIds)
        {
            if (ingredientIds == null) return string.Empty;

            var sorted = ingredientIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim().ToLowerInvariant())
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            return string.Join(Separator.ToString(), sorted);
        }

        /// <summary>
        /// Tách một Pair Value thành mảng các ID nguyên liệu cấu thành.
        /// </summary>
        public static string[] SplitPairKey(string pairKey)
        {
            if (string.IsNullOrWhiteSpace(pairKey)) return Array.Empty<string>();
            return pairKey.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
