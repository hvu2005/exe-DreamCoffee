using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject định nghĩa một món nội thất / vật phẩm trang trí (Decor Item Definition).
    /// </summary>
    [CreateAssetMenu(fileName = "DecorItem", menuName = "DreamCafe/Data/DecorItem")]
    public sealed class DecorItem : ScriptableObject
    {
        [Header("Thông tin định danh")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private DecorCategory _category;
        [SerializeField] private DecorTheme _theme;
        [SerializeField, Min(1)] private int _tier = 1;

        [Header("Kinh tế & Điều kiện")]
        [SerializeField, Min(0)] private int _price = 50000;
        [SerializeField, Min(0)] private int _requiredReputation = 0;
        [SerializeField, Min(0)] private int _reputationBonus = 100;
        [SerializeField] private bool _isDefaultUnlocked = false;

        [Header("Chỉ số tác động Gameplay")]
        [SerializeField, Range(0f, 1f), Tooltip("Tỉ lệ tăng thời gian kiên nhẫn cho khách (vd: 0.1 = +10%)")]
        private float _patienceBonusPercent = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Tỉ lệ tăng cơ hội nhận tip (vd: 0.05 = +5%)")]
        private float _tipChanceBonus = 0f;

        [SerializeField, Min(0), Tooltip("Sức chứa chỗ ngồi (nếu là bàn ghế, vd: 2 hoặc 4 khách)")]
        private int _seatingCapacity = 0;

        [SerializeField, Min(0f), Tooltip("Tốc độ cộng tiền thụ động trên giây (VNĐ/s)")]
        private float _moneyPerSecondBonus = 0f;

        [Header("Kích thước trên lưới ô")]
        [SerializeField, Tooltip("Số ô gạch món này chiếm trên sàn (ngang x dọc theo trục ô, không phải trục màn hình). " +
            "Ghế/chậu cây = 1x1, tủ bánh = 2x1. Khối ô được căn giữa quanh ô của slot.")]
        private Vector2Int _gridFootprint = Vector2Int.one;

        [SerializeField, Tooltip("Ô chiếm thêm ngoài khối chữ nhật, tính lệch so với ô gốc. Trục ô đi chéo trên màn hình: " +
            "+x chéo lên-phải, +y chéo lên-trái. Kéo dài ngang màn hình dùng (1,-1) và (-1,1); dọc màn hình dùng (1,1) và (-1,-1).")]
        private Vector2Int[] _extraCells = System.Array.Empty<Vector2Int>();

        [Header("Trực quan")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;
        [SerializeField, Tooltip("Độ lệch vị trí spawn mặc định trên mọi slot (nếu slot không có cấu hình riêng)")]
        private Vector2 _defaultSpawnOffset = Vector2.zero;
        [SerializeField, Tooltip("Sprite tương ứng khi đặt trên tường bên phải (slope -0.5 / lật 180° Y). Nếu để trống sẽ tự động lật Transform.")]
        private Sprite _rightWallSprite;

        // Public Getters
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public DecorCategory Category => _category;
        public DecorTheme Theme => _theme;
        public int Tier => _tier;
        public int Price => _price;
        public int RequiredReputation => _requiredReputation;
        public int ReputationBonus => _reputationBonus;
        public bool IsDefaultUnlocked => _isDefaultUnlocked;
        public float PatienceBonusPercent => _patienceBonusPercent;
        public float TipChanceBonus => _tipChanceBonus;
        public int SeatingCapacity => _seatingCapacity;
        public float MoneyPerSecondBonus => _moneyPerSecondBonus;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
        /// <summary>Số ô gạch món này chiếm trên sàn. Luôn tối thiểu 1x1.</summary>
        public Vector2Int GridFootprint => new(Mathf.Max(1, _gridFootprint.x), Mathf.Max(1, _gridFootprint.y));

        /// <summary>Các ô chiếm thêm ngoài khối chữ nhật, lệch so với ô gốc.</summary>
        public Vector2Int[] ExtraCells => _extraCells ?? System.Array.Empty<Vector2Int>();
        public Vector2 DefaultSpawnOffset => _defaultSpawnOffset;
        public Sprite RightWallSprite => _rightWallSprite;
    }
}
