using System.Collections.Generic;
using DreamCafe.Core.Services;
using DreamCafe.SystemControl.Decor;
using UnityEngine;

namespace DreamCafe.SystemControl.Navigation
{
    /// <summary>
    /// Bản cài đặt <see cref="IPathfindingService"/> chạy A* trên lưới ô gạch của quán
    /// (<see cref="ShopGrid"/>). Service chỉ lo phần "hỏi ở đâu, trả lời thế nào"; thuật toán A*
    /// nằm riêng trong <see cref="ShopGridPathfinder"/> nên đổi thuật toán không đụng tới hợp đồng.
    /// </summary>
    public sealed class GridPathfindingService : IPathfindingService
    {
        private ShopGrid _grid;

        public GridPathfindingService(ShopGrid grid = null)
        {
            _grid = grid;
        }

        public void Init(ServiceContext ctx)
        {
            EnsureGrid();
            Debug.Log(_grid != null
                ? "[GridPathfindingService] Sẵn sàng — tìm đường A* trên lưới ô của quán."
                : "[GridPathfindingService] Không tìm thấy ShopGrid trong scene, khách sẽ đi thẳng tới đích.");
        }

        public void Shutdown() => _grid = null;

        public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> waypoints)
        {
            waypoints.Clear();
            EnsureGrid();

            if (_grid == null)
            {
                // Không có bản đồ ô (scene test chẳng hạn) — đi thẳng, còn hơn đứng im.
                waypoints.Add(to);
                return true;
            }

            return ShopGridPathfinder.TryFindWorldPath(_grid, from, to, waypoints);
        }

        public bool IsWalkable(Vector3 world)
        {
            EnsureGrid();
            return _grid == null || _grid.IsWalkable(world);
        }

        public bool TrySnapToWalkable(Vector3 world, out Vector3 result)
        {
            result = world;
            EnsureGrid();
            if (_grid == null) return true;

            if (!ShopGridPathfinder.TryFindNearestWalkable(_grid, _grid.WorldToCell(world), out var cell)) return false;

            result = _grid.CellCenter(cell);
            return true;
        }

        private void EnsureGrid()
        {
            if (_grid == null) _grid = ShopGrid.Instance;
        }
    }
}
