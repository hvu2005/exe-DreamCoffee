using DreamCafe.DataControl;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Xoá tiến độ mở khoá công thức đã lưu.
    ///
    /// Từ lúc có <see cref="RecipeUnlockStore"/>, mò ra một công thức là nó nhớ luôn — nghĩa là bảng
    /// "NEW RECIPE DISCOVERED!" chỉ hiện đúng một lần trong cả đời máy. Muốn xem lại luồng đó khi
    /// test thì xoá tiến độ ở đây rồi bấm Play lại.
    /// </summary>
    public static class RecipeProgressMenu
    {
        [MenuItem("DreamCafe/Debug/Xoá tiến độ công thức đã mở khoá")]
        public static void ClearUnlockedRecipes()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Xoá tiến độ công thức",
                "Mọi công thức từng mò ra sẽ khoá lại như lúc chơi lần đầu (món IsDefaultUnlocked vẫn mở). " +
                "Không hoàn tác được.",
                "Xoá", "Huỷ");

            if (!confirmed) return;

            RecipeUnlockStore.Clear();
            Debug.Log("[RecipeProgress] Đã xoá tiến độ mở khoá công thức — bấm Play để bắt đầu lại từ đầu.");
        }
    }
}
