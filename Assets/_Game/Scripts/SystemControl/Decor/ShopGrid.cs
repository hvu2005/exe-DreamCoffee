using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>Trạng thái một ô gạch trong quán.</summary>
    public enum ShopCellKind
    {
        /// <summary>Ô trống, đi lại thoải mái.</summary>
        Free = 0,

        /// <summary>Thân đồ nội thất (mặt bàn, quầy, tủ) — khách phải đi vòng.</summary>
        Furniture = 1,

        /// <summary>Ghế ngồi — vẫn đi vào được (khách phải tới được mới ngồi), nhưng đã có chủ.</summary>
        Seat = 2
    }

    /// <summary>
    /// Bản đồ ô gạch của quán: quy mọi món nội thất về đúng các ô của <see cref="Grid"/> sàn
    /// (isometric, 1 ô gạch = 1 x 0.5 đơn vị thế giới) theo quy ước **một món = một ô**:
    /// thân bàn chiếm ô của nó, mỗi ghế chiếm thêm một ô.
    ///
    /// Ô <see cref="ShopCellKind.Furniture"/> được dựng <see cref="NavMeshObstacle"/> có carving, nên
    /// NavMesh tự khoét lỗ ngay lúc chạy — khách đi vòng qua bàn thay vì xuyên qua, và không phải
    /// bake lại mỗi lần người chơi kê lại đồ. Ô ghế thì để đi được, nếu không khách không tới chỗ
    /// ngồi được.
    /// </summary>
    public sealed class ShopGrid : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField, Tooltip("Grid của tilemap sàn. Bỏ trống thì tự tìm trong scene.")]
        private Grid _grid;

        [SerializeField, Tooltip("Tilemap sàn — ô nào có gạch mới là chỗ đi được. Bỏ trống thì tìm tilemap tên 'Ground'.")]
        private Tilemap _floor;

        [Header("Chặn đường")]
        [SerializeField, Tooltip("Dựng NavMeshObstacle trên các ô nội thất. Mặc định TẮT — khách đã đi bằng A* trên lưới ô, bật chỉ tốn object và làm bẩn log.")]
        private bool _blockFurniture = false;

        [SerializeField, Range(0.3f, 1f), Tooltip("Vật cản nhỏ hơn ô một chút để khe giữa hai món không bị bịt kín.")]
        private float _obstacleFill = 0.85f;

        [Header("Debug")]
        [SerializeField, Tooltip("Vẽ ô bị chiếm trong Scene view: đỏ = nội thất, xanh = ghế.")]
        private bool _drawGizmos = true;

        private readonly Dictionary<Vector3Int, ShopCellKind> _cells = new();
        private readonly List<GridOccupant> _occupants = new();
        private const string ObstaclePrefix = "Block_";
        private readonly List<GameObject> _obstacles = new();

        public static ShopGrid Instance { get; private set; }

        /// <summary>Toàn bộ ô đang bị chiếm và loại của chúng.</summary>
        public IReadOnlyDictionary<Vector3Int, ShopCellKind> Cells => _cells;

        /// <summary>Các món nội thất đang có mặt trên lưới (mỗi món là một <see cref="GridOccupant"/>).</summary>
        public IReadOnlyList<GridOccupant> Occupants => _occupants;

        /// <summary>Nửa kích thước một ô, dùng để vẽ hình thoi gizmo.</summary>
        public Vector3 CellHalfExtents
        {
            get
            {
                EnsureGrid();
                return _grid != null
                    ? new Vector3(_grid.cellSize.x * 0.5f, _grid.cellSize.y * 0.5f, 0f)
                    : new Vector3(0.5f, 0.25f, 0f);
            }
        }

        private void Awake()
        {
            Instance = this;
            EnsureGrid();
        }

        private void OnEnable()
        {
            Instance = this;
            // Món nào bật trước ShopGrid thì chưa kịp đăng ký — quét lại một lượt cho chắc.
            CollectOccupants();
        }

        /// <summary>Món nội thất tự gọi khi xuất hiện trên sân.</summary>
        public void Register(GridOccupant occupant)
        {
            if (occupant == null || _occupants.Contains(occupant)) return;

            // Chỉ nhận món nằm cùng scene với lưới. Mở một prefab ra sửa (Prefab Mode, hoặc
            // LoadPrefabContents trong script editor) cũng chạy OnEnable của nó, và nếu không chặn
            // thì bản prefab đó tự nhảy vào bản đồ ô như một món có thật, đứng ở gốc toạ độ.
            if (occupant.gameObject.scene != gameObject.scene) return;

            _occupants.Add(occupant);
            RebuildCells();
        }

        /// <summary>Món nội thất tự gọi khi bị dỡ đi.</summary>
        public void Unregister(GridOccupant occupant)
        {
            if (occupant == null) return;
            if (_occupants.Remove(occupant)) RebuildCells();
        }

        private void CollectOccupants()
        {
            _occupants.Clear();
            foreach (var occupant in FindObjectsByType<GridOccupant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                _occupants.Add(occupant);
            }
            RebuildCells();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureGrid()
        {
            if (_grid == null) _grid = FindFirstObjectByType<Grid>();

            if (_floor == null)
            {
                foreach (var tilemap in FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (tilemap.name == "Ground") { _floor = tilemap; break; }
                }
            }
        }

        /// <summary>
        /// Ô có gạch sàn hay không. Thiếu kiểm tra này thì đường đi chạy thẳng ra ngoài quán —
        /// bản đồ ô chỉ ghi chỗ BỊ CHIẾM, còn lại mặc định là trống, kể cả khoảng không ngoài sàn.
        /// </summary>
        public bool IsFloor(Vector3Int cell)
        {
            EnsureGrid();
            return _floor != null && _floor.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }

        // =====================================================================
        // ĐỔI QUA LẠI GIỮA TOẠ ĐỘ THẾ GIỚI VÀ Ô
        // =====================================================================

        /// <summary>Ô gạch chứa một điểm trong thế giới.</summary>
        public Vector3Int WorldToCell(Vector3 world)
        {
            EnsureGrid();
            return _grid != null ? _grid.WorldToCell(new Vector3(world.x, world.y, 0f)) : Vector3Int.zero;
        }

        /// <summary>
        /// Tâm của một ô gạch, đã ép về mặt phẳng z = 0.
        ///
        /// Cố ý hỏi TILEMAP chứ không dùng <c>Grid.GetCellCenterWorld</c>: với lưới isometric kiểu
        /// ZAsY, hàm của Grid cộng thêm cả nửa ô theo trục z rồi quy ngược sang y, nên tâm nó trả về
        /// nằm cao hơn viên gạch thật 0.125 đơn vị. Lệch chừng đó là mọi thứ dựa trên ô — chỗ ngồi,
        /// điểm rẽ của đường đi, khung gizmo — đều trôi lên khỏi sàn một phần tư ô.
        /// </summary>
        public Vector3 CellCenter(Vector3Int cell)
        {
            EnsureGrid();
            if (_floor != null)
            {
                var t = _floor.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                return new Vector3(t.x, t.y, 0f);
            }

            if (_grid == null) return Vector3.zero;

            // Không có tilemap thì tự cộng lấy nửa ô, vẫn tránh được phần z thừa của Grid.
            var c = _grid.CellToWorld(cell);
            return new Vector3(c.x, c.y + _grid.cellSize.y * 0.5f, 0f);
        }

        /// <summary>
        /// Ô gần một điểm nhất. Khác <see cref="WorldToCell"/> ở chỗ điểm rơi đúng mép ô thì vẫn
        /// cho kết quả ổn định: sát mép chỉ cần lệch vài phần trăm là WorldToCell nhảy hẳn sang ô
        /// bên cạnh, mà đồ nội thất lại hay được kê đúng mép.
        /// </summary>
        public Vector3Int NearestCell(Vector3 world)
        {
            EnsureGrid();

            var start = WorldToCell(world);
            Vector3 half = CellHalfExtents;
            if (half.x <= 0f || half.y <= 0f) return start;

            var best = start;
            float bestDistance = float.MaxValue;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var cell = new Vector3Int(start.x + dx, start.y + dy, start.z);
                Vector3 center = CellCenter(cell);
                float distance = Mathf.Abs(center.x - world.x) / half.x + Mathf.Abs(center.y - world.y) / half.y;
                if (distance < bestDistance) { bestDistance = distance; best = cell; }
            }

            return best;
        }

        /// <summary>Đưa một điểm bất kỳ về đúng tâm ô gần nhất.</summary>
        public Vector3 SnapToCell(Vector3 world) => CellCenter(NearestCell(world));

        // =====================================================================
        // TRA CỨU
        // =====================================================================

        public ShopCellKind GetKind(Vector3Int cell) =>
            _cells.TryGetValue(cell, out var kind) ? kind : ShopCellKind.Free;

        /// <summary>Ô có đi qua được không: phải có gạch sàn và không phải thân nội thất.</summary>
        public bool IsWalkable(Vector3Int cell) => IsFloor(cell) && GetKind(cell) != ShopCellKind.Furniture;

        /// <summary>Tiện cho gameplay: điểm này có đứng/đi được không.</summary>
        public bool IsWalkable(Vector3 world) => IsWalkable(WorldToCell(world));

        // =====================================================================
        // DỰNG LẠI BẢN ĐỒ
        // =====================================================================

        /// <summary>Quét lại toàn bộ món nội thất trong scene rồi dựng lại bản đồ ô.</summary>
        public void RebuildFromScene() => CollectOccupants();

        /// <summary>
        /// Dựng lại bản đồ ô từ danh sách món đang có mặt. Mỗi món tự khai nó chiếm ô nào qua
        /// <see cref="GridOccupant"/>, nên thêm/bớt đồ là bản đồ theo kịp ngay.
        /// </summary>
        public void RebuildCells()
        {
            _cells.Clear();
            ClearObstacles();

            for (int i = _occupants.Count - 1; i >= 0; i--)
            {
                var occupant = _occupants[i];
                if (occupant == null) { _occupants.RemoveAt(i); continue; }

                foreach (var cell in occupant.EnumerateBlockedCells())
                {
                    Occupy(cell, ShopCellKind.Furniture, occupant.name);
                }

                for (int seat = 0; seat < occupant.SeatCount; seat++)
                {
                    Occupy(occupant.SeatCell(seat), ShopCellKind.Seat, occupant.name + " (ghế " + seat + ")");
                }
            }

            // Vật cản NavMesh chỉ dựng lúc chạy: khách đã đi bằng A* trên lưới ô nên không cần tới
            // chúng, và dựng trong Edit mode thì mỗi lần xem gizmo lại đẻ ra một đống object rác.
            if (_blockFurniture && Application.isPlaying) BuildObstacles();
        }

        /// <summary>
        /// Ghi loại cho một ô. Ghế được ưu tiên đè lên thân nội thất: ô nào vừa là bàn vừa là ghế
        /// mà đánh thành vật cản thì khách không bao giờ ngồi xuống được.
        /// </summary>
        private void Occupy(Vector3Int cell, ShopCellKind kind, string owner)
        {
            if (_cells.TryGetValue(cell, out var existing))
            {
                if (existing == ShopCellKind.Seat || kind == ShopCellKind.Seat)
                {
                    _cells[cell] = ShopCellKind.Seat;
                    return;
                }

                Debug.LogWarning($"[ShopGrid] Ô {cell} bị hai món chồng nhau ({owner}). Kê lại cho khỏi đè ô.");
                return;
            }

            _cells[cell] = kind;
        }

        // =====================================================================
        // VẬT CẢN NAVMESH
        // =====================================================================

        /// <summary>
        /// Dọn vật cản cũ. Quét theo tên con thay vì chỉ theo danh sách trong bộ nhớ: danh sách mất
        /// sau domain reload (sửa script lúc đang Play) hoặc khi Rebuild bị gọi chồng nhau, và khi đó
        /// vật cản cũ nằm lại vĩnh viễn, chặn đường ở chỗ đã dọn đồ.
        /// </summary>
        private void ClearObstacles()
        {
            _obstacles.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (!child.name.StartsWith(ObstaclePrefix)) continue;

                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);

                // Destroy() chỉ huỷ ở cuối frame — tách ra khỏi cây ngay để lần dựng kế tiếp
                // trong cùng frame không đếm nhầm nó là vật cản còn sống. Bỏ qua lúc chính mình
                // đang bị bật/tắt (thoát Play chẳng hạn): Unity cấm đổi cha giữa chừng và chỉ đổ
                // cảnh báo, mà lúc đó object cũng sắp bị huỷ cùng scene rồi.
                if (gameObject.activeInHierarchy) child.SetParent(null, true);
            }
        }

        private void BuildObstacles()
        {
            EnsureGrid();
            if (_grid == null) return;

            Vector3 cellSize = _grid.cellSize;

            foreach (var pair in _cells)
            {
                if (pair.Value != ShopCellKind.Furniture) continue;

                var go = new GameObject($"{ObstaclePrefix}{pair.Key.x}_{pair.Key.y}");
                go.transform.SetParent(transform, false);
                go.transform.position = CellCenter(pair.Key);

                var obstacle = go.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                // NavMesh nằm trên mặt phẳng XY nên hộp phải dày theo trục Z mới cắt trúng nó.
                obstacle.size = new Vector3(cellSize.x * _obstacleFill, cellSize.y * _obstacleFill, 2f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                _obstacles.Add(go);
            }
        }

        // =====================================================================
        // GIZMOS
        // =====================================================================

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _cells.Count == 0) return;
            EnsureGrid();
            if (_grid == null) return;

            Vector3 half = new(_grid.cellSize.x * 0.5f, _grid.cellSize.y * 0.5f, 0f);

            foreach (var pair in _cells)
            {
                Gizmos.color = pair.Value == ShopCellKind.Furniture
                    ? new Color(0.9f, 0.25f, 0.25f, 0.9f)
                    : new Color(0.25f, 0.8f, 0.9f, 0.9f);

                // Ô isometric là hình thoi: nối 4 đỉnh trái/phải/trên/dưới quanh tâm ô.
                Vector3 c = CellCenter(pair.Key);
                Vector3 left = c + Vector3.left * half.x;
                Vector3 right = c + Vector3.right * half.x;
                Vector3 up = c + Vector3.up * half.y;
                Vector3 down = c + Vector3.down * half.y;

                Gizmos.DrawLine(left, up);
                Gizmos.DrawLine(up, right);
                Gizmos.DrawLine(right, down);
                Gizmos.DrawLine(down, left);
            }
        }
    }
}
