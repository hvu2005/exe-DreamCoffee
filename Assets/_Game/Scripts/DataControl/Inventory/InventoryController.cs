using System;
using System.Collections.Generic;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Kho nguyên liệu — API kiểu CRUD nhưng dành riêng cho số lượng nguyên liệu (SO).
    ///
    ///   Create/Add -> <see cref="Add"/>
    ///   Read       -> <see cref="GetQuantity(InventoryItem)"/>, <see cref="Has"/>, <see cref="Get"/>, <see cref="Items"/>
    ///   Update     -> <see cref="Adjust"/> (cộng/trừ theo delta), <see cref="SetQuantity"/> (gán tuyệt đối)
    ///   Delete     -> <see cref="Remove"/>, <see cref="Clear(InventoryItem)"/>, <see cref="ClearAll"/>
    ///
    /// Mỗi <see cref="InventoryItem"/> asset truyền vào chỉ đóng vai trò "định nghĩa" (template).
    /// Controller tự tạo và giữ một BẢN SAO RUNTIME cho mỗi loại (qua CreateRuntimeInstance) —
    /// tuyệt đối không chỉnh số lượng trên asset gốc, nên nhiều InventoryController (nhiều màn
    /// chơi, nhiều save-slot...) có thể dùng chung 1 bộ asset định nghĩa mà không đụng nhau.
    ///
    /// Đăng ký làm IService trong CompositionRoot để mọi hệ thống khác (pha chế, chợ, nghiên
    /// cứu công thức, nhân viên...) resolve dùng chung qua ServiceContext.Services.
    /// TODO: Thêm giới hạn sức chứa tổng của kho (không chỉ giới hạn từng loại) khi làm nâng
    /// cấp kệ/tủ lạnh. TODO: Thêm TryConsume theo danh sách (công thức) khi làm hệ thống pha chế.
    /// </summary>
    public sealed class InventoryController : IService
    {
        // Key = InventoryItem.Id, Value = bản sao runtime (không phải asset gốc).
        private readonly Dictionary<string, InventoryItem> _items = new();

        /// <summary>Bắn sau mọi thay đổi số lượng — UI subscribe cái này để refresh.</summary>
        public event Action Changed;

        /// <summary>
        /// Toàn bộ nguyên liệu đang có trong kho (đã lọc bỏ loại về 0). Mỗi item tự mang theo
        /// Icon/DisplayName/Quantity nên UI có thể bind trực tiếp mà không cần thêm view-model.
        /// Đây là view sống của dictionary nội bộ — chỉ đọc, đừng Add/Remove trong lúc foreach.
        /// </summary>
        public IReadOnlyCollection<InventoryItem> Items => _items.Values;

        public int DistinctCount => _items.Count;

        // =====================================================================
        // IService
        // =====================================================================

        public void Init(ServiceContext ctx) =>
            Debug.Log($"[Inventory] Init — {DistinctCount} loại nguyên liệu.");

        public void Shutdown()
        {
            Changed = null;
            _items.Clear();
        }

        // =====================================================================
        // CREATE / ADD
        // =====================================================================

        /// <summary>
        /// Nhập nguyên liệu vào kho. Bị kẹp bớt nếu chạm <see cref="InventoryItem.MaxQuantity"/>.
        /// </summary>
        /// <returns>Số lượng THỰC SỰ nhập được (0..amount).</returns>
        public int Add(InventoryItem itemDefinition, int amount)
        {
            if (!Validate(itemDefinition, nameof(Add)) || amount <= 0) return 0;

            var runtime = GetOrCreateRuntime(itemDefinition);
            int accepted = Mathf.Min(amount, runtime.RemainingCapacity);
            if (accepted <= 0)
            {
                Debug.LogWarning($"[Inventory] '{runtime.DisplayName}' đã đầy ({runtime.MaxQuantity}), không nhập thêm được.");
                return 0;
            }

            runtime.SetQuantityInternal(runtime.Quantity + accepted);
            if (accepted < amount)
                Debug.LogWarning($"[Inventory] '{runtime.DisplayName}' chỉ nhận thêm {accepted}/{amount} " +
                                  $"do chạm giới hạn {runtime.MaxQuantity}.");

            RaiseChanged();
            return accepted;
        }

        /// <summary>Nhập nhiều loại cùng lúc (vd: 1 đơn hàng chợ). Trả về số nhập được của từng dòng.</summary>
        public IReadOnlyList<int> AddRange(IReadOnlyList<(InventoryItem item, int amount)> lines)
        {
            var results = new List<int>(lines?.Count ?? 0);
            if (lines == null) return results;
            foreach (var (item, amount) in lines)
                results.Add(Add(item, amount));
            return results;
        }

        // =====================================================================
        // READ
        // =====================================================================

        public int GetQuantity(InventoryItem itemDefinition) =>
            itemDefinition != null && _items.TryGetValue(itemDefinition.Id, out var runtime) ? runtime.Quantity : 0;

        public int GetQuantity(string id) =>
            !string.IsNullOrEmpty(id) && _items.TryGetValue(id, out var runtime) ? runtime.Quantity : 0;

        public bool Has(InventoryItem itemDefinition, int amount = 1) =>
            itemDefinition != null && (amount <= 0 || GetQuantity(itemDefinition) >= amount);

        public bool Has(string id, int amount = 1) =>
            !string.IsNullOrEmpty(id) && (amount <= 0 || GetQuantity(id) >= amount);

        /// <summary>Lấy bản runtime (mang Quantity thật) đang được kho theo dõi, null nếu chưa có trong kho.</summary>
        public InventoryItem Get(InventoryItem itemDefinition) =>
            itemDefinition != null && _items.TryGetValue(itemDefinition.Id, out var runtime) ? runtime : null;

        public bool TryGet(string id, out InventoryItem runtimeItem) => _items.TryGetValue(id, out runtimeItem);

        // =====================================================================
        // UPDATE (cộng / trừ)
        // =====================================================================

        /// <summary>
        /// Cộng (delta &gt; 0) hoặc trừ (delta &lt; 0) so với số lượng hiện tại.
        /// </summary>
        /// <returns>Số lượng thay đổi THỰC SỰ áp dụng được — dương nếu cộng, âm nếu trừ.</returns>
        public int Adjust(InventoryItem itemDefinition, int delta)
        {
            if (delta == 0) return 0;
            return delta > 0 ? Add(itemDefinition, delta) : -Remove(itemDefinition, -delta);
        }

        /// <summary>Gán số lượng tuyệt đối (kẹp trong [0, MaxQuantity]). Dùng cho load save, cheat, editor tool.</summary>
        public void SetQuantity(InventoryItem itemDefinition, int amount)
        {
            if (!Validate(itemDefinition, nameof(SetQuantity))) return;

            int clamped = Mathf.Clamp(amount, 0, itemDefinition.MaxQuantity);
            int delta = clamped - GetQuantity(itemDefinition);
            if (delta > 0) Add(itemDefinition, delta);
            else if (delta < 0) Remove(itemDefinition, -delta);
        }

        // =====================================================================
        // DELETE / REMOVE
        // =====================================================================

        /// <summary>Rút bớt nguyên liệu.</summary>
        /// <returns>Số lượng THỰC SỰ rút được (0..amount, không bao giờ âm quá số đang có).</returns>
        public int Remove(InventoryItem itemDefinition, int amount)
        {
            if (!Validate(itemDefinition, nameof(Remove)) || amount <= 0) return 0;
            if (!_items.TryGetValue(itemDefinition.Id, out var runtime) || runtime.IsEmpty) return 0;

            int removed = Mathf.Min(amount, runtime.Quantity);
            runtime.SetQuantityInternal(runtime.Quantity - removed);

            // Về 0 thì bỏ luôn khỏi kho — UI liệt kê Items sẽ không còn thấy loại này nữa.
            if (runtime.IsEmpty) _items.Remove(itemDefinition.Id);

            RaiseChanged();
            return removed;
        }

        /// <summary>
        /// Trừ trọn gói nhiều dòng cùng lúc (vd: nguyên liệu 1 công thức). Nguyên tử: nếu thiếu
        /// BẤT KỲ dòng nào thì không trừ dòng nào cả.
        /// </summary>
        public bool TryConsume(IReadOnlyList<(InventoryItem item, int amount)> costs)
        {
            if (costs == null || costs.Count == 0) return true;

            foreach (var (item, amount) in costs)
                if (!Has(item, amount)) return false;

            foreach (var (item, amount) in costs)
                Remove(item, amount);
            return true;
        }

        /// <summary>Xoá sạch một loại nguyên liệu khỏi kho.</summary>
        public void Clear(InventoryItem itemDefinition)
        {
            if (itemDefinition != null) Remove(itemDefinition, GetQuantity(itemDefinition));
        }

        /// <summary>Xoá sạch toàn bộ kho.</summary>
        public void ClearAll()
        {
            _items.Clear();
            RaiseChanged();
        }

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private InventoryItem GetOrCreateRuntime(InventoryItem definition)
        {
            if (_items.TryGetValue(definition.Id, out var existing)) return existing;

            var runtime = definition.CreateRuntimeInstance();
            _items[definition.Id] = runtime;
            return runtime;
        }

        private static bool Validate(InventoryItem item, string caller)
        {
            if (item != null) return true;
            Debug.LogError($"[Inventory] {caller} nhận InventoryItem null.");
            return false;
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
