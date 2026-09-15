using DreamCafe.Core.Services;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Interface truy xuất danh mục tĩnh của toàn bộ vật phẩm nội thất và khu vực mở rộng.
    /// </summary>
    public interface IDecorRepository : IService
    {
        /// <summary>Lấy thông tin một món nội thất theo ID.</summary>
        DecorItem GetDecor(string id);

        /// <summary>Lấy toàn bộ danh mục nội thất.</summary>
        DecorItem[] GetAllDecor();

        /// <summary>Lấy danh sách nội thất theo phân loại.</summary>
        DecorItem[] GetDecorByCategory(DecorCategory category);

        /// <summary>Lấy dữ liệu một khu vực mở rộng theo ID.</summary>
        ExpansionZoneData GetZone(ExpansionZoneId zoneId);

        /// <summary>Lấy danh sách toàn bộ các khu vực mở rộng.</summary>
        ExpansionZoneData[] GetAllZones();
    }
}
