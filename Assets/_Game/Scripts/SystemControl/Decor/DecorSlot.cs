using System;
using System.Collections.Generic;
using DreamCafe.DataControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Góc nhìn tường: Tường Trái (Slope +0.5 / Mặc định) hoặc Tường Phải (Slope -0.5 / Lật 180° Y).
    /// </summary>
    public enum WallPerspective
    {
        LeftWall = 0,   // Tường Trái / Tường Sau (Slope +0.5)
        RightWall = 1   // Tường Phải (Slope -0.5 / Lật 180° Y)
    }

    /// <summary>
    /// Cấu hình độ lệch vị trí spawn (local offset) của từng món nội thất cụ thể trên slot.
    /// </summary>
    [Serializable]
    public class ItemSpawnOffset
    {
        public string itemId = string.Empty;
        public Vector2 offset = Vector2.zero;
    }

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

        [Header("Góc nhìn tường (Wall Perspective)")]
        [SerializeField] private WallPerspective _wallPerspective = WallPerspective.LeftWall;

        [Header("Lớp hiển thị (Sorting Layer & Order Override)")]
        [SerializeField] private bool _overrideSorting = false;
        [SerializeField] private string _customSortingLayer = "Default";
        [SerializeField] private int _customSortingOrder = 18;

        [Header("Độ lệch vị trí tùy chỉnh theo từng Item")]
        [SerializeField] private List<ItemSpawnOffset> _itemOffsets = new();

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
        /// <summary>Các điểm neo ghế — dùng để quy ghế về ô lưới và cho AI tìm chỗ ngồi.</summary>
        public IReadOnlyList<Transform> SeatAnchors => _seatAnchors;
        public GameObject EmptyIndicator { get => _emptyIndicator; set => _emptyIndicator = value; }
        public Transform MountPoint => _mountPoint != null ? _mountPoint : transform;
        public GameObject SpawnedInstance => _spawnedInstance;
        public IReadOnlyList<ItemSpawnOffset> ItemOffsets => _itemOffsets;
        public WallPerspective WallPerspectiveSetting => _wallPerspective;
        public bool OverrideSorting => _overrideSorting;
        public string CustomSortingLayer => _customSortingLayer;
        public int CustomSortingOrder => _customSortingOrder;

        private void Awake()
        {
            if (_mountPoint == null) _mountPoint = transform;
            _seatOccupiedFlags = new bool[_seatAnchors.Length];

            // Bảo đảm slot tường và các phần tử con luôn giữ góc chuẩn (0, 0, 0), góc nhìn do WallPerspective điều khiển
            if (_allowedCategory == DecorCategory.WallDecor)
            {
                NormalizeWallSlotTransforms();
            }

            // Bảo đảm luôn có Collider2D để nhận tương tác OnMouseDown.
            //
            // Cắt đúng hình thoi MỘT ô chứ không dùng khung 1.0 x 0.8 như trước: slot chỉ đứng trên
            // một ô, mà khung chữ nhật thì trùm luôn cả bốn ô chéo quanh nó — bấm vào ô bên cạnh
            // cũng chọn phải slot này, và hai slot kề nhau thì khung chồng lên nhau.
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<PolygonCollider2D>();
                Vector2 half = CellHalfExtents();
                col.pathCount = 1;
                col.SetPath(0, new[]
                {
                    new Vector2(-half.x, 0f), new Vector2(0f, half.y),
                    new Vector2(half.x, 0f), new Vector2(0f, -half.y)
                });
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_allowedCategory == DecorCategory.WallDecor)
            {
                NormalizeWallSlotTransforms();
            }

            UpdateWallPerspectiveVisuals();
        }
