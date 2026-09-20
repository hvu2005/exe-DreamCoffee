using System.Text;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Đổi vùng chạm của nội thất từ một khung chữ nhật bao cả món sang
    /// <see cref="GridCellCollider"/> — đúng các ô món chiếm trên sàn.
    ///
    /// Khung cũ được đặt vừa cái ẢNH, mà ảnh trong phối cảnh 2.5D cao hơn phần nằm trên sàn rất
    /// nhiều. Chênh lệch có món lên tới hàng chục lần: chậu Monstera đang đeo khung 10.03 x 12.94,
    /// tức rộng 20 ô và cao 25 ô, trong khi nó chỉ đứng trên 1 ô.
    ///
    /// Menu: DreamCafe > Decor > Xem hitbox hiện tại | Đổi hitbox sang theo ô
    /// </summary>
    public static class DecorHitboxTool
    {
        private const string DecorPrefabDir = "Assets/_Game/Prefabs/Decor";

        [MenuItem("DreamCafe/Decor/Xem hitbox hiện tại", priority = 40)]
        private static void Report()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            Vector2 cell = grid != null ? new Vector2(grid.cellSize.x, grid.cellSize.y) : new Vector2(1f, 0.5f);

            var sb = new StringBuilder("[DecorHitbox] Vùng chạm của từng món (một ô = ")
                .Append(cell.x).Append(" x ").Append(cell.y).AppendLine("):\n");

            foreach (string path in PrefabPaths())
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                var occupant = go.GetComponent<GridOccupant>();
                var box = go.GetComponent<BoxCollider2D>();
                var poly = go.GetComponent<PolygonCollider2D>();

                if (poly != null && go.GetComponent<GridCellCollider>() != null)
                {
                    sb.AppendLine($"  {name,-30} theo ô ({poly.pathCount} hình thoi)");
                    continue;
                }

                if (box == null)
                {
                    sb.AppendLine($"  {name,-30} chưa có collider");
                    continue;
                }

                int cells = occupant != null ? CountCells(occupant) : 1;
                float ratio = cell.x * cell.y > 0f ? box.size.x * box.size.y / (cell.x * cell.y) : 0f;
                sb.AppendLine($"  {name,-30} khung {box.size.x:0.##} x {box.size.y:0.##}" +
                              $"   ~{ratio:0.#} ô diện tích, món chỉ chiếm {cells} ô");
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem("DreamCafe/Decor/Đổi hitbox sang theo ô", priority = 41)]
        private static void Convert()
        {
            if (!EditorUtility.DisplayDialog("Đổi hitbox sang theo ô",
                    "Gỡ BoxCollider2D của các món có GridOccupant và thay bằng vùng chạm theo ô.\n\n" +
                    "Món KHÔNG có GridOccupant (đồ treo tường, slot mẫu) giữ nguyên.",
                    "Đổi", "Thôi"))
            {
                return;
            }

            var sb = new StringBuilder("[DecorHitbox] Kết quả:\n");
            int changed = 0;

            foreach (string path in PrefabPaths())
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (asset == null || asset.GetComponent<GridOccupant>() == null)
                {
                    sb.AppendLine($"  {name,-30} bỏ qua (không có GridOccupant)");
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var box = root.GetComponent<BoxCollider2D>();
                    string before = box != null ? $"{box.size.x:0.##} x {box.size.y:0.##}" : "không có";
                    if (box != null) Object.DestroyImmediate(box);

                    var poly = root.GetComponent<PolygonCollider2D>();
                    if (poly == null) poly = root.AddComponent<PolygonCollider2D>();

                    var builder = root.GetComponent<GridCellCollider>();
                    if (builder == null) builder = root.AddComponent<GridCellCollider>();
                    builder.Rebuild();

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                    sb.AppendLine($"  {name,-30} khung {before}  ->  {poly.pathCount} ô");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            sb.AppendLine($"\n  Đã đổi {changed} món.");
            Debug.Log(sb.ToString());
        }

        private static string[] PrefabPaths()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { DecorPrefabDir });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            System.Array.Sort(paths, System.StringComparer.Ordinal);
            return paths;
        }

        private static int CountCells(GridOccupant occupant)
        {
            var blocked = occupant.BlockedCells;
            int body = blocked != null && blocked.Count > 0 ? blocked.Count : 1;
            return body + occupant.SeatCount;
        }
    }
}
