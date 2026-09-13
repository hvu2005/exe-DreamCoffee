using System;
using System.Collections.Generic;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Controller quản lý dữ liệu Công Thức (Recipe) tại Runtime theo mô hình CRUD.
    /// Hỗ trợ tra cứu nhanh bằng Pair Value (chuỗi ghép ID nguyên liệu).
    ///
    ///   Create -> <see cref="AddRecipe"/>, <see cref="AddRecipes"/>
    ///   Read   -> <see cref="GetRecipe"/>, <see cref="GetAll"/>, <see cref="GetUnlocked"/>, <see cref="GetLocked"/>,
    ///             <see cref="FindByPairKey"/>, <see cref="MatchRecipe"/>, <see cref="CanCraft"/>
    ///   Update -> <see cref="UnlockRecipe"/>, <see cref="LockRecipe"/>, <see cref="TryDiscover"/>
    ///   Delete -> <see cref="RemoveRecipe"/>, <see cref="ResetAll"/>
    ///
    /// Kế thừa IService để tham gia ServiceManager và tích hợp cùng InventoryController.
    /// </summary>
    public sealed class RecipeController : IService
    {
        // Key = RecipeItem.Id
        private readonly Dictionary<string, RecipeItem> _recipes = new();

        // Key = RecipeItem.PairValue, Value = RecipeItem.Id
        private readonly Dictionary<string, string> _pairKeyIndex = new(StringComparer.OrdinalIgnoreCase);

        // Tập hợp ID các công thức đã được mở khóa
        private readonly HashSet<string> _unlockedIds = new();

        /// <summary>Bắn sự kiện khi danh sách hoặc trạng thái mở khóa của công thức thay đổi.</summary>
        public event Action Changed;

        /// <summary>Bắn sự kiện khi một công thức mới được mở khóa (vd: sau khi mò thành công).</summary>
        public event Action<RecipeItem> RecipeUnlocked;

        /// <summary>Tổng số lượng công thức trong hệ thống.</summary>
        public int TotalCount => _recipes.Count;

        /// <summary>Số lượng công thức đã mở khóa.</summary>
        public int UnlockedCount => _unlockedIds.Count;

        // =====================================================================
        // IService Lifecycle
        // =====================================================================

        /// <summary>Khởi tạo controller với ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[RecipeController] Initialized — {_recipes.Count} recipes ({_unlockedIds.Count} unlocked).");
        }

        /// <summary>Dọn dẹp tài nguyên khi hệ thống tắt.</summary>
        public void Shutdown()
        {
            Changed = null;
            RecipeUnlocked = null;
            _recipes.Clear();
            _pairKeyIndex.Clear();
            _unlockedIds.Clear();
        }

        // =====================================================================
        // CREATE / ADD
        // =====================================================================

        /// <summary>
        /// Đăng ký thêm một công thức vào controller.
        /// </summary>
        /// <param name="recipe">Định nghĩa công thức.</param>
        /// <param name="forceUnlocked">Ghi đè trạng thái mở khóa nếu chỉ định (mặc định lấy theo IsDefaultUnlocked).</param>
        public bool AddRecipe(RecipeItem recipe, bool? forceUnlocked = null)
        {
            if (recipe == null)
            {
                Debug.LogWarning("[RecipeController] Không thể thêm công thức null.");
                return false;
            }

            string id = recipe.Id;
            _recipes[id] = recipe;

            // Đăng ký vào bảng tra cứu Pair Key
            string pairKey = recipe.PairValue;
            if (!string.IsNullOrEmpty(pairKey))
            {
                _pairKeyIndex[pairKey] = id;
            }

            bool isUnlocked = forceUnlocked ?? recipe.IsDefaultUnlocked;
            if (isUnlocked)
            {
                _unlockedIds.Add(id);
            }
            else
            {
                _unlockedIds.Remove(id);
            }

            RaiseChanged();
            return true;
        }

        /// <summary>Đăng ký hàng loạt công thức (vd từ ScriptableRecipeRepository).</summary>
        public void AddRecipes(IEnumerable<RecipeItem> recipes)
        {
            if (recipes == null) return;
            foreach (var r in recipes)
            {
                if (r != null)
                {
                    _recipes[r.Id] = r;
                    string pairKey = r.PairValue;
                    if (!string.IsNullOrEmpty(pairKey))
                    {
                        _pairKeyIndex[pairKey] = r.Id;
                    }
                    if (r.IsDefaultUnlocked)
                    {
                        _unlockedIds.Add(r.Id);
                    }
                }
            }
            RaiseChanged();
        }

        // =====================================================================
        // READ
        // =====================================================================

        /// <summary>Lấy công thức theo ID.</summary>
        public RecipeItem GetRecipe(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _recipes.TryGetValue(id, out var recipe) ? recipe : null;
        }

        /// <summary>Lấy toàn bộ công thức đã đăng ký.</summary>
        public IReadOnlyCollection<RecipeItem> GetAll() => _recipes.Values;

        /// <summary>Lấy danh sách các công thức ĐÃ MỞ KHÓA.</summary>
        public IReadOnlyList<RecipeItem> GetUnlocked()
        {
            var list = new List<RecipeItem>(_unlockedIds.Count);
            foreach (var id in _unlockedIds)
            {
                if (_recipes.TryGetValue(id, out var recipe))
                {
                    list.Add(recipe);
                }
            }
            return list;
        }

        /// <summary>Lấy danh sách các công thức CHƯA MỞ KHÓA.</summary>
        public IReadOnlyList<RecipeItem> GetLocked()
        {
            var list = new List<RecipeItem>();
            foreach (var kvp in _recipes)
            {
                if (!_unlockedIds.Contains(kvp.Key))
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        /// <summary>Kiểm tra xem công thức có ID tương ứng đã mở khóa hay chưa.</summary>
        public bool IsUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id) && _unlockedIds.Contains(id);
        }

        /// <summary>
        /// Tìm kiếm công thức dựa trên Pair Value nguyên liệu trực tiếp (vd: "coffee_bean+milk").
        /// Tra cứu O(1) qua chỉ mục hash table.
        /// </summary>
        public RecipeItem FindByPairKey(string pairKey)
        {
            if (string.IsNullOrEmpty(pairKey)) return null;
            return _pairKeyIndex.TryGetValue(pairKey, out var id) ? GetRecipe(id) : null;
        }

        /// <summary>
        /// So khớp công thức từ danh sách các nguyên liệu đang đưa vào (tự động chuẩn hóa thành Pair Value).
        /// </summary>
        public RecipeItem MatchRecipe(IEnumerable<string> ingredientIds)
        {
            string pairKey = RecipeKeyUtility.ComputePairKey(ingredientIds);
            return FindByPairKey(pairKey);
        }

        /// <summary>
        /// Kiểm tra xem kho đồ (InventoryController) có đủ tất cả nguyên liệu để làm công thức này không.
        /// </summary>
        public bool CanCraft(string recipeId, InventoryController inventory)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null || inventory == null) return false;

            // Gom số lượng cần của từng loại nguyên liệu
            var neededCounts = new Dictionary<string, int>();
            foreach (var ingId in recipe.RequiredIngredientIds)
            {
                if (string.IsNullOrEmpty(ingId)) continue;
                neededCounts[ingId] = neededCounts.TryGetValue(ingId, out int c) ? c + 1 : 1;
            }

            foreach (var kvp in neededCounts)
            {
                if (inventory.GetQuantity(kvp.Key) < kvp.Value)
                {
                    return false;
                }
            }

            return true;
        }

        // =====================================================================
        // UPDATE
        // =====================================================================

        /// <summary>Mở khóa một công thức theo ID.</summary>
        public bool UnlockRecipe(string id)
        {
            if (string.IsNullOrEmpty(id) || !_recipes.ContainsKey(id)) return false;

            if (_unlockedIds.Add(id))
            {
                var recipe = _recipes[id];
                Debug.Log($"[RecipeController] Đã mở khóa công thức: {recipe.DisplayName} ({id})");
                RecipeUnlocked?.Invoke(recipe);
                RaiseChanged();
                return true;
            }

            return false;
        }

        /// <summary>Khóa lại một công thức.</summary>
        public bool LockRecipe(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (_unlockedIds.Remove(id))
            {
                RaiseChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Thử nghiệm mò công thức (Hệ thống Discovery):
        /// Đưa tổ hợp nguyên liệu vào, nếu khớp với một công thức trong game:
        /// - Nếu chưa mở khóa: mở khóa ngay và trả về true kèm công thức vừa khám phá!
        /// - Nếu đã mở khóa: trả về false nhưng vẫn trả về công thức tương ứng.
        /// - Nếu không khớp công thức nào: trả về false và out null.
        /// </summary>
        public bool TryDiscover(IEnumerable<string> ingredientIds, out RecipeItem discoveredRecipe)
        {
            discoveredRecipe = MatchRecipe(ingredientIds);
            if (discoveredRecipe == null)
            {
                return false;
            }

            if (!IsUnlocked(discoveredRecipe.Id))
            {
                UnlockRecipe(discoveredRecipe.Id);
                return true;
            }

            return false;
        }

        // =====================================================================
        // DELETE / RESET
        // =====================================================================

        /// <summary>Xóa một công thức khỏi hệ thống runtime.</summary>
        public bool RemoveRecipe(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (_recipes.TryGetValue(id, out var recipe))
            {
                _pairKeyIndex.Remove(recipe.PairValue);
            }

            bool removed = _recipes.Remove(id);
            _unlockedIds.Remove(id);

            if (removed)
            {
                RaiseChanged();
            }

            return removed;
        }

        /// <summary>Khôi phục lại toàn bộ trạng thái mở khóa ban đầu theo definition.</summary>
        public void ResetAll()
        {
            _unlockedIds.Clear();
            foreach (var kvp in _recipes)
            {
                if (kvp.Value.IsDefaultUnlocked)
                {
                    _unlockedIds.Add(kvp.Key);
                }
            }
            RaiseChanged();
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
