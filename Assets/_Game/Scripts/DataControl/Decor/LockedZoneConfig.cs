using System;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Cấu hình tùy biến cho một khu vực bị khóa trong quán cà phê (Inspector-driven).
    /// Cho phép nhà phát triển / thiết kế tự do chỉ định Zone nào sẽ bị khóa và mức giá mở khóa tùy ý mà không bị gò bó theo GDD.
    /// </summary>
    [Serializable]
    public class LockedZoneConfig
    {
        [Tooltip("Định danh khu vực muốn khóa")]
        public ExpansionZoneId zoneId = ExpansionZoneId.Lounge_Zone2;

        [Tooltip("Tên hiển thị của khu vực trên huy hiệu ổ khóa")]
        public string displayName = "Sảnh Trong Nhà";

        [Tooltip("Số tiền (VNĐ) cần để mở khóa khu vực này")]
        [Min(0)]
        public int unlockPrice = 400000;

        [Tooltip("Điểm danh tiếng yêu cầu để mở khóa (0 = không yêu cầu)")]
        [Min(0)]
        public int requiredReputation = 0;

        [Tooltip("Điểm danh tiếng thưởng thêm cho quán khi mở khóa thành công")]
        [Min(0)]
        public int reputationBonus = 1000;

        [Tooltip("Hình ảnh layout che phủ (bạt thi công / công trình cải tạo). Nếu để trống sẽ dùng sprite mặc định.")]
        public Sprite customOverlaySprite;
    }
}
