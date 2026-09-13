using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject chứa danh mục toàn bộ DecorItem và ExpansionZoneData trong game.
    /// Đăng ký vào DatabaseManager để các hệ thống khác truy xuất.
    /// </summary>
    [CreateAssetMenu(fileName = "DecorRepository", menuName = "DreamCafe/Data/DecorRepository")]
    public sealed class ScriptableDecorRepository : ScriptableObject, IDecorRepository
    {
        [SerializeField] private DecorItem[] _items = Array.Empty<DecorItem>();
        [SerializeField] private ExpansionZoneData[] _zones = Array.Empty<ExpansionZoneData>();

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
    }
}
