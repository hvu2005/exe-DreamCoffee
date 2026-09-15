using System;
using System.Collections.Generic;
using DreamCafe.Core.Services;
using DreamCafe.DataControl;
using UnityEngine;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Kết quả của một lần bấm BREW.
    /// </summary>
    public enum BrewOutcome
    {
        /// <summary>Ra đúng một công thức đã biết từ trước.</summary>
        Success = 0,
        /// <summary>Mò trúng công thức chưa mở khoá — vừa được mở khoá ngay lúc này.</summary>
        Discovered = 1,
        /// <summary>Tổ hợp nguyên liệu không khớp công thức nào trong database.</summary>
        NoMatch = 2,
        /// <summary>Chưa bỏ nguyên liệu nào vào cốc.</summary>
        EmptyMixture = 3,
        // 4 bỏ trống: từng là NotEnoughWater, nay nguyên liệu tự mang nước theo nên không còn nữa.
        /// <summary>Kho không còn đủ nguyên liệu (bị trừ mất ở hệ thống khác trong lúc đang chọn).</summary>
        MissingIngredient = 5
    }

    /// <summary>Chi tiết một lần pha — UI đọc cái này để hiện thông báo.</summary>
    public readonly struct BrewResult
    {
        public readonly BrewOutcome Outcome;
        public readonly RecipeItem Recipe;
        public readonly string PairKey;
        public readonly bool ConsumedIngredients;

        public BrewResult(BrewOutcome outcome, RecipeItem recipe, string pairKey, bool consumed)
        {
            Outcome = outcome;
            Recipe = recipe;
            PairKey = pairKey;
            ConsumedIngredients = consumed;
        }

        /// <summary>Có tạo ra được đồ uống hay không (Success hoặc Discovered).</summary>
        public bool IsDrinkMade => Outcome == BrewOutcome.Success || Outcome == BrewOutcome.Discovered;

        /// <summary>
        /// Máy pha đã thật sự chạy một mẻ (ra món hoặc hỏng món), khác với lỗi thao tác kiểu chưa
        /// bỏ nguyên liệu nào vào cốc. Chỉ những lần này mới đáng cho màn chờ + popup.
        /// </summary>
        public bool IsBrewCompleted => IsDrinkMade || Outcome == BrewOutcome.NoMatch;
    }

    /// <summary>
    /// Bộ máy pha chế: giữ "cốc" đang trộn dở (danh sách nguyên liệu), kiểm tra kho, và khi bấm
    /// BREW thì tra <see cref="RecipeController"/> bằng Pair Value để biết ra món gì.
    ///
    ///   Thêm/bớt nguyên liệu -> <see cref="TryAddIngredient"/>, <see cref="RemoveAt"/>, <see cref="ClearMixture"/>
    ///   Pha                  -> <see cref="Brew"/>
    ///
    /// Không còn bước đổ nước riêng: mỗi nguyên liệu đã tự mang theo phần nước của nó, nên cốc đầy
    /// dần đúng theo số nguyên liệu (xem <see cref="FillRatio"/>).
    ///
    /// Là POCO (không phải MonoBehaviour) giống InventoryController/RecipeController để test được
    /// tách rời UI — BrewingPanelView chỉ là lớp hiển thị mỏng bọc ngoài nó.
    ///
    /// Mò trúng công thức chưa mở khoá sẽ tự unlock qua <see cref="RecipeController.TryDiscover"/>,
    /// và RecipeController lại bắn tiếp sự kiện cho GameSystemsProvider mở khoá khách hàng liên quan.
    /// </summary>
    public sealed class BrewingController : IService
    {
        /// <summary>Số nguyên liệu tối đa bỏ vào một cốc — khớp với 3 ô hiển thị trên panel.</summary>
        public const int MaxIngredients = 3;

        private readonly List<InventoryItem> _mixture = new(MaxIngredients);

        private InventoryController _inventory;
        private RecipeController _recipes;

        /// <summary>Có trừ nguyên liệu khi pha hỏng không. Bật = mò sai vẫn mất đồ (đúng ý đồ "đánh cược").</summary>
        public bool ConsumeOnFailure { get; set; } = true;

        /// <summary>Bắn sau mọi thay đổi của cốc (thêm/bớt nguyên liệu) — UI refresh theo.</summary>
        public event Action Changed;

        /// <summary>Bắn sau mỗi lần bấm BREW, kèm kết quả.</summary>
        public event Action<BrewResult> Brewed;

        /// <summary>Các nguyên liệu đang có trong cốc (theo thứ tự bỏ vào).</summary>
        public IReadOnlyList<InventoryItem> Mixture => _mixture;

        /// <summary>
        /// Cốc đã đầy tới đâu, 0..1. Mỗi nguyên liệu đổ thêm đúng một phần
        /// (1/<see cref="MaxIngredients"/>) — bỏ đủ 3 thứ là đầy cốc.
        /// </summary>
        public float FillRatio => Mathf.Clamp01((float)_mixture.Count / MaxIngredients);

        public bool IsFull => _mixture.Count >= MaxIngredients;
        public bool IsEmpty => _mixture.Count == 0;

        /// <summary>Pair Value của cốc hiện tại (vd: "coffee_bean+milk") — chuẩn hoá, không phụ thuộc thứ tự bỏ vào.</summary>
        public string CurrentPairKey => RecipeKeyUtility.ComputePairKey(GetMixtureIds());

        // =====================================================================
        // IService
        // =====================================================================

        public void Init(ServiceContext ctx) => Debug.Log("[Brewing] Init — sẵn sàng pha chế.");

        public void Shutdown()
        {
            Changed = null;
            Brewed = null;
            _mixture.Clear();
            _inventory = null;
            _recipes = null;
        }

        /// <summary>
        /// Nối bộ pha chế vào kho và sổ công thức. Gọi lại được nhiều lần (vd: khi provider sẵn sàng muộn).
        /// </summary>
        public void Bind(InventoryController inventory, RecipeController recipes)
        {
            _inventory = inventory;
            _recipes = recipes;
        }

        /// <summary>Đã nối được vào kho + sổ công thức hay chưa.</summary>
        public bool IsBound => _inventory != null && _recipes != null;

        // =====================================================================
        // Nguyên liệu
        // =====================================================================

        /// <summary>
        /// Bỏ thêm một nguyên liệu vào cốc. Chưa trừ kho ở bước này — chỉ trừ khi bấm BREW,
        /// nên người chơi có thể bỏ ra bỏ vào thoải mái mà không mất đồ.
        /// </summary>
        /// <param name="definition">Định nghĩa nguyên liệu (asset gốc trong database).</param>
        /// <param name="reason">Lý do từ chối, để UI hiện lên cho người chơi.</param>
        public bool TryAddIngredient(InventoryItem definition, out string reason)
        {
            if (definition == null)
            {
                reason = "Invalid ingredient.";
                return false;
            }

            if (IsFull)
            {
                reason = $"The cup only fits {MaxIngredients} ingredients.";
                return false;
            }

            if (GetAvailable(definition) <= 0)
            {
                reason = $"Not enough {definition.DisplayName} in stock.";
                return false;
            }

            _mixture.Add(definition);
            reason = null;
            RaiseChanged();
            return true;
        }

        /// <summary>Nhấc một nguyên liệu ra khỏi cốc theo vị trí ô.</summary>
        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _mixture.Count) return false;
            _mixture.RemoveAt(index);
            RaiseChanged();
            return true;
        }

        /// <summary>Đổ cốc đi: bỏ hết nguyên liệu.</summary>
        public void ClearMixture()
        {
            if (_mixture.Count == 0) return;
            _mixture.Clear();
            RaiseChanged();
        }

        /// <summary>
        /// Số lượng còn có thể bỏ thêm vào cốc = tồn kho trừ đi số đã nằm sẵn trong cốc.
        /// </summary>
        public int GetAvailable(InventoryItem definition)
        {
            if (definition == null || _inventory == null) return 0;
            return Mathf.Max(0, _inventory.GetQuantity(definition.Id) - CountInMixture(definition.Id));
        }

        /// <summary>Đếm số lần một loại nguyên liệu đang nằm trong cốc.</summary>
        public int CountInMixture(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;

            int count = 0;
            foreach (var item in _mixture)
            {
                if (item != null && string.Equals(item.Id, itemId, StringComparison.OrdinalIgnoreCase)) count++;
            }
            return count;
        }

        // =====================================================================
        // Pha chế
        // =====================================================================

        /// <summary>
        /// Pha cốc hiện tại: tra công thức theo Pair Value, trừ nguyên liệu trong kho, mở khoá
        /// công thức nếu đây là lần đầu mò trúng. Cốc được đổ sạch sau mỗi lần pha thành công
        /// (hoặc pha hỏng có trừ đồ).
        /// </summary>
        public BrewResult Brew()
        {
            if (IsEmpty)
                return Finish(new BrewResult(BrewOutcome.EmptyMixture, null, string.Empty, false), clear: false);

            string pairKey = CurrentPairKey;
            var costs = BuildCosts();

            // Kho có thể đã bị hệ thống khác trừ mất trong lúc người chơi còn đang chọn.
            foreach (var (item, amount) in costs)
            {
                if (_inventory == null || !_inventory.Has(item.Id, amount))
                    return Finish(new BrewResult(BrewOutcome.MissingIngredient, null, pairKey, false), clear: false);
            }

            var recipe = _recipes != null ? _recipes.FindByPairKey(pairKey) : null;

            if (recipe == null)
            {
                bool wasted = ConsumeOnFailure && _inventory.TryConsume(costs);
                return Finish(new BrewResult(BrewOutcome.NoMatch, null, pairKey, wasted), clear: true);
            }

            // Chụp lại danh sách ID trước khi trừ kho — Finish() sẽ đổ cốc nên phải lấy trước.
            var mixtureIds = GetMixtureIds();

            if (!_inventory.TryConsume(costs))
                return Finish(new BrewResult(BrewOutcome.MissingIngredient, recipe, pairKey, false), clear: false);

            // TryDiscover trả true đúng một lần duy nhất — lần đầu tiên mò ra công thức này.
            bool discovered = _recipes.TryDiscover(mixtureIds, out _);

            // Thành phẩm (nếu công thức khai báo OutputItemId) được nhập ngược lại vào kho.
            TryStoreOutput(recipe);

            var outcome = discovered ? BrewOutcome.Discovered : BrewOutcome.Success;
            return Finish(new BrewResult(outcome, recipe, pairKey, true), clear: true);
        }

        /// <summary>Xem trước cốc hiện tại sẽ ra món gì mà không tốn nguyên liệu (debug/gợi ý).</summary>
        public RecipeItem Peek() => _recipes != null ? _recipes.FindByPairKey(CurrentPairKey) : null;

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private List<string> GetMixtureIds()
        {
            var ids = new List<string>(_mixture.Count);
            foreach (var item in _mixture)
            {
                if (item != null) ids.Add(item.Id);
            }
            return ids;
        }

        /// <summary>Gộp nguyên liệu trùng loại thành một dòng (item, số lượng) để trừ kho nguyên tử.</summary>
        private List<(InventoryItem item, int amount)> BuildCosts()
        {
            var costs = new List<(InventoryItem item, int amount)>();
            foreach (var item in _mixture)
            {
                if (item == null) continue;

                int index = costs.FindIndex(c => string.Equals(c.item.Id, item.Id, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) costs[index] = (costs[index].item, costs[index].amount + 1);
                else costs.Add((item, 1));
            }
            return costs;
        }

        private void TryStoreOutput(RecipeItem recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.OutputItemId) || _inventory == null) return;

            // Chỉ nhập được thành phẩm nếu loại đó đã có mặt trong kho runtime (có definition để cộng vào).
            if (_inventory.TryGet(recipe.OutputItemId, out var runtime) && runtime != null)
            {
                _inventory.Add(runtime, 1);
            }
        }

        private BrewResult Finish(BrewResult result, bool clear)
        {
            if (clear) _mixture.Clear();

            Brewed?.Invoke(result);
            RaiseChanged();
            return result;
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
