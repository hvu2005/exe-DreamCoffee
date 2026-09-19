using System.Globalization;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Định dạng chỉ số công thức cho các bảng thống kê trên UI. Gom về một chỗ để mọi nơi hiện
    /// cùng một kiểu số, và sau này đổi đơn vị chỉ phải sửa đúng file này.
    /// Chữ giữ ASCII ("d/s") vì font mặc định LiberationSans SDF không có glyph tiếng Việt.
    /// </summary>
    public static class RecipeStatFormat
    {
        /// <summary>Đơn vị hiển thị của tiền/giây.</summary>
        public const string MoneyPerSecondUnit = "d/s";

        // Cùng culture với TopBarCurrencyView để dấu phân cách nghìn nhất quán toàn game
        private static readonly CultureInfo Culture = new("vi-VN");

        /// <summary>Chỉ con số tiền/giây: "3", "2,5" — dùng khi nhãn đơn vị nằm sẵn trên UI.</summary>
        public static string MoneyPerSecondValue(RecipeItem recipe)
        {
            float mps = recipe != null ? recipe.MoneyPerSecondBonus : 0f;
            return mps % 1f == 0f
                ? ((long)mps).ToString("N0", Culture)
                : mps.ToString("#,##0.##", Culture);
        }

        /// <summary>Số kèm đơn vị: "3 d/s".</summary>
        public static string MoneyPerSecond(RecipeItem recipe) =>
            $"{MoneyPerSecondValue(recipe)} {MoneyPerSecondUnit}";
    }
}
