using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Một thẻ công thức trong lưới của <see cref="RecipePanelView"/>. Prefab dùng chung cho mọi
    /// công thức — nội dung gán runtime qua Bind(). Công thức chưa mở khoá hiện dạng "???" để không
    /// lộ đáp án của vòng lặp mò công thức bên panel pha chế.
    /// Chữ để tiếng Anh vì font mặc định (LiberationSans SDF) không có glyph tiếng Việt.
    /// </summary>
    public sealed class RecipeSlotView : MonoBehaviour
    {
        [SerializeField] private Image statusTag;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text ingredientLabel;
        [SerializeField] private TMP_Text priceLabel;

        private static readonly Color Unlocked = new(0.36f, 0.70f, 0.45f);
        private static readonly Color Craftable = new(0.42f, 0.55f, 0.25f);
        private static readonly Color Locked = new(0.55f, 0.51f, 0.47f);

        /// <summary>
        /// Hiển thị một công thức.
        /// </summary>
        /// <param name="recipe">Định nghĩa công thức.</param>
        /// <param name="unlocked">Đã mở khoá hay chưa — chưa thì che tên và nguyên liệu.</param>
        /// <param name="ingredients">Chuỗi tên nguyên liệu đã dựng sẵn (panel lo phần tra tên).</param>
        /// <param name="canCraft">Kho hiện có đủ nguyên liệu để pha ngay không.</param>
        public void Bind(RecipeItem recipe, bool unlocked, string ingredients, bool canCraft)
        {
            if (recipe == null) return;

            nameLabel.text = unlocked ? recipe.DisplayName : "???";
            ingredientLabel.text = unlocked
                ? ingredients
                : $"{recipe.RequiredIngredientIds.Count} unknown ingredients";

            // Công thức chưa mở khoá vẫn giữ khung ảnh nhưng bôi xám để nhìn ra là còn thiếu.
            icon.sprite = recipe.Icon;
            icon.color = recipe.Icon == null
                ? Color.clear
                : (unlocked ? Color.white : new Color(0.15f, 0.15f, 0.15f, 0.55f));

            priceLabel.text = unlocked ? RecipeStatFormat.MoneyPerSecond(recipe) : "-";

            if (!unlocked)
            {
                statusLabel.text = "LOCKED";
                statusTag.color = Locked;
            }
            else if (canCraft)
            {
                statusLabel.text = "READY";
                statusTag.color = Craftable;
            }
            else
            {
                statusLabel.text = "KNOWN";
                statusTag.color = Unlocked;
            }
        }
    }
}
