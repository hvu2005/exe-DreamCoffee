using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Dựng vùng chạm của một món nội thất **theo đúng các ô nó chiếm**, thay cho một khung chữ nhật
    /// bao cả món.
    ///
    /// Vì sao không dùng một BoxCollider2D: sprite nội thất trong phối cảnh 2.5D cao hơn phần nó
    /// thật sự nằm trên sàn rất nhiều (lưng ghế, thân tủ, cả cái cây). Khung bao vừa cái ảnh thì
    /// rộng gấp mấy lần chỗ món đứng — bấm vào khoảng trống cạnh nó cũng ăn, mà hai món kê gần nhau
    /// thì khung chồng lên nhau nên bấm trúng cái sau lại chọn phải cái trước.
    ///
    /// Một <see cref="PolygonCollider2D"/> chứa được nhiều đường bao rời nhau, nên mỗi ô là một hình
    /// thoi riêng — món chiếm 3 ô thì vùng chạm đúng 3 hình thoi đó, không dính phần khoảng không
    /// giữa chúng.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class GridCellCollider : MonoBehaviour
    {
        [SerializeField, Tooltip("Món cần lấy danh sách ô. Bỏ trống thì tìm trên chính nó hoặc cha.")]
        private GridOccupant _occupant;

        [SerializeField, Tooltip("Tính cả ô ghế. Nên bật: hình cái ghế nằm ở đó, người chơi bấm vào ghế " +
                                 "là đang muốn chọn cả bộ bàn ghế.")]
        private bool _includeSeats = true;

        [SerializeField, Range(0.5f, 1f), Tooltip("Thu hình thoi nhỏ lại một chút để vùng chạm của hai " +
                                                  "món kê sát nhau không chạm mép nhau.")]
        private float _fill = 0.96f;

        private static readonly Vector2 FallbackCellSize = new(1f, 0.5f);

        /// <summary>Món đang bám theo. Tìm lên cha vì collider hay nằm ở nút gốc của prefab.</summary>
        public GridOccupant Occupant
        {
            get
            {
                if (_occupant == null) _occupant = GetComponentInParent<GridOccupant>();
                return _occupant;
            }
        }

        private void OnEnable() => Rebuild();

        /// <summary>Dựng lại đường bao từ danh sách ô hiện tại.</summary>
        public void Rebuild()
        {
            var collider2d = GetComponent<PolygonCollider2D>();
            if (collider2d == null) return;

            var occupant = Occupant;
            if (occupant == null)
            {
                collider2d.pathCount = 0;
                return;
            }

            var cells = CollectCells(occupant);
            Transform anchor = occupant.AnchorTransform;
            if (anchor == null) anchor = occupant.transform;

            Vector2 size = ResolveCellSize();
            float hx = size.x * 0.5f * _fill;
            float hy = size.y * 0.5f * _fill;

            collider2d.pathCount = cells.Count;

            var points = new Vector2[4];
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                Vector3 centre = anchor.position
                                 + new Vector3(size.x * 0.5f * (cell.x - cell.y),
                                               size.y * 0.5f * (cell.x + cell.y), 0f);

                // Đổi từng đỉnh qua hệ toạ độ của collider chứ không đổi tâm rồi cộng nửa ô: nút này
                // có thể đang bị phóng to/thu nhỏ, cộng thẳng là hình thoi méo theo.
                points[0] = transform.InverseTransformPoint(centre + new Vector3(-hx, 0f, 0f));
                points[1] = transform.InverseTransformPoint(centre + new Vector3(0f, hy, 0f));
                points[2] = transform.InverseTransformPoint(centre + new Vector3(hx, 0f, 0f));
                points[3] = transform.InverseTransformPoint(centre + new Vector3(0f, -hy, 0f));
                collider2d.SetPath(i, points);
            }
        }

        private List<Vector2Int> CollectCells(GridOccupant occupant)
        {
            var cells = new List<Vector2Int>();

            var blocked = occupant.BlockedCells;
            if (blocked != null)
            {
                for (int i = 0; i < blocked.Count; i++)
                {
                    if (!cells.Contains(blocked[i])) cells.Add(blocked[i]);
                }
            }

            if (_includeSeats)
            {
                for (int i = 0; i < occupant.SeatCount; i++)
                {
                    Vector2Int seat = occupant.SeatCellOffset(i);
                    if (!cells.Contains(seat)) cells.Add(seat);
                }
            }

            // Danh sách rỗng nghĩa là món chỉ chiếm đúng ô gốc — vẫn phải có chỗ mà bấm.
            if (cells.Count == 0) cells.Add(Vector2Int.zero);
            return cells;
        }

        private static Vector2 ResolveCellSize()
        {
            var grid = FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            return grid != null ? new Vector2(grid.cellSize.x, grid.cellSize.y) : FallbackCellSize;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Unity cấm đụng vào component khác ngay trong OnValidate; hoãn một nhịp.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                Rebuild();
            };
        }

        private void OnDrawGizmosSelected()
        {
            var collider2d = GetComponent<PolygonCollider2D>();
            if (collider2d == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0.15f, 0.9f);
            for (int path = 0; path < collider2d.pathCount; path++)
            {
                var points = collider2d.GetPath(path);
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 a = transform.TransformPoint(points[i]);
                    Vector3 b = transform.TransformPoint(points[(i + 1) % points.Length]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
#endif
    }
}
