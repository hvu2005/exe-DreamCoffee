using System;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Tổng hợp các chỉ số buff nhận được từ toàn bộ nội thất đang trang bị trong quán.
    /// </summary>
    [Serializable]
    public struct DecorBuffs
    {
        /// <summary>Tổng sức chứa chỗ ngồi (số lượng khách có thể ngồi cùng lúc).</summary>
        public int TotalSeatingCapacity;

        /// <summary>Tỉ lệ tăng thời gian kiên nhẫn của khách (% cộng thêm, vd: 0.15 = +15%).</summary>
        public float PatienceBonusPercent;

        /// <summary>Tỉ lệ tăng khả năng nhận tiền tip (% cộng thêm, vd: 0.1 = +10%).</summary>
        public float TipChanceBonus;

        /// <summary>Tốc độ sinh tiền thụ động tăng thêm mỗi giây (VNĐ/s).</summary>
        public float MoneyPerSecondBonus;

        public static DecorBuffs Zero => new()
        {
            TotalSeatingCapacity = 0,
            PatienceBonusPercent = 0f,
            TipChanceBonus = 0f,
            MoneyPerSecondBonus = 0f
        };

        public static DecorBuffs operator +(DecorBuffs a, DecorBuffs b)
        {
            return new DecorBuffs
            {
                TotalSeatingCapacity = a.TotalSeatingCapacity + b.TotalSeatingCapacity,
                PatienceBonusPercent = a.PatienceBonusPercent + b.PatienceBonusPercent,
                TipChanceBonus = a.TipChanceBonus + b.TipChanceBonus,
                MoneyPerSecondBonus = a.MoneyPerSecondBonus + b.MoneyPerSecondBonus
            };
        }
    }
}