#endif

        private void Start()
        {
            UpdateWallPerspectiveVisuals();

            if (_emptyIndicator != null)
            {
                _emptyIndicator.SetActive(_currentDecorItem == null);
            }
        }

        /// <summary>Nửa kích thước một ô sàn; không có Grid trong scene thì lấy mặc định 1 x 0.5.</summary>
        private static Vector2 CellHalfExtents()
        {
            var grid = FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            return grid != null
                ? new Vector2(grid.cellSize.x * 0.5f, grid.cellSize.y * 0.5f)
                : new Vector2(0.5f, 0.25f);
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

        private void ClearMountPointChildren()
        {
            if (_mountPoint != null)
            {
                for (int i = _mountPoint.childCount - 1; i >= 0; i--)
                {
                    var child = _mountPoint.GetChild(i).gameObject;
                    DestroyImmediate(child);
                }
            }
            _spawnedInstance = null;
        }

        /// <summary>
        /// Kiểm tra xem slot có cấu hình offset riêng cho item này hay không.
        /// </summary>
        public bool HasCustomOffset(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            for (int i = 0; i < _itemOffsets.Count; i++)
            {
                if (_itemOffsets[i] != null && _itemOffsets[i].itemId == itemId) return true;
            }
            return false;
        }

        /// <summary>
        /// Lấy độ lệch vị trí (local offset) của item trên slot này.
        /// </summary>
        public Vector2 GetItemOffset(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return Vector2.zero;
            for (int i = 0; i < _itemOffsets.Count; i++)
            {
                if (_itemOffsets[i] != null && _itemOffsets[i].itemId == itemId) return _itemOffsets[i].offset;
            }
            return Vector2.zero;
        }

        /// <summary>
        /// Lấy offset hiệu dụng: nếu slot có offset riêng thì ưu tiên dùng, nếu không thì dùng DefaultSpawnOffset của item.
        /// </summary>
        public Vector2 GetEffectiveOffset(DecorItem item)
        {
            if (item == null) return Vector2.zero;
            return HasCustomOffset(item.Id) ? GetItemOffset(item.Id) : item.DefaultSpawnOffset;
        }

        /// <summary>
        /// Gán hoặc cập nhật offset cho một item trên slot này. Cập nhật ngay mô hình nếu đang hiển thị.
        /// </summary>
        public void SetItemOffset(string itemId, Vector2 offset)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            bool found = false;
            for (int i = 0; i < _itemOffsets.Count; i++)
            {
                if (_itemOffsets[i] != null && _itemOffsets[i].itemId == itemId)
                {
                    _itemOffsets[i].offset = offset;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                _itemOffsets.Add(new ItemSpawnOffset { itemId = itemId, offset = offset });
            }

            if (_spawnedInstance != null && _currentDecorItem != null && _currentDecorItem.Id == itemId)
            {
                _spawnedInstance.transform.localPosition = (Vector3)offset;
            }
        }

        /// <summary>
        /// Xóa bỏ cấu hình offset riêng cho item (trở về dùng DefaultSpawnOffset của item).
        /// </summary>
        public bool RemoveItemOffset(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            for (int i = 0; i < _itemOffsets.Count; i++)
            {
                if (_itemOffsets[i] != null && _itemOffsets[i].itemId == itemId)
                {
                    _itemOffsets.RemoveAt(i);
                    if (_spawnedInstance != null && _currentDecorItem != null && _currentDecorItem.Id == itemId)
                    {
                        _spawnedInstance.transform.localPosition = (Vector3)_currentDecorItem.DefaultSpawnOffset;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Thay đổi góc nhìn phối cảnh tường (Tường Trái / Tường Phải).
        /// </summary>
        public void SetWallPerspective(WallPerspective perspective)
        {
            _wallPerspective = perspective;
            UpdateWallPerspectiveVisuals();
        }

        /// <summary>
        /// Chuyển đổi qua lại giữa góc tường trái và tường phải.
        /// </summary>
        public void ToggleWallPerspective()
        {
            _wallPerspective = (_wallPerspective == WallPerspective.LeftWall)
                ? WallPerspective.RightWall
                : WallPerspective.LeftWall;
            UpdateWallPerspectiveVisuals();
        }

        /// <summary>
        /// Chuẩn hóa rotation của slot tường về (0,0,0) để tránh việc xoay 3D làm ngược hướng tranh với ô slot.
        /// </summary>
        public void NormalizeWallSlotTransforms()
        {
            if (transform.localEulerAngles != Vector3.zero)
            {
                transform.localRotation = Quaternion.identity;
            }
            if (_mountPoint != null && _mountPoint.localEulerAngles != Vector3.zero)
            {
                _mountPoint.localRotation = Quaternion.identity;
            }
            if (_emptyIndicator != null && _emptyIndicator.transform.localEulerAngles != Vector3.zero)
            {
                _emptyIndicator.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Cập nhật hình ảnh góc nghiêng của item đang hiển thị và chỉ báo ô trống theo góc tường.
        /// </summary>
        public void UpdateWallPerspectiveVisuals()
        {
            if (_allowedCategory == DecorCategory.WallDecor)
            {
                NormalizeWallSlotTransforms();
            }

            if (_emptyIndicator != null)
            {
                var indSr = _emptyIndicator.GetComponentInChildren<SpriteRenderer>();
                if (indSr != null)
                {
                    indSr.flipX = (_allowedCategory == DecorCategory.WallDecor && _wallPerspective == WallPerspective.RightWall);
                }
            }

            if (_spawnedInstance != null && _currentDecorItem != null)
            {
                ApplyWallPerspectiveToInstance(_spawnedInstance, _currentDecorItem);
            }
        }

        /// <summary>
        /// Áp dụng sprite hoặc lật ảnh phù hợp với góc tường cho GameObject hiển thị.
        /// </summary>
        public void ApplyWallPerspectiveToInstance(GameObject instance, DecorItem item)
        {
            if (instance == null || item == null) return;
            if (_allowedCategory != DecorCategory.WallDecor) return;

            var sr = instance.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

            if (_wallPerspective == WallPerspective.RightWall)
            {
                if (item.RightWallSprite != null)
                {
                    sr.sprite = item.RightWallSprite;
                    sr.flipX = false;
                }
                else
                {
                    sr.flipX = true;
                }
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // LeftWall: Sử dụng sprite chuẩn gốc của item
                var origSr = item.Prefab != null ? item.Prefab.GetComponentInChildren<SpriteRenderer>() : null;
                if (origSr != null && origSr.sprite != null)
                {
                    sr.sprite = origSr.sprite;
                }
                sr.flipX = false;
                instance.transform.localRotation = Quaternion.identity;
            }

            // Bảo vệ lộn ngược nếu trục Up hướng xuống
            if (instance.transform.up.y < -0.1f)
            {
                instance.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }
        }

        /// <summary>
        /// Áp lớp hiển thị cho món vừa dựng. Mặc định dùng quy tắc 2.5D chung
        /// (<see cref="Rendering.IsoDepthSorter"/>): mảnh nào chân thấp hơn thì vẽ đè lên trên, nhờ
        /// vậy khách đi ngang bàn sẽ chìm/nổi đúng chỗ. Slot nào bật <see cref="_overrideSorting"/>
        /// thì vẫn theo số gán tay như cũ.
        /// </summary>
        public void ApplySortingToInstance(GameObject instance)
        {
            if (instance == null) return;

            if (!_overrideSorting && _allowedCategory != DecorCategory.OutdoorPlanter)
            {
                var sorter = instance.GetComponent<Rendering.IsoDepthSorter>();
                if (sorter == null) sorter = instance.AddComponent<Rendering.IsoDepthSorter>();

                // Nút hình của món bị nhích lên một khoảng (_artOffset) cho đẹp mắt, nên điểm đặt
                // của từng mảnh nằm cao hơn ô nó đứng đúng bằng khoảng đó. Trừ lại thì mọi mảnh
                // quy về tâm ô thật — cùng một thước với gót chân của khách.
                var occupant = instance.GetComponent<GridOccupant>();
                Vector2 ground = occupant != null ? -occupant.ArtOffset : Vector2.zero;

                // Ghế thì ghim thẳng về tâm ô ghế, không suy ra từ transform. Trừ _artOffset chỉ
                // đúng khi mọi mảnh được đặt đúng ô của nó; bộ sofa không như vậy — ghế của nó lệch
                // gần nửa đơn vị, tức gần hai hàng ô, đủ để cái ghế vẽ đè lên khách đang ngồi.
                sorter.ClearGroundPins();
                if (occupant != null)
                {
                    for (int i = 0; i < occupant.SeatCount; i++)
                    {
                        var chair = occupant.SeatChairArt(i);
                        if (chair != null) sorter.PinGround(chair, occupant.SeatWorldPosition(i));
                    }
                }

                sorter.Configure(Rendering.IsoDepthMode.PerRenderer, continuous: false,
                                 groundOffset: ground, layerInCell: Rendering.IsoDepth.SlotFurniture);
                return;
            }

            // Nếu là chậu hoa OutdoorPlanter hoặc được đánh dấu ghi đè sorting
            if (_allowedCategory == DecorCategory.OutdoorPlanter || _overrideSorting)
            {
                string targetLayer = (_overrideSorting && !string.IsNullOrEmpty(_customSortingLayer))
                    ? _customSortingLayer
                    : "Default";
                int targetOrder = _overrideSorting ? _customSortingOrder : 18;

                var srs = instance.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    srs[i].sortingLayerName = targetLayer;
                    srs[i].sortingOrder = targetOrder;
                }
            }
        }

        /// <summary>
        /// Hiển thị mô hình nội thất tại slot này.
        /// </summary>
        public void DisplayItem(DecorItem item)
        {
            ClearMountPointChildren();
            _currentDecorItem = item;

            if (item != null && item.Prefab != null)
            {
                _spawnedInstance = Instantiate(item.Prefab, _mountPoint);
                Vector3 effectiveOffset = (Vector3)GetEffectiveOffset(item);
                _spawnedInstance.transform.localPosition = effectiveOffset;
                _spawnedInstance.transform.localRotation = Quaternion.identity;
                _spawnedInstance.transform.localScale = item.Prefab.transform.localScale;

                // Tự động căn chỉnh theo góc nhìn tường
                ApplyWallPerspectiveToInstance(_spawnedInstance, item);

                // Tự động áp dụng Sorting Layer (chậu hoa lên trước bàn ghế)
                ApplySortingToInstance(_spawnedInstance);
            }

            if (_emptyIndicator != null)
            {
                _emptyIndicator.SetActive(item == null);
                var indSr = _emptyIndicator.GetComponentInChildren<SpriteRenderer>();
                if (indSr != null)
                {
                    indSr.flipX = (_allowedCategory == DecorCategory.WallDecor && _wallPerspective == WallPerspective.RightWall);
                }
            }

            SetPromptBubble(false);
        }

        /// <summary>
        /// Gỡ bỏ hiển thị nội thất khỏi slot.
        /// </summary>
        public void ClearDisplay()
        {
            ClearMountPointChildren();
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
        /// Bàn này đã có ít nhất một người ngồi chưa. Dùng để ưu tiên xếp khách vào bàn còn trống
        /// hẳn thay vì nhét chung bàn với người lạ.
        /// </summary>
        public bool HasOccupiedSeat()
        {
            for (int i = 0; i < _seatOccupiedFlags.Length; i++)
            {
                if (_seatOccupiedFlags[i]) return true;
            }
            return false;
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
