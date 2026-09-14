using System;
using System.Collections.Generic;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// CRUD Controller quản lý toàn bộ hệ thống Nội Thất, Trang Bị Slot-Based và Mở Rộng Không Gian.
    /// </summary>
    public sealed class DecorController : IService
    {
        private readonly Dictionary<string, DecorItem> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ExpansionZoneId, ExpansionZoneData> _zones = new();
        private readonly HashSet<string> _unlockedItemIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<ExpansionZoneId> _unlockedZoneIds = new();
        private readonly Dictionary<string, string> _slotAssignments = new(StringComparer.OrdinalIgnoreCase);

        private DecorBuffs _cachedTotalBuffs = DecorBuffs.Zero;

        // Events
        public event Action Changed;
        public event Action<string, DecorItem> DecorEquipped;
        public event Action<string> DecorUnequipped;
        public event Action<ExpansionZoneId> ZoneUnlocked;
        public event Action<DecorBuffs> BuffsChanged;

        // Properties
        public IReadOnlyCollection<string> UnlockedItemIds => _unlockedItemIds;
        public IReadOnlyCollection<ExpansionZoneId> UnlockedZoneIds => _unlockedZoneIds;
        public IReadOnlyDictionary<string, string> SlotAssignments => _slotAssignments;
        public DecorBuffs TotalBuffs => _cachedTotalBuffs;

        // =====================================================================
        // IService
        // =====================================================================

        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[DecorController] Khởi tạo hệ thống Decor Controller — {_items.Count} items, {_zones.Count} zones.");
        }

        public void Shutdown()
        {
            _items.Clear();
            _zones.Clear();
            _unlockedItemIds.Clear();
            _unlockedZoneIds.Clear();
            _slotAssignments.Clear();
            _cachedTotalBuffs = DecorBuffs.Zero;
        }

        // =====================================================================
        // CREATE / REGISTER
        // =====================================================================

        public void RegisterItem(DecorItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;

            _items[item.Id] = item;
            if (item.IsDefaultUnlocked)
            {
                _unlockedItemIds.Add(item.Id);
            }
        }

        public void RegisterZone(ExpansionZoneData zone)
        {
            if (zone == null) return;

            _zones[zone.ZoneId] = zone;
            if (zone.IsDefaultUnlocked)
            {
                _unlockedZoneIds.Add(zone.ZoneId);
            }
        }

        public void RegisterFromRepository(IDecorRepository repository)
        {
            if (repository == null) return;

            foreach (var item in repository.GetAllDecor())
            {
                RegisterItem(item);
            }

            foreach (var zone in repository.GetAllZones())
            {
                RegisterZone(zone);
            }

            // Mặc định quầy bar chính luôn được trang bị sẵn (quầy phục vụ cốt lõi của quán)
            if (_items.ContainsKey("item_counter_emerald") && !_slotAssignments.ContainsKey("slot_counter_main"))
            {
                _slotAssignments["slot_counter_main"] = "item_counter_emerald";
            }

            RecalculateBuffs();
            Changed?.Invoke();
        }

        // =====================================================================
        // READ
        // =====================================================================

        public DecorItem GetItem(string id) =>
            !string.IsNullOrEmpty(id) && _items.TryGetValue(id, out var item) ? item : null;

        public DecorItem[] GetAllItems() => _items.Values.ToArray();

        public DecorItem[] GetItemsByCategory(DecorCategory category) =>
            _items.Values.Where(i => i.Category == category).ToArray();

        public ExpansionZoneData GetZone(ExpansionZoneId zoneId) =>
            _zones.TryGetValue(zoneId, out var zone) ? zone : null;

        public bool IsUnlocked(string decorId) =>
            !string.IsNullOrEmpty(decorId) && _unlockedItemIds.Contains(decorId);

        public bool IsZoneUnlocked(ExpansionZoneId zoneId) =>
            _unlockedZoneIds.Contains(zoneId);

        public string GetEquippedItemId(string slotId) =>
            !string.IsNullOrEmpty(slotId) && _slotAssignments.TryGetValue(slotId, out var id) ? id : null;

        public DecorItem GetEquippedItem(string slotId)
        {
            var id = GetEquippedItemId(slotId);
            return !string.IsNullOrEmpty(id) ? GetItem(id) : null;
        }

        // =====================================================================
        // UPDATE: UNLOCK & EQUIP
        // =====================================================================

        /// <summary>
        /// Mua và mở khóa một món nội thất mới bằng tiền mặt (yêu cầu đủ điểm Danh tiếng).
        /// </summary>
        public bool TryUnlockDecor(string decorId, CurrencyController currency)
        {
            var item = GetItem(decorId);
            if (item == null)
            {
                Debug.LogWarning($"[DecorController] Không tìm thấy nội thất với ID '{decorId}'.");
                return false;
            }

            if (IsUnlocked(decorId))
            {
                Debug.Log($"[DecorController] '{item.DisplayName}' đã được mở khóa trước đó.");
                return true;
            }

            if (currency != null)
            {
                if (currency.Reputation < item.RequiredReputation)
                {
                    Debug.LogWarning($"[DecorController] Chưa đủ Danh tiếng ({currency.Reputation:N0} < {item.RequiredReputation:N0}).");
                    return false;
                }

                if (!currency.SpendMoney(item.Price))
                {
                    Debug.LogWarning($"[DecorController] Không đủ tiền để mua '{item.DisplayName}' ({currency.CurrentMoney:N0} < {item.Price:N0}).");
                    return false;
                }

                if (item.ReputationBonus > 0)
                {
                    currency.AddReputation(item.ReputationBonus);
                }
            }

            _unlockedItemIds.Add(decorId);
            Changed?.Invoke();
            Debug.Log($"[DecorController] Mua thành công '{item.DisplayName}'! Đã cộng +{item.ReputationBonus} điểm danh tiếng.");
            return true;
        }

        /// <summary>
        /// Gán một món nội thất vào một Slot cụ thể trong quán.
        /// </summary>
        public bool TryEquipDecor(string slotId, string decorId)
        {
            if (string.IsNullOrEmpty(slotId)) return false;

            var item = GetItem(decorId);
            if (item == null)
            {
                Debug.LogWarning($"[DecorController] Không tìm thấy nội thất '{decorId}' để trang bị.");
                return false;
            }

            if (!IsUnlocked(decorId))
            {
                Debug.LogWarning($"[DecorController] Nội thất '{item.DisplayName}' chưa được mở khóa.");
                return false;
            }

            _slotAssignments[slotId] = decorId;
            RecalculateBuffs();

            DecorEquipped?.Invoke(slotId, item);
            Changed?.Invoke();
            Debug.Log($"[DecorController] Đã trang bị '{item.DisplayName}' vào slot '{slotId}'.");
            return true;
        }

        /// <summary>
        /// Gỡ bỏ nội thất khỏi slot.
        /// </summary>
        public bool TryUnequipDecor(string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || !_slotAssignments.Remove(slotId))
            {
                return false;
            }

            RecalculateBuffs();
            DecorUnequipped?.Invoke(slotId);
            Changed?.Invoke();
            Debug.Log($"[DecorController] Đã tháo nội thất khỏi slot '{slotId}'.");
            return true;
        }

        /// <summary>
        /// Mở rộng một phân vùng không gian quán cà phê mới.
        /// </summary>
        public bool TryUnlockZone(ExpansionZoneId zoneId, CurrencyController currency)
        {
            var zone = GetZone(zoneId);
            if (zone == null)
            {
                Debug.LogWarning($"[DecorController] Không tìm thấy khu vực '{zoneId}'.");
                return false;
            }

            if (IsZoneUnlocked(zoneId))
            {
                Debug.Log($"[DecorController] Khu vực '{zone.ZoneName}' đã mở khóa trước đó.");
                return true;
            }

            if (currency != null)
            {
                if (currency.Reputation < zone.RequiredReputation)
                {
                    Debug.LogWarning($"[DecorController] Chưa đủ Danh tiếng để mở rộng '{zone.ZoneName}' ({currency.Reputation:N0} < {zone.RequiredReputation:N0}).");
                    return false;
                }

                if (!currency.SpendMoney(zone.UnlockPrice))
                {
                    Debug.LogWarning($"[DecorController] Không đủ tiền để mở rộng '{zone.ZoneName}' ({currency.CurrentMoney:N0} < {zone.UnlockPrice:N0}).");
                    return false;
                }

                if (zone.ReputationBonus > 0)
                {
                    currency.AddReputation(zone.ReputationBonus);
                }
            }

            _unlockedZoneIds.Add(zoneId);
            ZoneUnlocked?.Invoke(zoneId);
            Changed?.Invoke();
            Debug.Log($"[DecorController] Khai mở thành công '{zone.ZoneName}'! Nhận +{zone.ReputationBonus} điểm danh tiếng.");
            return true;
        }

        // =====================================================================
        // BUFF CALCULATION
        // =====================================================================

        private void RecalculateBuffs()
        {
            var total = DecorBuffs.Zero;
            foreach (var decorId in _slotAssignments.Values)
            {
                var item = GetItem(decorId);
                if (item == null) continue;

                total.TotalSeatingCapacity += item.SeatingCapacity;
                total.PatienceBonusPercent += item.PatienceBonusPercent;
                total.TipChanceBonus += item.TipChanceBonus;
                total.MoneyPerSecondBonus += item.MoneyPerSecondBonus;
            }

            _cachedTotalBuffs = total;
            BuffsChanged?.Invoke(_cachedTotalBuffs);
        }

        // =====================================================================
        // DELETE / RESET
        // =====================================================================

        public void ResetAll()
        {
            _unlockedItemIds.Clear();
            _unlockedZoneIds.Clear();
            _slotAssignments.Clear();

            foreach (var item in _items.Values)
            {
                if (item.IsDefaultUnlocked) _unlockedItemIds.Add(item.Id);
            }

            foreach (var zone in _zones.Values)
            {
                if (zone.IsDefaultUnlocked) _unlockedZoneIds.Add(zone.ZoneId);
            }

            RecalculateBuffs();
            Changed?.Invoke();
        }
    }
}
