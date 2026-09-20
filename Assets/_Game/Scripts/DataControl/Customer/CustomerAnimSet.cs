using System;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Bộ khung hình của MỘT khách. Gom lại một chỗ thay vì rải từng ô Sprite rời, để chỗ nào cần
    /// hình khách cũng nhận đúng một thứ và biết ngay bộ này đã khai hay chưa.
    ///
    /// Cố ý KHÔNG có đường lui về ảnh đại diện. Trước đây <c>WalkFront</c> tự trả về avatar khi
    /// chưa khai hình đi — mà avatar thì không bao giờ null, nên nơi gọi không tài nào phân biệt
    /// "đã khai" với "chưa khai", và mỗi khách lại hiện avatar riêng của nó. Chưa khai thì
    /// <see cref="HasWalk"/> phải trả về false để nơi gọi tự quyết dùng bộ dự phòng.
    /// </summary>
    [Serializable]
    public struct CustomerAnimSet
    {
        [Tooltip("Hình lúc đi XUỐNG phía người xem (thấy mặt).")]
        public Sprite walkFront;

        [Tooltip("Hình lúc đi LÊN phía trong quán (thấy lưng).")]
        public Sprite walkBack;

        [Tooltip("Hình lúc ngồi, tay không. Bỏ trống thì ngồi vẫn giữ hình đi.")]
        public Sprite sit;

        [Tooltip("Hình lúc ngồi cầm tách cà phê. Bỏ trống thì không dùng tới.")]
        public Sprite sitCoffee;

        [Tooltip("Hình lúc ngồi cầm ly nước. Bỏ trống thì không dùng tới.")]
        public Sprite sitDrink;

        /// <summary>Bộ này có khai hình đi hay không. Thiếu một trong hai vẫn tính là có.</summary>
        public bool HasWalk => walkFront != null || walkBack != null;

        /// <summary>Số kiểu ngồi đã khai.</summary>
        public int SitCount =>
            (sit != null ? 1 : 0) + (sitCoffee != null ? 1 : 0) + (sitDrink != null ? 1 : 0);

        /// <summary>
        /// Kiểu ngồi thứ <paramref name="index"/> trong các kiểu ĐÃ KHAI (đếm vòng). Cho phép mỗi
        /// khách chọn một kiểu rồi giữ nguyên suốt lượt ngồi, thay vì đổi tay giữa chừng.
        /// </summary>
        public Sprite SitPose(int index)
        {
            int count = SitCount;
            if (count <= 0) return null;

            index = ((index % count) + count) % count;
            if (sit != null && index-- == 0) return sit;
            if (sitCoffee != null && index-- == 0) return sitCoffee;
            if (sitDrink != null && index-- == 0) return sitDrink;
            return sit != null ? sit : (sitCoffee != null ? sitCoffee : sitDrink);
        }

        /// <summary>Hình đi theo hướng; thiếu hình của hướng nào thì mượn hướng còn lại.</summary>
        public Sprite Walk(bool facingBack)
        {
            if (facingBack) return walkBack != null ? walkBack : walkFront;
            return walkFront != null ? walkFront : walkBack;
        }
    }
}
