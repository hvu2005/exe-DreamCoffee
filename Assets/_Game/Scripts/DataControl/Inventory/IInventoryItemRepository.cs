using DreamCafe.Core.Services;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Đọc danh mục nguyên liệu tĩnh (định nghĩa) — khác với InventoryController (số lượng runtime).
    /// Extends IService để tham gia vòng đời ServiceManager (Init/Shutdown là no-op).
    /// </summary>
    public interface IInventoryItemRepository : IService
    {
        InventoryItem GetItem(string id);
        InventoryItem[] GetAllItems();
        InventoryItem[] GetItemsByCategory(ItemCategory category);
    }
}
