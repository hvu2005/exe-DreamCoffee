using System;
using System.Collections.Generic;
using System.Text;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Panel công thức: bấm R để bật/tắt. Mỗi lần mở sẽ hỏi DatabaseManager lấy
    /// <see cref="ScriptableRecipeRepository"/> rồi đổ toàn bộ công thức ra lưới; trạng thái mở khoá
    /// và tồn kho lấy từ các controller runtime của <see cref="GameSystemsProvider"/>.
    ///
    /// Script nằm trên node gốc (luôn active) còn <see cref="window"/> mới là phần bật/tắt — tắt node
    /// gốc thì Update() không chạy, không bấm R mở lại được (cùng cách làm với InventoryPanelView).
    /// </summary>
    public sealed class RecipePanelView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject window;
        [SerializeField] private Transform grid;
        [SerializeField] private RecipeSlotView slotPrefab;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("Bỏ trống thì dùng DatabaseManager.Instance.")]
        private DatabaseManager database;

        [Header("Hiển thị")]
        [SerializeField, Tooltip("Bật thì giấu luôn công thức chưa mở khoá; tắt thì hiện dạng '???'.")]
        private bool hideLockedRecipes;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.R;

        private readonly List<RecipeSlotView> _slots = new();
        private readonly StringBuilder _ingredientBuilder = new();

        private RecipeController _boundRecipes;

        public bool IsOpen => window != null && window.activeSelf;

        /// <summary>Lấy lúc dùng chứ không phải lúc Awake — thứ tự Awake giữa các object không đảm bảo.</summary>
        private DatabaseManager Database => database != null ? database : DatabaseManager.Instance;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (window != null) window.SetActive(false);
        }

        private void OnDestroy() => Unbind();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            window.SetActive(true);
            Bind();
            Refresh();
        }

        public void Close()
        {
            window.SetActive(false);
            Unbind();
        }

        /// <summary>Nghe RecipeController để panel đang mở tự cập nhật khi có công thức vừa mở khoá.</summary>
        private void Bind()
        {
            var recipes = GameSystemsProvider.Instance != null ? GameSystemsProvider.Instance.Recipe : null;
            if (recipes == _boundRecipes) return;

            Unbind();
            _boundRecipes = recipes;
            if (_boundRecipes != null) _boundRecipes.Changed += Refresh;
        }

        private void Unbind()
        {
            if (_boundRecipes != null) _boundRecipes.Changed -= Refresh;
            _boundRecipes = null;
        }

        public void Refresh()
        {
            var db = Database;
            var repository = db != null ? db.Get<ScriptableRecipeRepository>() : null;
            if (repository == null)
            {
                Debug.LogWarning("[RecipePanel] Chưa có ScriptableRecipeRepository trong DatabaseManager.");
            }

            var recipes = repository != null ? repository.GetAllRecipes() : Array.Empty<RecipeItem>();

            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();

            // Trạng thái mở khoá thật nằm ở controller runtime (đã mò ra trong lúc chơi), asset chỉ
            // giữ trạng thái khởi điểm IsDefaultUnlocked.
            var provider = GameSystemsProvider.Instance;
            var recipeController = provider != null ? provider.Recipe : null;
            var inventory = provider != null ? provider.Inventory : null;
            var itemRepository = db != null ? db.Get<ScriptableInventoryItemRepository>() : null;

            int unlockedCount = 0;

            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;

                bool unlocked = recipeController != null
                    ? recipeController.IsUnlocked(recipe.Id)
                    : recipe.IsDefaultUnlocked;

                if (unlocked) unlockedCount++;
                if (!unlocked && hideLockedRecipes) continue;

                bool canCraft = unlocked && recipeController != null && inventory != null
                                && recipeController.CanCraft(recipe.Id, inventory);

                var slot = Instantiate(slotPrefab, grid);
                slot.Bind(recipe, unlocked, DescribeIngredients(recipe, itemRepository), canCraft);
                _slots.Add(slot);
            }

            int total = 0;
            foreach (var recipe in recipes)
                if (recipe != null) total++;

            if (summaryLabel != null) summaryLabel.text = $"{unlockedCount} / {total} discovered";
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(_slots.Count == 0);
        }

        /// <summary>Đổi danh sách ID nguyên liệu thành chuỗi tên hiển thị: "Coffee Bean + Milk".</summary>
        private string DescribeIngredients(RecipeItem recipe, ScriptableInventoryItemRepository itemRepository)
        {
            var ids = recipe.RequiredIngredientIds;
            if (ids == null || ids.Count == 0) return "No ingredients";

            _ingredientBuilder.Clear();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.IsNullOrEmpty(ids[i])) continue;
                if (_ingredientBuilder.Length > 0) _ingredientBuilder.Append(" + ");

                // Không tra được tên thì in thẳng ID — vẫn hơn là hiện ô trống khi asset thiếu.
                var item = itemRepository != null ? itemRepository.GetItem(ids[i]) : null;
                _ingredientBuilder.Append(item != null ? item.DisplayName : ids[i]);
            }

            return _ingredientBuilder.Length > 0 ? _ingredientBuilder.ToString() : "No ingredients";
        }
    }
}
