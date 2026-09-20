using UnityEngine;

namespace DreamCafe.SystemControl.Rendering
{
    /// <summary>
    /// Quy tắc sắp lớp DUY NHẤT cho toàn bộ cảnh 2.5D. Mọi thứ đứng trên sàn — bàn, ghế, quầy,
    /// khách — đều phải quy về đúng công thức này.
    ///
    /// <para><b>Mỗi vật chỉ khai một thứ: NÓ ĐỨNG Ở ĐÂU TRÊN SÀN.</b> Không khai sprite cao bao
    /// nhiêu, không khai nó phải nằm trên/dưới vật nào. Từ điểm chạm sàn đó suy ra order:</para>
    ///
    /// <code>
    ///   order = (BaseY - chạmSàn.y) * 100     ← chân thấp hơn thì vẽ đè lên
    ///         -  chạmSàn.x * 1                ← phá hoà giữa các ô cùng độ cao
    ///         +  slot                          ← thứ tự trong CÙNG một ô
    /// </code>
    ///
    /// <para>Ba vế xếp theo độ ưu tiên giảm dần, và khoảng cách giữa chúng được giữ đủ xa để vế sau
    /// không bao giờ lật ngược được vế trước: một hàng ô cách nhau 25 bậc, vế X tối đa ~7.5 bậc
    /// (bề ngang quán), slot tối đa 3 bậc.</para>
    ///
    /// <para>Nhờ có <b>slot</b> mà không còn trường hợp đặc biệt nào. Khách ngồi lên ghế chỉ là
    /// "cùng ô với cái ghế nhưng slot cao hơn" — trước đây phải đi đọc sortingOrder runtime của
    /// ghế và bàn rồi chọn một số ở giữa, mà con số đó lại phụ thuộc bộ sắp lớp của món nội thất
    /// đã chạy hay chưa. Còn việc khách ngồi ghế sau bàn thì phải chìm dưới bàn, ghế trước bàn thì
    /// phải nổi lên trên — đó là hệ quả tự nhiên của ô, không phải luật riêng.</para>
    /// </summary>
    public static class IsoDepth
    {
        /// <summary>Mốc quy chiếu, đặt cao hơn mọi thứ trong quán để order luôn dương.</summary>
        public const float BaseY = 8f;

        /// <summary>Số bậc order cho mỗi đơn vị thế giới. 100 đủ mịn để hai ô kề nhau (0.25) cách 25 bậc.</summary>
        public const float OrdersPerUnit = 100f;

        /// <summary>
        /// Số bậc cho mỗi đơn vị theo trục X, chỉ để PHÁ HOÀ giữa các ô cùng độ cao màn hình.
        ///
        /// Trong lưới isometric, mọi ô trên cùng một đường chéo (cùng <c>cx + cy</c>) có chung độ
        /// cao — ô (0,0), (1,-1), (2,-2)… nằm ngang nhau. Chỉ so trục Y thì chúng hoà, mà sprite
        /// nội thất rộng hơn một ô nên vẫn chồng lên nhau. Vế này phân hạng theo đúng quy ước các
        /// tilemap trong scene đang dùng (Sort Order = <b>Top Right</b>): lệch về trên-phải thì
        /// nằm sau.
        /// </summary>
        public const float OrdersPerUnitX = 1f;

        // ---------------------------------------------------------------- slot trong cùng một ô

        /// <summary>Thảm, vệt sàn — nằm dưới cùng.</summary>
        public const int SlotFloor = 0;

        /// <summary>Bàn ghế, quầy, chậu cây. Mặc định cho mọi món nội thất.</summary>
        public const int SlotFurniture = 1;

        /// <summary>Khách và nhân vật. Cao hơn nội thất nên ngồi lên ghế là tự nổi trên mặt ghế.</summary>
        public const int SlotActor = 3;

        /// <summary>Khoảng slot rộng nhất được phép, để không lấn sang ô khác.</summary>
        public const int MaxSlot = 3;

        /// <summary>
        /// Order của một vật chạm sàn tại <paramref name="groundX"/>, <paramref name="groundY"/>.
        /// Điểm truyền vào phải là ĐIỂM CHẠM SÀN (tâm ô vật đứng), không phải tâm sprite và cũng
        /// không phải đáy khung sprite — khung có cả viền trong suốt, mỗi ảnh một kiểu.
        /// </summary>
        public static int OrderFor(float groundX, float groundY, int slot = SlotFurniture) =>
            Mathf.RoundToInt((BaseY - groundY) * OrdersPerUnit - groundX * OrdersPerUnitX)
            + Mathf.Clamp(slot, 0, MaxSlot);
    }
}
