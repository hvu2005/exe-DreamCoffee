using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Nhớ những công thức người chơi đã mò ra, giữ qua các lần chạy game.
    ///
    /// Thiếu nó thì mỗi lần bấm Play là mọi công thức có <c>IsDefaultUnlocked = false</c> lại về
    /// trạng thái chưa biết, và bảng "NEW RECIPE DISCOVERED!" hiện lại y như lần đầu dù người chơi
    /// đã mở khoá món đó từ lâu.
    ///
    /// Lưu bằng PlayerPrefs cho gọn: một chuỗi ID ngăn nhau bằng dấu ';'. Tiến độ mới có bấy nhiêu
    /// nên chưa cần tới file save; sau này đổi sang JSON thì cũng chỉ phải sửa trong lớp này.
    /// </summary>
    public static class RecipeUnlockStore
    {
        /// <summary>Khoá PlayerPrefs — đổi tên khoá này là người chơi cũ mất sạch tiến độ.</summary>
        public const string PrefsKey = "dreamcafe.recipes.unlocked";

        private const char Separator = ';';

        /// <summary>Ghi lại toàn bộ công thức đang mở khoá.</summary>
        public static void Save(RecipeController recipes)
        {
            if (recipes == null) return;

            var unlocked = recipes.GetUnlocked();
            var ids = new List<string>(unlocked.Count);
            foreach (var recipe in unlocked)
            {
                if (recipe != null && !string.IsNullOrEmpty(recipe.Id)) ids.Add(recipe.Id);
            }

            PlayerPrefs.SetString(PrefsKey, string.Join(Separator.ToString(), ids));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Mở khoá lại những công thức đã lưu. Phải gọi SAU khi nạp định nghĩa từ database, không
        /// thì chưa có công thức nào trong controller để mà mở.
        /// </summary>
        /// <returns>Số công thức thật sự được khôi phục (không tính món vốn đã mở sẵn).</returns>
        public static int Load(RecipeController recipes)
        {
            if (recipes == null) return 0;

            string raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return 0;

            int restored = 0;
            foreach (string id in raw.Split(Separator))
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                // Công thức bị xoá khỏi database thì UnlockRecipe trả false — bỏ qua, đừng để save
                // cũ làm hỏng lượt chơi mới.
                if (recipes.UnlockRecipe(id.Trim())) restored++;
            }

            return restored;
        }

        /// <summary>Xoá sạch tiến độ — dùng khi cần test lại luồng khám phá công thức.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}
