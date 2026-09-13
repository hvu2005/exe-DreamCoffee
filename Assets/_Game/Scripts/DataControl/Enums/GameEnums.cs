namespace DreamCafe.DataControl
{
    /// <summary>
    /// Các phân loại khách hàng trong game.
    /// </summary>
    public enum CustomerType
    {
        Student = 0,
        Worker = 1,
        Tourist = 2,
        VIP = 3,
        Influencer = 4
    }

    /// <summary>
    /// Các loại tiền tệ và chỉ số tài chính trong quán.
    /// </summary>
    public enum CurrencyType
    {
        Money = 0,
        MoneyPerSecond = 1,
        Reputation = 2
    }

    /// <summary>
    /// Các danh mục nội thất & trang trí trong quán cà phê.
    /// </summary>
    public enum DecorCategory
    {
        SeatingSet = 0,      // Bàn ghế đón khách (ảnh hưởng sức chứa chỗ ngồi)
        CounterStation = 1,  // Quầy bar, quầy thu ngân
        Appliance = 2,       // Thiết bị (máy pha cà phê, tủ bánh ngọt, tủ lạnh)
        FloorDecor = 3,      // Cây cảnh sàn, chậu kiểng, thảm
        WallDecor = 4,       // Tranh ảnh, bảng menu, đèn tường
        OutdoorPlanter = 5   // Bồn hoa, rào chắn ngoài hiên
    }

    /// <summary>
    /// Các phong cách thiết kế thẩm mỹ (Themes).
    /// </summary>
    public enum DecorTheme
    {
        CozyWood = 0,
        VintageClassic = 1,
        ModernEmerald = 2,
        CutePastel = 3
    }

    /// <summary>
    /// Định danh các khu vực mở rộng không gian quán.
    /// </summary>
    public enum ExpansionZoneId
    {
        Starter_Zone1 = 0,       // Khu vực khởi nghiệp (mặc định)
        Lounge_Zone2 = 1,        // Mở rộng sảnh trong nhà (ghế sofa, bàn 4 người)
        OutdoorPatio_Zone3 = 2   // Mở rộng sân hiên ngoại cảnh (dãy bàn ngoài trời)
    }
}

