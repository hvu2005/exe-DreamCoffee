using DreamCafe.Core.Utils;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Phân nhóm nguyên liệu — chỉ để UI lọc theo tab, không ảnh hưởng logic kho.
    /// </summary>
    public enum ItemCategory
    {
        Base = 0,       // Cơ bản: cà phê, trà, bột mì, đường
        Flavor = 1,     // Hương vị: syrup, vani, cacao
        Topping = 2,    // Topping: trân châu, kem, thạch
        Fresh = 3       // Đồ tươi: sữa, trứng, trái cây — hạn ngắn
    }

    /// <summary>
    /// "Cục SO" đại diện cho một loại nguyên liệu, gộp cả dữ liệu tĩnh (tên, icon, giá,
    /// hạn sử dụng) LẪN số lượng runtime (quantity) theo đúng ý đồ thiết kế: item = SO.
    ///
    /// LƯU Ý QUAN TRỌNG khi dùng kiểu này trong Unity:
    /// Asset .asset trên ổ đĩa là bản gốc (template) dùng chung. Nếu sửa thẳng `quantity`
    /// trên asset gốc lúc Play Mode, Unity sẽ ghi đè NGAY vào asset đó — số lượng bị lưu
    /// nhầm vào project, không mất khi thoát Play Mode như GameObject trong scene.
    /// => InventoryController KHÔNG BAO GIỜ chỉnh quantity trên asset gốc. Nó tạo bản sao
    /// runtime bằng <see cref="CreateRuntimeInstance"/> (ScriptableObject.Instantiate) rồi
    /// chỉ chỉnh số lượng trên bản sao đó. Asset gốc luôn giữ nguyên vai trò "định nghĩa".
    ///
    /// TODO: Thêm biến động giá theo mùa / độ hiếm khi làm hệ thống chợ.
    /// </summary>
    [CreateAssetMenu(fileName = "item_new", menuName = "DreamCafe/Inventory Item", order = 0)]
    public sealed class InventoryItem : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("ID bất biến dùng để so khớp/lưu game. Mặc định lấy theo tên asset.")]
        private string id;
        [SerializeField] private string displayName = "Nguyên liệu mới";
        [SerializeField, TextArea(2, 4)] private string description;

        [Header("Visual")]
        [SerializeField, Tooltip("Icon hiển thị trong kho/UI.")]
        private Sprite icon;
        [SerializeField, Tooltip("Màu tint phủ lên icon — dùng cho biến thể cùng icon gốc.")]
        private Color tint = Color.white;
        [SerializeField, HexColor]
        [Tooltip("Màu của nguyên liệu khi đổ vào cốc pha chế. Lưu dưới dạng mã hex (vd #6F4E37); bỏ trống thì lấy theo Tint.")]
        private string hexColor = "#FFFFFF";
        [SerializeField, Tooltip("Prefab hiển thị ngoài thế giới (vd: bao cà phê trên kệ) — có thể để trống.")]
        private GameObject worldPrefab;

        [Header("Phân loại & đơn vị")]
        [SerializeField] private ItemCategory category = ItemCategory.Base;
        [SerializeField, Tooltip("Đơn vị hiển thị: gói, hộp, ml, g...")]
        private string unitLabel = "phần";

        [Header("Kinh tế")]
        [SerializeField, Min(0), Tooltip("Giá nhập cho 1 đơn vị.")]
        private int basePrice = 10;

        [Header("Bảo quản")]
        [SerializeField, Min(0), Tooltip("Số ngày đến khi hỏng. 0 = không bao giờ hết hạn.")]
        private int shelfLifeDays;
        [SerializeField, Min(1), Tooltip("Số lượng tối đa được phép cộng dồn (vd: 1 kệ chứa tối đa).")]
        private int maxQuantity = 999;

        [Header("Runtime")]
        [SerializeField, Min(0), Tooltip("Số lượng đang có. CHỈ được chỉnh qua InventoryController trên bản runtime, không sửa tay trên asset gốc.")]
        private int quantity;

        // ---------- Dữ liệu tĩnh (đọc từ asset gốc hoặc bản runtime đều như nhau) ----------
        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color Tint => tint;

        /// <summary>Mã màu hex thô như designer gõ vào (vd "#6F4E37").</summary>
        public string HexColor => hexColor;

        /// <summary>
        /// Màu nguyên liệu này đổ ra trong cốc, dịch từ <see cref="HexColor"/>. Mã sai cú pháp hoặc
        /// bỏ trống thì rơi về <see cref="Tint"/> — cốc vẫn có màu chứ không thành đen thui.
        /// Kết quả được nhớ lại theo chuỗi hex, đổi mã trong Inspector là tự dịch lại.
        /// </summary>
        public Color LiquidColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(hexColor)) return tint;

                if (!string.Equals(_parsedHex, hexColor, System.StringComparison.Ordinal))
                {
                    _parsedHex = hexColor;
                    if (!ColorUtility.TryParseHtmlString(hexColor.Trim(), out _parsedColor)) _parsedColor = tint;
                }

                return _parsedColor;
            }
        }

        [System.NonSerialized] private string _parsedHex;
        [System.NonSerialized] private Color _parsedColor;

        public GameObject WorldPrefab => worldPrefab;
        public ItemCategory Category => category;
        public string UnitLabel => unitLabel;
        public int BasePrice => basePrice;
        public int ShelfLifeDays => shelfLifeDays;
        public int MaxQuantity => Mathf.Max(1, maxQuantity);
        public bool IsPerishable => shelfLifeDays > 0;

        // ---------- Dữ liệu runtime ----------
        /// <summary>Số lượng hiện có. Chỉ nên đọc từ bên ngoài — sửa qua InventoryController.</summary>
        public int Quantity => quantity;
        public bool IsEmpty => quantity <= 0;
        public bool IsFull => quantity >= MaxQuantity;

        /// <summary>Chỗ trống còn lại tới khi đầy stack.</summary>
        public int RemainingCapacity => Mathf.Max(0, MaxQuantity - quantity);

        /// <summary>
        /// Gán số lượng runtime, kẹp trong [0, MaxQuantity]. `internal` để chỉ code trong cùng
        /// assembly (InventoryController) gọi được — script ở assembly/plugin khác không chỉnh bừa.
        /// </summary>
        internal void SetQuantityInternal(int value) => quantity = Mathf.Clamp(value, 0, MaxQuantity);

        /// <summary>
        /// Tạo bản sao runtime để InventoryController thao tác — không đụng vào asset gốc trên đĩa.
        /// Gọi 1 lần khi item lần đầu được thêm vào kho.
        /// </summary>
        public InventoryItem CreateRuntimeInstance()
        {
            var instance = Instantiate(this);
            instance.name = name;      // bỏ hậu tố "(Clone)" cho log/debug dễ đọc
            instance.quantity = 0;
            return instance;
        }

        public override string ToString() => $"{DisplayName} x{quantity}";

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
#endif
    }
}
