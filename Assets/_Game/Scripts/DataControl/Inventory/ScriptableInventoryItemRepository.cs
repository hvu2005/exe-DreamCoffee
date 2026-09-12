using System;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject chứa danh sách toàn bộ InventoryItem definition — gán trong Inspector.
    /// Tạo qua Assets > Create > DreamCafe > Data > InventoryItemRepository.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryItemRepository", menuName = "DreamCafe/Data/InventoryItemRepository")]
    public sealed class ScriptableInventoryItemRepository : ScriptableObject, IInventoryItemRepository
    {
        [SerializeField] private InventoryItem[] items = Array.Empty<InventoryItem>();

        public void Init(ServiceContext ctx) => Debug.Log($"[InventoryItemRepository] Initialized — {items.Length} items.");
        public void Shutdown() { }

        public InventoryItem GetItem(string id) =>
            items.FirstOrDefault(i => i != null && i.Id == id);

        public InventoryItem[] GetAllItems() => items;

        public InventoryItem[] GetItemsByCategory(ItemCategory category) =>
            items.Where(i => i != null && i.Category == category).ToArray();
    }
}
