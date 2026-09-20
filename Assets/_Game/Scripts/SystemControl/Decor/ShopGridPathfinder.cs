using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Tìm đường A* trên lưới ô của quán (<see cref="ShopGrid"/>). Đi được = ô có gạch sàn và không
    /// phải thân nội thất, nên khách tự né bàn ghế mà không cần NavMesh — vốn không tả nổi lưới
    /// isometric ở độ phân giải một ô (hai ô kề nhau chỉ cách 0.25 đơn vị theo trục y).
    ///
    /// Vật cản ở đây có hai loại khác hẳn nhau: nội thất chiếm cả một Ô, còn vách tường đứng trên
    /// CẠNH giữa hai ô. Vách không làm ô nào mất chỗ đứng, nó chỉ cắt lối đi giữa hai ô — nên phải
    /// hỏi riêng bằng <see cref="ShopGrid.IsEdgeBlocked"/> chứ không quy về ô được.
    ///
    /// Lưới isometric: bốn ô kề cạnh (±1 theo x hoặc y) là bốn hướng chéo trên màn hình, còn bốn ô
    /// kề chéo trong toạ độ ô lại là trên/dưới/trái/phải trên màn hình. Dùng cả tám để khách đi
    /// mượt, nhưng cấm cắt góc xuyên qua khe giữa hai món đồ.
    /// </summary>
    public static class ShopGridPathfinder
    {
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;
        private const int MaxNodes = 4096;
        private const int MaxGoalRadius = 4;

        private static readonly Vector3Int[] StraightDirs =
        {
            new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0)
        };

        private static readonly Vector3Int[] DiagonalDirs =
        {
            new(1, 1, 0), new(1, -1, 0), new(-1, 1, 0), new(-1, -1, 0)
        };

        /// <summary>
        /// Tìm đường từ ô này sang ô kia. <paramref name="path"/> trả về gồm cả ô đích, không gồm ô
        /// xuất phát. Trả về false nếu bít đường hoàn toàn.
        ///
        /// Đích bị chiếm (ô ghế có bàn chắn, hoặc người chơi vừa kê đồ đè lên) thì nhận CẢ VÒNG ô
        /// đi được quanh nó làm đích — ô nào tới rẻ nhất thì dừng ở đó. Không chấm trước một ô rồi
        /// mới tìm đường tới đúng ô ấy: chấm trước là chấm bằng hình học, chưa biết khách đang đứng
        /// phía nào, nên khách đi ngang sau lưng ghế vẫn phải vòng tiếp sang ô bên hông đã chấm.
        /// </summary>
        public static bool TryFindPath(ShopGrid grid, Vector3Int start, Vector3Int goal, List<Vector3Int> path)
        {
            path.Clear();
            if (grid == null) return false;

            var goals = new List<Vector3Int>();
            if (!CollectGoals(grid, goal, goals)) return false;
            if (goals.Contains(start)) return true;

            var open = new List<Vector3Int> { start };
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };
            var fScore = new Dictionary<Vector3Int, int> { [start] = Heuristic(start, goals) };
            var closed = new HashSet<Vector3Int>();
            int visited = 0;

            while (open.Count > 0 && visited++ < MaxNodes)
            {
                // Lưới nhỏ (vài chục ô) nên quét tuyến tính rẻ hơn dựng priority queue.
                int bestIndex = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (Score(fScore, open[i]) < Score(fScore, open[bestIndex])) bestIndex = i;
                }

                Vector3Int current = open[bestIndex];
                if (goals.Contains(current))
                {
                    Reconstruct(cameFrom, current, start, path);
                    return true;
                }

                open.RemoveAt(bestIndex);
                closed.Add(current);

                for (int d = 0; d < 8; d++)
                {
                    bool diagonal = d >= 4;
                    Vector3Int dir = diagonal ? DiagonalDirs[d - 4] : StraightDirs[d];
                    Vector3Int next = current + dir;

                    if (closed.Contains(next) || !grid.IsWalkable(next)) continue;

                    if (!diagonal)
                    {
                        // Vách tường đứng trên CẠNH giữa hai ô: hai ô vẫn đứng được nhưng không
                        // bước qua nhau. Thiếu khúc này thì khách đi xuyên tường.
                        if (grid.IsEdgeBlocked(current, next)) continue;
                    }
                    else
                    {
                        // Đi chéo là lách qua điểm góc chung của bốn ô, nên phải hỏi cả hai đường
                        // vòng chữ L. Đòi CẢ HAI đều thông: vừa không lách qua khe chéo giữa hai
                        // món kê sát nhau, vừa không quệt qua đầu một bức vách. Chặt tay ở đây
                        // không làm mất đường đi — bốn hướng thẳng vẫn còn nguyên.
                        Vector3Int viaX = current + new Vector3Int(dir.x, 0, 0);
                        Vector3Int viaY = current + new Vector3Int(0, dir.y, 0);

                        if (!grid.IsWalkable(viaX) || !grid.IsWalkable(viaY)) continue;
                        if (grid.IsEdgeBlocked(current, viaX) || grid.IsEdgeBlocked(viaX, next)) continue;
                        if (grid.IsEdgeBlocked(current, viaY) || grid.IsEdgeBlocked(viaY, next)) continue;
                    }

                    int tentative = Score(gScore, current) + (diagonal ? DiagonalCost : StraightCost);
                    if (gScore.TryGetValue(next, out int known) && tentative >= known) continue;

                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    fScore[next] = tentative + Heuristic(next, goals);
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            return false;
        }

        /// <summary>Tìm đường theo toạ độ thế giới, trả về danh sách tâm ô để bám theo.</summary>
        public static bool TryFindWorldPath(ShopGrid grid, Vector3 from, Vector3 to, List<Vector3> waypoints)
        {
            waypoints.Clear();
            if (grid == null) return false;

            var cells = new List<Vector3Int>();
            if (!TryFindPath(grid, grid.WorldToCell(from), grid.WorldToCell(to), cells)) return false;

            for (int i = 0; i < cells.Count; i++) waypoints.Add(grid.CellCenter(cells[i]));
            return true;
        }

        /// <summary>Ô đi được gần nhất quanh một ô cho trước (quét lan dần ra ngoài).</summary>
        public static bool TryFindNearestWalkable(ShopGrid grid, Vector3Int origin, out Vector3Int result)
        {
            result = origin;
            if (grid == null) return false;
            if (grid.IsWalkable(origin)) return true;

            for (int radius = 1; radius <= MaxGoalRadius; radius++)
            {
                // Trong cùng một vòng, lấy ô gần tâm nhất. Quét theo thứ tự dx/dy trần thì ô chéo
                // (-1,-1) luôn ra trước dù có ô kề cạnh sát hơn.
                bool found = false;
                float bestDistance = float.MaxValue;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Chỉ xét viền của vòng hiện tại, bên trong đã quét ở vòng trước.
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

                        var candidate = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
                        if (!grid.IsWalkable(candidate)) continue;

                        float distance = dx * dx + dy * dy;
                        if (found && distance >= bestDistance) continue;

                        bestDistance = distance;
                        result = candidate;
                        found = true;
                    }
                }

                if (found) return true;
            }

            return false;
        }

        /// <summary>
        /// Tập ô được tính là "tới nơi". Đích đi được thì chỉ có chính nó. Đích bị chiếm thì lấy
        /// TOÀN BỘ ô đi được của vòng gần nhất còn chỗ trống — tám phía quanh cái ghế đều ngồi
        /// được, kể cả đứng sau lưng ghế; A* tự dừng ở ô nào rẻ nhất so với chỗ khách đang đứng.
        /// Chỉ khi cả vòng bị bịt mới nới ra vòng ngoài.
        /// </summary>
        private static bool CollectGoals(ShopGrid grid, Vector3Int goal, List<Vector3Int> goals)
        {
            goals.Clear();

            if (grid.IsWalkable(goal))
            {
                goals.Add(goal);
                return true;
            }

            for (int radius = 1; radius <= MaxGoalRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Chỉ xét viền của vòng hiện tại, bên trong đã quét ở vòng trước.
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

                        var candidate = new Vector3Int(goal.x + dx, goal.y + dy, goal.z);
                        if (grid.IsWalkable(candidate)) goals.Add(candidate);
                    }
                }

                if (goals.Count > 0) return true;
            }

            return false;
        }

        private static int Score(Dictionary<Vector3Int, int> map, Vector3Int cell) =>
            map.TryGetValue(cell, out int value) ? value : int.MaxValue / 2;

        /// <summary>
        /// Ước lượng quãng còn lại tới đích GẦN NHẤT trong tập. Phải lấy min chứ không ước lượng
        /// tới ô ghế ở giữa: ước lượng tới ô giữa thì cao hơn quãng thật tới ô vành, A* mất tính
        /// chấp nhận được và có thể chốt nhầm một ô vành đi tốn hơn.
        /// </summary>
        private static int Heuristic(Vector3Int cell, List<Vector3Int> goals)
        {
            int best = int.MaxValue;
            for (int i = 0; i < goals.Count; i++)
            {
                int h = Heuristic(cell, goals[i]);
                if (h < best) best = h;
            }
            return best == int.MaxValue ? 0 : best;
        }

        private static int Heuristic(Vector3Int a, Vector3Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            int diagonal = Mathf.Min(dx, dy);
            return DiagonalCost * diagonal + StraightCost * (dx + dy - 2 * diagonal);
        }

        private static void Reconstruct(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current,
            Vector3Int start, List<Vector3Int> path)
        {
            while (current != start)
            {
                path.Add(current);
                if (!cameFrom.TryGetValue(current, out current)) break;
            }
            path.Reverse();
        }
    }
}
