using UnityEngine;

namespace DreamCafe.SystemControl.Rendering
{
    /// <summary>
    /// Quy tắc sắp lớp chung cho toàn bộ cảnh 2.5D: **vật nào có chân thấp hơn trên màn hình thì vẽ
    /// đè lên trên**. Mọi thứ đứng trên sàn — bàn, ghế, quầy, khách — đều phải quy về đúng một công
    /// thức này, nếu không sẽ lại rơi vào cảnh mỗi prefab một con số gán tay (bàn 15, ghế 14, quầy 8)
    /// rồi khách đi qua thì lúc chìm lúc nổi.
    ///
    /// Điểm so sánh là **mép dưới** của vật (chỗ chạm sàn), không phải tâm sprite: cái quầy cao 3
    /// đơn vị và cái ghế cao 0.5 đơn vị vẫn so với nhau công bằng ở chân.
    /// </summary>
    public static class IsoDepth
    {
        /// <summary>Mốc quy chiếu, đặt cao hơn mọi thứ trong quán để order luôn dương.</summary>
        public const float BaseY = 8f;

        /// <summary>Số bậc order cho mỗi đơn vị thế giới. 100 đủ mịn để hai ô kề nhau (0.25) cách 25 bậc.</summary>
        public const float OrdersPerUnit = 100f;

        /// <summary>Order tương ứng với một mép dưới trong toạ độ thế giới.</summary>
        public static int OrderFor(float worldBottomY) => Mathf.RoundToInt((BaseY - worldBottomY) * OrdersPerUnit);
    }
}
