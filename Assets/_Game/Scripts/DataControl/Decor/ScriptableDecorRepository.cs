using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Một món nội thất đã nằm sẵn trong quán ngay từ lúc mở game (quầy bar, bộ bàn ghế khởi đầu...).
    /// Cấu hình trực tiếp trên Inspector của DecorRepository thay vì hard-code trong controller.
    /// </summary>
    [Serializable]
    public sealed class DecorDefaultPlacement
    {
        [Tooltip("Id của DecorSlot trong scene, vd: slot_table_02.")]
        public string slotId = string.Empty;

        [Tooltip("Món nội thất đặt sẵn vào slot đó. Món này được mở khóa luôn, người chơi không phải mua.")]
        public DecorItem item;
    }

    /// <summary>
    /// ScriptableObject chứa danh mục toàn bộ DecorItem và ExpansionZoneData trong game.
    /// Đăng ký vào DatabaseManager để các hệ thống khác truy xuất.
    /// </summary>
    [CreateAssetMenu(fileName = "DecorRepository", menuName = "DreamCafe/Data/DecorRepository")]
    public sealed class ScriptableDecorRepository : ScriptableObject, IDecorRepository
    {
        [SerializeField] private DecorItem[] _items = Array.Empty<DecorItem>();
        [SerializeField] private ExpansionZoneData[] _zones = Array.Empty<ExpansionZoneData>();

        [Header("Trang bị sẵn khi mở quán")]
        [SerializeField, Tooltip("Các slot đã có sẵn nội thất ngay từ đầu ván chơi (quầy bar, bàn ghế khởi đầu...).")]
        private DecorDefaultPlacement[] _defaultPlacements = Array.Empty<DecorDefaultPlacement>();

        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[DecorRepository] Initialized — {_items.Length} decor items, {_zones.Length} zones.");
        }

        public void Shutdown()
        {
        }

        public DecorItem GetDecor(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _items.FirstOrDefault(i => i != null && string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public DecorItem[] GetAllDecor() => _items;

        public DecorItem[] GetDecorByCategory(DecorCategory category) =>
            _items.Where(i => i != null && i.Category == category).ToArray();

        public ExpansionZoneData GetZone(ExpansionZoneId zoneId) =>
            _zones.FirstOrDefault(z => z != null && z.ZoneId == zoneId);

        public ExpansionZoneData[] GetAllZones() => _zones;

        public DecorDefaultPlacement[] GetDefaultPlacements() => _defaultPlacements;
    }
}
