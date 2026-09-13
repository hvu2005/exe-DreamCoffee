using System;
using DreamCafe.DataControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Vị trí đặt nội thất cố định (Slot-based Node) trong quán cà phê theo hệ tọa độ 2.5D Isometric.
    /// Quản lý việc hiển thị mô hình nội thất, cung cấp điểm neo ghế ngồi (SeatAnchor) cho khách,
    /// và xử lý tương tác nhấp chọn nâng cấp / đổi mẫu mã.
    /// </summary>
    [SelectionBase]
    public sealed class DecorSlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("Định danh & Quy tắc")]
        [SerializeField] private string _slotId = "slot_table_01";
        [SerializeField] private DecorCategory _allowedCategory = DecorCategory.SeatingSet;
        [SerializeField] private ExpansionZoneId _requiredZone = ExpansionZoneId.Starter_Zone1;

        [Header("Gắn kết trực quan")]
        [SerializeField] private Transform _mountPoint;
        [SerializeField, Tooltip("Các điểm neo ghế ngồi để AI khách hàng tìm đến và ngồi (chỉ dùng cho bàn ghế)")]
        private Transform[] _seatAnchors = Array.Empty<Transform>();

        [Header("Chỉ báo trạng thái (Upgrade / Lock Bubble)")]
        [SerializeField] private GameObject _promptBubble;

        [Header("Chỉ báo ô trống (Empty Indicator)")]
        [SerializeField] private GameObject _emptyIndicator;

        private GameObject _spawnedInstance;
        private DecorItem _currentDecorItem;
        private bool[] _seatOccupiedFlags = Array.Empty<bool>();

        // Events
        public static event Action<DecorSlot> SlotTapped;

        // Getters & Setters
        public string SlotId => _slotId;
        public DecorCategory AllowedCategory => _allowedCategory;
        public ExpansionZoneId RequiredZone => _requiredZone;
        public DecorItem CurrentDecorItem => _currentDecorItem;
        public int TotalSeats => _seatAnchors.Length;
        public GameObject EmptyIndicator { get => _emptyIndicator; set => _emptyIndicator = value; }

        private void Awake()
        {
            if (_mountPoint == null) _mountPoint = transform;
            _seatOccupiedFlags = new bool[_seatAnchors.Length];

            // Bảo đảm luôn có Collider2D để nhận tương tác OnMouseDown
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.0f, 0.8f);
            }
        }

        private void Start()
        {
            if (_emptyIndicator != null)
            {
                _emptyIndicator.SetActive(_currentDecorItem == null);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SlotTapped?.Invoke(this);
        }

        private void OnMouseDown()
        {
            // Bắn event khi người chơi click/tap vào slot
            SlotTapped?.Invoke(this);
        }

        /// <summary>
        /// Hiển thị mô hình nội thất tại slot này.
        /// </summary>
        public void DisplayItem(DecorItem item)
        {
            if (_spawnedInstance != null)
            {
                DestroyImmediate(_spawnedInstance);
                _spawnedInstance = null;
            }

            _currentDecorItem = item;

            if (item != null && item.Prefab != null)
            {
                _spawnedInstance = Instantiate(item.Prefab, _mountPoint);
                _spawnedInstance.transform.localPosition = Vector3.zero;
                _spawnedInstance.transform.localRotation = Quaternion.identity;
                _spawnedInstance.transform.localScale = item.Prefab.transform.localScale;
            }

            if (_emptyIndicator != null)
            {
                _emptyIndicator.SetActive(item == null);
            }

            SetPromptBubble(false);
        }

        /// <summary>
        /// Gỡ bỏ hiển thị nội thất khỏi slot.
        /// </summary>
        public void ClearDisplay()
        {
            if (_spawnedInstance != null)
            {
                DestroyImmediate(_spawnedInstance);
                _spawnedInstance = null;
            }

            _currentDecorItem = null;

            if (_emptyIndicator != null)
            {
                _emptyIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// Bật/tắt bong bóng chỉ báo nâng cấp hoặc chú ý trên đầu slot.
        /// </summary>
        public void SetPromptBubble(bool visible)
        {
            if (_promptBubble != null)
            {
                _promptBubble.SetActive(visible);
            }
        }

        // =====================================================================
        // QUẢN LÝ GHẾ NGỒI CHO AI KHÁCH HÀNG (CUSTOMER SEATING)
        // =====================================================================

        /// <summary>
        /// Tìm ghế trống đầu tiên tại bàn này. Trả về null nếu bàn đã đầy hoặc không có ghế.
        /// </summary>
        public Transform GetFreeSeat(out int seatIndex)
        {
            for (int i = 0; i < _seatAnchors.Length; i++)
            {
                if (!_seatOccupiedFlags[i] && _seatAnchors[i] != null)
                {
                    seatIndex = i;
                    return _seatAnchors[i];
                }
            }

            seatIndex = -1;
            return null;
        }

        /// <summary>
        /// Đánh dấu ghế đang có khách ngồi hoặc khách vừa rời đi.
        /// </summary>
        public void SetSeatOccupied(int seatIndex, bool occupied)
        {
            if (seatIndex >= 0 && seatIndex < _seatOccupiedFlags.Length)
            {
                _seatOccupiedFlags[seatIndex] = occupied;
            }
        }

        /// <summary>
        /// Kiểm tra bàn còn chỗ trống không.
        /// </summary>
        public bool HasFreeSeats()
        {
            for (int i = 0; i < _seatOccupiedFlags.Length; i++)
            {
                if (!_seatOccupiedFlags[i]) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.5f, 1f));

            if (_seatAnchors != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var seat in _seatAnchors)
                {
                    if (seat != null)
                    {
                        Gizmos.DrawWireSphere(seat.position, 0.2f);
                    }
                }
            }
        }
    }
}
