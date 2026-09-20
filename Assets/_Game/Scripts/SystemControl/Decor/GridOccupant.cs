using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Gắn lên từng món nội thất để khai báo **nó chiếm những ô nào trên lưới sàn**, và ô nào là chỗ
    /// ngồi. Toạ độ khai báo là **độ lệch so với ô của chính nó** (ô gốc = 0,0), nên cùng một prefab
    /// đặt ở đâu cũng đúng.
    ///
    /// Trục ô đi chéo trên màn hình: <c>+x</c> chéo lên-phải, <c>+y</c> chéo lên-trái. Muốn kéo dài
    /// ngang màn hình dùng (1,-1) và (-1,1); dọc màn hình dùng (1,1) và (-1,-1).
    ///
    /// Component tự đăng ký vào <see cref="ShopGrid"/> khi bật và tự rút ra khi tắt, nên kéo thả một
    /// món mới vào scene là bản đồ ô cập nhật theo — khách lập tức biết đường mà né.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GridOccupant : MonoBehaviour
    {
        /// <summary>Một chỗ ngồi: ô nào trên lưới, khách ngồi đúng điểm nào, và ghép với cái ghế nào trong art.</summary>
        [Serializable]
        public sealed class SeatSlot
        {
            [Tooltip("Ô chứa ghế, lệch so với ô gốc của món. Đây là ô khách đi tới và giữ chỗ.")]
            public Vector2Int cell = Vector2Int.zero;

            [Tooltip("Điểm ngồi gốc, lệch so với gốc món. Lệnh 'Đặt ô theo chân đồ vật' tự điền = vị trí " +
                     "cái ghế trong art. ĐỪNG sửa tay ở đây — chạy lại lệnh là mất.")]
            public Vector2 sitOffset = Vector2.zero;

            [Tooltip("Chỉnh tay vị trí ngồi: cộng thêm vào điểm ngồi gốc. Đây là chỗ để bạn nhích khách " +
                     "cho khít mặt ghế (lên/xuống/trái/phải). Tool KHÔNG bao giờ ghi đè trường này.")]
            public Vector2 sitAdjust = Vector2.zero;

            [Tooltip("Sprite cái ghế tương ứng. Dùng để tính lớp vẽ: khách ngồi phải nằm TRÊN ghế.")]
            public SpriteRenderer chairArt;

            [Tooltip("Tên gợi nhớ, chỉ để dễ đọc trong Inspector.")]
            public string label = "ghế";
        }

        [Header("Ô bị chiếm (khách phải né)")]
        [SerializeField, Tooltip("Các ô thân món chiếm chỗ, lệch so với ô gốc. Mặc định (0,0) là chính ô nó đứng.")]
        private Vector2Int[] _blockedCells = { Vector2Int.zero };

        [Header("Ô chỗ ngồi (khách tìm đường tới đây)")]
        [SerializeField, Tooltip("Mỗi phần tử là một chỗ ngồi. Ô ghế vẫn đi vào được — không thì khách không ngồi xuống nổi.")]
        private SeatSlot[] _seats = Array.Empty<SeatSlot>();

        [Header("Hình vẽ")]
        [SerializeField, Tooltip("Nút chứa toàn bộ sprite của món. Bỏ trống thì tìm con tên 'Art'.")]
        private Transform _artRoot;

        [SerializeField, Tooltip("Nhích hình vẽ so với ô sàn. Đây là CHỖ DUY NHẤT chỉnh vị trí hình — " +
            "ô lưới không đổi theo, nên nhích thoải mái mà không sợ lệch bản đồ đường đi.")]
        private Vector2 _artOffset = Vector2.zero;

        [Header("Neo ô gốc")]
        [SerializeField, Tooltip("Bỏ trống thì lấy DecorSlot cha làm ô gốc (vị trí logic); không có slot thì lấy chính nó. " +
            "Gán tay khi muốn chỉ định điểm khác.")]
        private Transform _anchorOverride;

        [Header("Debug")]
        [SerializeField, Tooltip("Vẽ ô chiếm (đỏ) và ô ghế (xanh) trong Scene view.")]
        private bool _drawGizmos = true;

        private bool[] _seatTaken;
        private ShopGrid _cachedGrid;
        private Transform _cachedAnchor;

        /// <summary>
        /// Lưới đang dùng. Không bám cứng vào <see cref="ShopGrid.Instance"/>: trong Edit mode (và cả
        /// lúc chạy, nếu occupant bật trước ShopGrid) thì Instance còn rỗng, mà lúc đó mọi ô sẽ dồn
        /// hết về (0,0) — bản đồ sai mà nhìn gizmo cũng không ra.
        /// </summary>
        private ShopGrid Grid
        {
            get
            {
                if (ShopGrid.Instance != null) return ShopGrid.Instance;
                if (_cachedGrid == null) _cachedGrid = FindFirstObjectByType<ShopGrid>();
                return _cachedGrid;
            }
        }

        /// <summary>
        /// Điểm neo ô gốc. Ưu tiên DecorSlot cha vì art thường được kê lệch tâm ô vài chục cm cho
        /// đẹp mắt — lấy theo art thì món nhảy sang ô bên cạnh, bản đồ ô sai một ô so với chỗ
        /// người thiết kế đặt.
        /// </summary>
        public Transform AnchorTransform
        {
            get
            {
                if (_anchorOverride != null) return _anchorOverride;
                if (_cachedAnchor == null)
                {
                    var slot = GetComponentInParent<DecorSlot>();
                    _cachedAnchor = slot != null ? slot.transform : transform;
                }
                return _cachedAnchor;
            }
        }

        /// <summary>Ô gốc của món.</summary>
        public Vector3Int AnchorCell
        {
            get
            {
                var grid = Grid;
                // Lấy ô GẦN NHẤT chứ không phải ô chứa điểm: gốc món hay nằm sát mép ô, lệch vài
                // phần trăm là WorldToCell nhảy sang ô bên cạnh, cả vết chân trôi theo một ô.
                return grid != null ? grid.NearestCell(AnchorTransform.position) : Vector3Int.zero;
            }
        }

        public IReadOnlyList<Vector2Int> BlockedCells => _blockedCells;

        /// <summary>Số chỗ ngồi món này cung cấp (bàn ghế thì > 0, tủ lạnh thì 0).</summary>
        public int SeatCount => _seats != null ? _seats.Length : 0;

        /// <summary>
        /// Nút chứa hình vẽ. Tách riêng khỏi gốc món để **ô lưới và hình vẽ không dính vào nhau**:
        /// gốc món luôn nằm đúng tâm ô nó đứng, còn hình muốn nhích lên xuống bao nhiêu là việc của
        /// <see cref="_artOffset"/>. Trước đây độ lệch này nằm rải trong localPosition của từng
        /// sprite con, nên công cụ nào chạm vào là hình tự dịch mà không ai truy ra được.
        /// </summary>
        public Transform ArtRoot
        {
            get
            {
                if (_artRoot == null) _artRoot = transform.Find("Art");
                return _artRoot;
            }
        }

        /// <summary>Độ lệch hình vẽ so với ô sàn.</summary>
        public Vector2 ArtOffset
        {
            get => _artOffset;
            set { _artOffset = value; ApplyArtOffset(); }
        }

        /// <summary>Đẩy <see cref="_artOffset"/> xuống transform của nút hình.</summary>
        public void ApplyArtOffset()
        {
            var art = ArtRoot;
            if (art == null) return;

            var local = art.localPosition;
            var target = new Vector3(_artOffset.x, _artOffset.y, local.z);
            if (local != target) art.localPosition = target;
        }

        private void Awake()
        {
            EnsureSeatFlags();
            ApplyArtOffset();
        }

        private void OnEnable()
        {
            EnsureSeatFlags();
            ApplyArtOffset();
            if (Grid != null) Grid.Register(this);
        }

#if UNITY_EDITOR
        private void OnValidate() => ApplyArtOffset();
#endif

        private void OnDisable()
        {
            if (Grid != null) Grid.Unregister(this);
        }

        private void EnsureSeatFlags()
        {
            if (_seatTaken == null || _seatTaken.Length != SeatCount) _seatTaken = new bool[SeatCount];
        }

        // =====================================================================
        // Ô TRÊN LƯỚI
        // =====================================================================

        /// <summary>Liệt kê các ô thân món chiếm, đã quy về toạ độ ô tuyệt đối.</summary>
        public IEnumerable<Vector3Int> EnumerateBlockedCells()
        {
            var anchor = AnchorCell;
            if (_blockedCells == null || _blockedCells.Length == 0)
            {
                yield return anchor;
                yield break;
            }

            foreach (var offset in _blockedCells)
            {
                yield return new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, anchor.z);
            }
        }

        /// <summary>
        /// Ô của một chỗ ngồi, còn ở dạng **lệch so với ô gốc**. Dùng khi cần biết món này sẽ chiếm
        /// những ô nào nếu đặt ở một chỗ khác — ví dụ lúc xem trước vết chân trong công cụ xếp slot,
        /// khi món còn nằm trong prefab chứ chưa có mặt trong scene.
        /// </summary>
        public Vector2Int SeatCellOffset(int index) => _seats[index].cell;

        /// <summary>Ô tuyệt đối của một chỗ ngồi.</summary>
        public Vector3Int SeatCell(int index)
        {
            var anchor = AnchorCell;
            var offset = _seats[index].cell;
            return new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, anchor.z);
        }

        /// <summary>Tâm ô của một chỗ ngồi — dùng cho tìm đường và bản đồ ô.</summary>
        public Vector3 SeatWorldPosition(int index) => CellCenterFor(_seats[index].cell);

        /// <summary>
        /// Điểm khách ngồi thật sự. Mặc định là tâm ô, nhưng nếu chỗ ngồi có khai
        /// <see cref="SeatSlot.sitOffset"/> thì lấy theo đó để khách ngồi khít vào mặt ghế trong art.
        /// </summary>
        public Vector3 SeatSitPosition(int index)
        {
            var seat = _seats[index];
            Vector2 total = seat.sitOffset + seat.sitAdjust;

            // Không khai gì thì ngồi giữa ô; khai rồi thì ngồi đúng chỗ ghế (+ phần chỉnh tay).
            if (seat.sitOffset == Vector2.zero && seat.sitAdjust == Vector2.zero) return SeatWorldPosition(index);

            Vector3 anchor = AnchorTransform.position;
            return new Vector3(anchor.x + total.x, anchor.y + total.y, 0f);
        }

        /// <summary>
        /// Lớp vẽ cho khách khi ngồi vào chỗ này: **trên cái ghế, dưới cái bàn**. Không có cái này
        /// thì khách bị ghế che mất (ghế chân thấp hơn nên order cao hơn), nhìn như ngồi chui
        /// xuống dưới ghế.
        /// </summary>
        public int SeatedSortingOrder(int index)
        {
            var seat = _seats[index];
            int chairOrder = seat.chairArt != null ? seat.chairArt.sortingOrder : int.MinValue;

            // Mảnh thân thấp nhất (mặt bàn) là trần trên: khách phải nằm dưới nó.
            int bodyOrder = int.MaxValue;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || renderer == seat.chairArt) continue;

                bool isSeatArt = false;
                for (int i = 0; i < SeatCount; i++)
                {
                    if (_seats[i].chairArt == renderer) { isSeatArt = true; break; }
                }
                if (isSeatArt) continue;

                bodyOrder = Mathf.Min(bodyOrder, renderer.sortingOrder);
            }

            if (chairOrder == int.MinValue) return bodyOrder == int.MaxValue ? 0 : bodyOrder - 1;
            if (bodyOrder == int.MaxValue) return chairOrder + 1;

            // Chen vào khoảng giữa ghế và bàn; khít quá thì lấy sát trên ghế.
            return bodyOrder - chairOrder > 1 ? (chairOrder + bodyOrder) / 2 : chairOrder + 1;
        }

        // =====================================================================
        // GIỮ CHỖ
        // =====================================================================

        /// <summary>Còn ghế trống không.</summary>
        public bool HasFreeSeat()
        {
            EnsureSeatFlags();
            for (int i = 0; i < _seatTaken.Length; i++)
            {
                if (!_seatTaken[i]) return true;
            }
            return false;
        }

        /// <summary>Đã có ai ngồi ở món này chưa — để ưu tiên xếp khách vào bàn còn trống hẳn.</summary>
        public bool HasOccupiedSeat()
        {
            EnsureSeatFlags();
            for (int i = 0; i < _seatTaken.Length; i++)
            {
                if (_seatTaken[i]) return true;
            }
            return false;
        }

        /// <summary>Giữ một ghế trống. Trả về false nếu hết chỗ.</summary>
        public bool TryReserveSeat(out int index)
        {
            EnsureSeatFlags();
            for (int i = 0; i < _seatTaken.Length; i++)
            {
                if (_seatTaken[i]) continue;

                _seatTaken[i] = true;
                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>Trả ghế lại cho quán.</summary>
        public void ReleaseSeat(int index)
        {
            EnsureSeatFlags();
            if (index >= 0 && index < _seatTaken.Length) _seatTaken[index] = false;
        }

        /// <summary>Trả hết ghế (gọi khi dỡ món hoặc reset ván chơi).</summary>
        public void ReleaseAllSeats()
        {
            EnsureSeatFlags();
            for (int i = 0; i < _seatTaken.Length; i++) _seatTaken[i] = false;
        }

        // =====================================================================
        // GIZMOS
        // =====================================================================

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            var blocked = _blockedCells != null && _blockedCells.Length > 0
                ? _blockedCells
                : new[] { Vector2Int.zero };

            foreach (var offset in blocked)
            {
                DrawCell(offset, new Color(0.95f, 0.35f, 0.25f, 0.95f));
            }

            for (int i = 0; i < SeatCount; i++)
            {
                DrawCell(_seats[i].cell, new Color(0.3f, 0.85f, 1f, 0.95f));

                // Điểm ngồi: chấm vàng + gạch nối về tâm ô, để nhích sitAdjust mà thấy ngay.
                Vector3 sit = SeatSitPosition(i);
                Gizmos.color = new Color(1f, 0.85f, 0.15f, 1f);
                Gizmos.DrawWireSphere(sit, 0.07f);
                Gizmos.DrawLine(CellCenterFor(_seats[i].cell), sit);
            }
        }

        /// <summary>
        /// Tâm của một ô, tính từ độ lệch so với ô gốc.
        ///
        /// Mở prefab ra sửa thì cố ý KHÔNG hỏi lưới của scene: gốc prefab nằm ở (0,0), mà điểm đó
        /// rơi đúng vào mép giữa hai ô của sàn — <c>WorldToCell</c> trả về ô có tâm cao hơn gốc
        /// 0.25, và chỉ cần nhích gốc xuống 2cm là nhảy hẳn sang ô bên dưới. Nhìn trong Prefab Mode
        /// thì hình thoi lệch hẳn một ô so với art. Ở đó vẽ quanh chính gốc món theo hình học ô là
        /// đúng ý nghĩa của dữ liệu, còn trong scene thì vẫn bám lưới thật để khớp bản đồ đường đi.
        /// </summary>
        private Vector3 CellCenterFor(Vector2Int offset)
        {
            var grid = Grid;
            bool sameScene = grid != null && grid.gameObject.scene == gameObject.scene;

            if (!sameScene)
            {
                Vector2 size = CellSize(grid);
                return AnchorTransform.position
                       + new Vector3(size.x * 0.5f * (offset.x - offset.y), size.y * 0.5f * (offset.x + offset.y), 0f);
            }

            var anchor = AnchorCell;
            return grid.CellCenter(new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, anchor.z));
        }

        private static Vector2 CellSize(ShopGrid grid) =>
            grid != null ? new Vector2(grid.CellHalfExtents.x * 2f, grid.CellHalfExtents.y * 2f) : new Vector2(1f, 0.5f);

        private void DrawCell(Vector2Int offset, Color color)
        {
            Gizmos.color = color;
            Vector3 c = CellCenterFor(offset);
            Vector2 size = CellSize(Grid);

            Vector3 left = c + Vector3.left * (size.x * 0.5f);
            Vector3 right = c + Vector3.right * (size.x * 0.5f);
            Vector3 up = c + Vector3.up * (size.y * 0.5f);
            Vector3 down = c + Vector3.down * (size.y * 0.5f);

            Gizmos.DrawLine(left, up);
            Gizmos.DrawLine(up, right);
            Gizmos.DrawLine(right, down);
            Gizmos.DrawLine(down, left);
        }
    }
}
