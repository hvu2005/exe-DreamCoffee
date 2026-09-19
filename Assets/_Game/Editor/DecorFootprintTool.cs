using System.Collections.Generic;
using System.Text;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Hỗ trợ khai báo <see cref="DecorItem.GridFootprint"/> — số ô gạch mỗi món nội thất chiếm trên
    /// sàn (ghế 1 ô, tủ bánh 2 ô, quầy bar dài 3 ô...).
    ///
    /// Không tự suy footprint từ ảnh: trong phối cảnh 2.5D, chiều cao sprite gộp cả phần dựng đứng
    /// (thân tủ, lưng ghế) lẫn phần nằm trên sàn, nên đoán theo ảnh là sai. Công cụ chỉ **gợi ý** bề
    /// ngang theo ảnh rồi để người thiết kế chốt.
    ///
    /// Menu: DreamCafe > Decor > Xem bảng footprint | Dựng lại bản đồ ô (xem gizmo)
    /// </summary>
    public static class DecorFootprintTool
    {
        private const string MenuReport = "DreamCafe/Decor/Xem bảng footprint";
        private const string MenuRebuild = "DreamCafe/Decor/Dựng lại bản đồ ô (xem gizmo)";

        [MenuItem(MenuReport, priority = 30)]
        private static void Report()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            float cellWidth = grid != null ? grid.cellSize.x : 1f;

            var sb = new StringBuilder("[DecorFootprint] Kích thước theo ô của từng món:\n");
            sb.AppendLine("  (gợi ý chỉ tính bề ngang ảnh / bề ngang ô — chiều sâu trên sàn phải tự chốt)\n");

            foreach (var guid in AssetDatabase.FindAssets("t:DecorItem"))
            {
                var item = AssetDatabase.LoadAssetAtPath<DecorItem>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;

                float spriteWidth = MeasureWidth(item);
                int suggested = Mathf.Max(1, Mathf.RoundToInt(spriteWidth / cellWidth));

                string flag = suggested != item.GridFootprint.x ? "   <-- lệch gợi ý" : string.Empty;
                sb.AppendLine($"  {item.Id,-26} đang khai báo {item.GridFootprint.x}x{item.GridFootprint.y}" +
                              $"   ảnh rộng {spriteWidth:0.00} (~{suggested} ô){flag}");
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Dựng lại bản đồ ô ngay trong Edit mode để gizmo của <see cref="ShopGrid"/> hiện đúng khối
        /// ô vừa khai báo — chỉnh footprint xong là thấy ngay, không cần bấm Play.
        /// </summary>
        [MenuItem(MenuRebuild, priority = 31)]
        private static void Rebuild()
        {
            var shopGrid = Object.FindFirstObjectByType<ShopGrid>(FindObjectsInactive.Include);
            if (shopGrid == null)
            {
                Debug.LogWarning("[DecorFootprint] Scene chưa có ShopGrid.");
                return;
            }

            shopGrid.RebuildFromScene();
            SceneView.RepaintAll();

            int furniture = 0, seats = 0;
            foreach (var pair in shopGrid.Cells)
            {
                if (pair.Value == ShopCellKind.Furniture) furniture++;
                else if (pair.Value == ShopCellKind.Seat) seats++;
            }

            Debug.Log($"[DecorFootprint] Bản đồ ô: {furniture} ô nội thất (đỏ), {seats} ô ghế (xanh). " +
                      "Xem gizmo trong Scene view.");
        }

        /// <summary>
        /// Đọc vị trí ghế trong art của prefab rồi ghi thành ô lệch vào <see cref="GridOccupant"/>.
        /// Làm bằng tay dễ sai: ghế của bàn tròn nằm chéo lên-trái và lên-phải chứ không đối xứng
        /// hai bên như nhìn thoáng qua. Chạy lại lệnh này mỗi khi art dời chỗ ghế.
        ///
        /// Quy ước: mọi SpriteRenderer có tên bắt đầu bằng "Chair"/"Seat"/"Sofa" được coi là một ghế.
        /// </summary>
        [MenuItem("DreamCafe/Decor/Đọc ô ghế từ art vào GridOccupant", priority = 32)]
        private static void ReadSeatsFromArt()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorFootprint] Cần một Grid trong scene để quy đổi toạ độ.");
                return;
            }

            var report = new StringBuilder("[DecorFootprint] Ô ghế đọc từ art:");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Decor" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var occupant = root != null ? root.GetComponent<GridOccupant>() : null;
                if (occupant == null)
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                var offsets = new List<Vector2Int>();
                var names = new List<string>();
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string n = renderer.name;
                    string key = n.ToLowerInvariant();
                    // Art đặt tên không thống nhất: Chair_1, Armchair_L, ArmchairRL (1)... nên dò
                    // theo chuỗi con thay vì tiền tố.
                    if (!key.Contains("chair") && !key.Contains("seat") && !key.Contains("sofa")) continue;

                    // Quy đổi phải lấy mốc là TÂM một ô, không phải gốc toạ độ: biên ô isometric
                    // là hình thoi nên cùng một khoảng lệch, đứng ở tâm ô hay ở góc ô cho ra hai
                    // kết quả khác nhau. Ngoài thực tế món nội thất luôn đứng ở tâm ô (đã snap).
                    Vector3 local = renderer.transform.position - root.transform.position;
                    var originCell = new Vector3Int(0, 0, 0);
                    Vector3 originCenter = grid.GetCellCenterWorld(originCell);
                    Vector3Int shifted = grid.WorldToCell(originCenter + local);

                    var offset = new Vector2Int(shifted.x - originCell.x, shifted.y - originCell.y);
                    if (offsets.Contains(offset))
                    {
                        Debug.LogWarning($"[DecorFootprint] {System.IO.Path.GetFileNameWithoutExtension(path)}: " +
                                         $"'{n}' rơi trùng ô ({offset.x},{offset.y}) với ghế khác — art để hai ghế quá sát nhau.");
                    }

                    offsets.Add(offset);
                    names.Add(n);
                }

                var so = new SerializedObject(occupant);
                var seats = so.FindProperty("_seats");
                seats.arraySize = offsets.Count;
                for (int i = 0; i < offsets.Count; i++)
                {
                    var element = seats.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("cell").vector2IntValue = offsets[i];
                    element.FindPropertyRelative("label").stringValue = names[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                report.Append($"  {System.IO.Path.GetFileNameWithoutExtension(path),-28} {offsets.Count} ghế:");
                for (int i = 0; i < offsets.Count; i++) report.Append($" {names[i]}({offsets[i].x},{offsets[i].y})");
                report.AppendLine();
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Kéo art của từng cái ghế về đúng **tâm ô ghế** đã khai báo trong
        /// <see cref="GridOccupant"/>. Không căn thì khách ngồi đúng tâm ô nhưng hình cái ghế lại
        /// lệch vài chục cm — nhìn như ngồi hụt ra ngoài ghế.
        ///
        /// Ghế thứ i trong art ghép với chỗ ngồi thứ i trong danh sách (cùng thứ tự lúc đọc từ art).
        /// </summary>
        [MenuItem("DreamCafe/Decor/Căn art ghế vào tâm ô", priority = 33)]
        private static void AlignSeatArtToCells()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorFootprint] Cần một Grid trong scene để quy đổi toạ độ.");
                return;
            }

            var origin = new Vector3Int(0, 0, 0);
            Vector3 originCenter = grid.GetCellCenterWorld(origin);
            var report = new StringBuilder("[DecorFootprint] Căn art ghế vào tâm ô:");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Decor" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var occupant = root != null ? root.GetComponent<GridOccupant>() : null;
                if (occupant == null || occupant.SeatCount == 0)
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                var chairs = new List<Transform>();
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string key = renderer.name.ToLowerInvariant();
                    if (key.Contains("chair") || key.Contains("seat") || key.Contains("sofa")) chairs.Add(renderer.transform);
                }

                var so = new SerializedObject(occupant);
                var seats = so.FindProperty("_seats");
                int count = Mathf.Min(chairs.Count, seats.arraySize);

                report.AppendLine();
                report.Append($"  {System.IO.Path.GetFileNameWithoutExtension(path),-28}");

                for (int i = 0; i < count; i++)
                {
                    Vector2Int cell = seats.GetArrayElementAtIndex(i).FindPropertyRelative("cell").vector2IntValue;
                    Vector3 target = grid.GetCellCenterWorld(new Vector3Int(origin.x + cell.x, origin.y + cell.y, 0)) - originCenter;

                    Vector3 before = chairs[i].localPosition;
                    chairs[i].localPosition = new Vector3(target.x, target.y, before.z);
                    report.Append($"  {chairs[i].name}: ({before.x:0.00},{before.y:0.00})->({target.x:0.00},{target.y:0.00})");
                }

                if (chairs.Count != seats.arraySize)
                {
                    Debug.LogWarning($"[DecorFootprint] {System.IO.Path.GetFileNameWithoutExtension(path)}: " +
                                     $"{chairs.Count} ghế trong art nhưng khai báo {seats.arraySize} chỗ ngồi — số lẻ bị bỏ qua.");
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Đặt art của từng món **đúng tâm ô** — tâm ảnh trùng tâm ô, không nâng chân lên.
        /// Ghi thẳng vào prefab nên mọi chỗ dùng prefab đó đều theo, không phải sửa từng instance
        /// trong scene.
        ///
        /// Hai kiểu prefab:
        /// - Sprite nằm trên ROOT: vị trí do <c>DecorItem.DefaultSpawnOffset</c> quyết định (DecorSlot
        ///   áp dụng lúc dựng món), nên đặt offset về 0 và dọn luôn local position của root.
        /// - Sprite nằm ở CON: đặt local position của từng mảnh đúng bằng tâm ô của nó — thân ở ô
        ///   gốc, mỗi ghế ở ô ghế của nó.
        ///
        /// Chạy lại nhiều lần cho cùng kết quả.
        /// </summary>
        [MenuItem("DreamCafe/Decor/Đặt art đúng tâm ô", priority = 34)]
        private static void PlaceArtOnCellCenter()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorFootprint] Cần một Grid trong scene để quy đổi toạ độ.");
                return;
            }

            var origin = new Vector3Int(0, 0, 0);
            Vector3 originCenter = grid.GetCellCenterWorld(origin);
            var report = new StringBuilder("[DecorFootprint] Đặt art vào tâm ô:");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Decor" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var occupant = root != null ? root.GetComponent<GridOccupant>() : null;
                if (occupant == null)
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (root.GetComponent<SpriteRenderer>() != null)
                {
                    root.transform.localPosition = Vector3.zero;
                    ClearSpawnOffset(name, report);
                }
                else
                {
                    var so = new SerializedObject(occupant);
                    var seats = so.FindProperty("_seats");
                    int seatIndex = 0;

                    foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (renderer.sprite == null || renderer.transform == root.transform) continue;

                        string key = renderer.name.ToLowerInvariant();
                        bool isChair = key.Contains("chair") || key.Contains("seat") || key.Contains("sofa");

                        Vector2Int cell = Vector2Int.zero;
                        if (isChair && seatIndex < seats.arraySize)
                        {
                            cell = seats.GetArrayElementAtIndex(seatIndex).FindPropertyRelative("cell").vector2IntValue;
                            seatIndex++;
                        }

                        Vector3 target = grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0)) - originCenter;
                        Vector3 before = renderer.transform.localPosition;
                        renderer.transform.localPosition = new Vector3(target.x, target.y, before.z);

                        report.AppendLine();
                        report.Append($"  {name,-26} {renderer.name,-16} ({before.x:0.00},{before.y:0.00}) -> " +
                                      $"({target.x:0.00},{target.y:0.00})  [tâm ô ({cell.x},{cell.y})]");
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>Đưa DefaultSpawnOffset của món về 0 để art nằm ngay tâm ô.</summary>
        private static void ClearSpawnOffset(string prefabName, StringBuilder report)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DecorItem"))
            {
                var item = AssetDatabase.LoadAssetAtPath<DecorItem>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null || item.Prefab == null || item.Prefab.name != prefabName) continue;

                var so = new SerializedObject(item);
                var prop = so.FindProperty("_defaultSpawnOffset");
                Vector2 before = prop.vector2Value;
                if (Mathf.Approximately(before.x, 0f) && Mathf.Approximately(before.y, 0f)) return;

                prop.vector2Value = Vector2.zero;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);

                report.AppendLine();
                report.Append($"  {prefabName,-26} {"(sprite ở root)",-16} DefaultSpawnOffset ({before.x:0.00},{before.y:0.00}) -> (0,0)");
                return;
            }
        }

        /// <summary>
        /// Phóng art cho **vừa khít số ô nó chiếm**: bề ngang hình = số ô ngang × bề ngang một ô.
        ///
        /// Art gốc được vẽ nhỏ hơn ô khá nhiều (bàn tròn chỉ rộng 58% một ô, ghế 44%) nên quán nhìn
        /// trống trải và mô hình "một món = một ô" không đọc ra được bằng mắt. Hệ số tính từ mảnh
        /// THÂN rồi áp cho mọi mảnh, nên tỉ lệ bàn/ghế giữ nguyên như art vẽ.
        ///
        /// Scale ghi vào từng mảnh chứ không vào root: scale root sẽ kéo theo cả khoảng cách giữa
        /// các mảnh, làm ghế văng ra khỏi ô của nó.
        /// </summary>
        [MenuItem("DreamCafe/Decor/Phóng art cho vừa ô", priority = 35)]
        private static void FitArtToCells()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorFootprint] Cần một Grid trong scene để quy đổi toạ độ.");
                return;
            }

            var report = new StringBuilder("[DecorFootprint] Phóng art cho vừa ô:");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Decor" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var occupant = root != null ? root.GetComponent<GridOccupant>() : null;
                if (occupant == null)
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                // Bề ngang cần đạt = số ô ngang món này trải ra.
                float targetWidth = HorizontalCellSpan(occupant, grid) * grid.cellSize.x;

                // Bề ngang hiện tại của phần THÂN (bỏ ghế ra, vì ghế nằm ở ô khác).
                float bodyWidth = 0f;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;
                    string key = renderer.name.ToLowerInvariant();
                    if (key.Contains("chair") || key.Contains("seat") || key.Contains("sofa")) continue;
                    bodyWidth = Mathf.Max(bodyWidth, renderer.sprite.bounds.size.x * Mathf.Abs(renderer.transform.lossyScale.x));
                }

                if (bodyWidth <= 0.0001f)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                float factor = targetWidth / bodyWidth;
                if (Mathf.Abs(factor - 1f) < 0.02f)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;
                    Vector3 scale = renderer.transform.localScale;
                    renderer.transform.localScale = new Vector3(scale.x * factor, scale.y * factor, scale.z);
                }

                report.AppendLine();
                report.Append($"  {name,-26} thân {bodyWidth:0.00} -> {targetWidth:0.00} (x{factor:0.00})");

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>Số ô mà khối thân của món trải ra theo bề ngang màn hình.</summary>
        private static int HorizontalCellSpan(GridOccupant occupant, Grid grid)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var offset in occupant.BlockedCells)
            {
                // Bề ngang màn hình của một ô lệch: x = 0.5 * (cx - cy) * cellWidth.
                float x = 0.5f * (offset.x - offset.y) * grid.cellSize.x;
                min = Mathf.Min(min, x);
                max = Mathf.Max(max, x);
            }

            if (min > max) return 1;
            return Mathf.Max(1, Mathf.RoundToInt((max - min) / grid.cellSize.x) + 1);
        }

        /// <summary>
        /// Suy ô của món **từ chân art**, không đụng gì tới hình vẽ.
        ///
        /// Art để pivot giữa ảnh nên tâm hình nằm lơ lửng giữa thân đồ vật; ô phải tính theo chỗ
        /// đồ vật CHẠM SÀN mới đúng nghĩa "món này đứng ở ô nào". Lệnh này lấy mép dưới của từng
        /// mảnh sprite, xem nó rơi vào ô nào, rồi ghi vào <see cref="GridOccupant"/>:
        /// mảnh thân -> ô chặn đường, mảnh ghế -> ô chỗ ngồi.
        ///
        /// Không dời, không phóng art — chỉ đổi số ô. Chạy lại cho cùng kết quả.
        /// </summary>
        [MenuItem("DreamCafe/Decor/Đặt ô theo chân đồ vật", priority = 35)]
        private static void DeriveCellsFromArtBase()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorFootprint] Cần một Grid trong scene để quy đổi toạ độ.");
                return;
            }

            var report = new StringBuilder("[DecorFootprint] Ô suy từ chân art:");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Decor" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var occupant = root != null ? root.GetComponent<GridOccupant>() : null;
                if (occupant == null)
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                // Mốc quy chiếu là TÂM một ô: biên ô isometric là hình thoi nên đo từ gốc toạ độ
                // sẽ lệch. Ngoài thực tế món luôn được đặt vào tâm ô.
                var originCell = new Vector3Int(0, 0, 0);
                Vector3 originCenter = grid.GetCellCenterWorld(originCell);

                var blocked = new List<Vector2Int>();
                var seatCells = new List<Vector2Int>();
                var seatNames = new List<string>();
                var seatChairs = new List<SpriteRenderer>();
                var seatSitOffsets = new List<Vector2>();

                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;

                    // Chân mảnh = mép dưới ảnh, quy về toạ độ thế giới giả định gốc ở tâm ô.
                    float scaleY = Mathf.Abs(renderer.transform.lossyScale.y);
                    float baseOffsetY = renderer.transform.localPosition.y + renderer.sprite.bounds.min.y * scaleY;
                    float baseOffsetX = renderer.transform.localPosition.x + renderer.sprite.bounds.center.x * Mathf.Abs(renderer.transform.lossyScale.x);

                    Vector3Int cell = grid.WorldToCell(originCenter + new Vector3(baseOffsetX, baseOffsetY, 0f));
                    var offset = new Vector2Int(cell.x - originCell.x, cell.y - originCell.y);

                    string key = renderer.name.ToLowerInvariant();
                    bool isChair = key.Contains("chair") || key.Contains("seat") || key.Contains("sofa");

                    if (isChair)
                    {
                        seatCells.Add(offset);
                        seatNames.Add(renderer.name);
                        seatChairs.Add(renderer);
                        // Điểm ngồi mặc định = ngay chỗ cái ghế được vẽ, để khách ngồi khít mặt ghế
                        // thay vì đứng giữa ô.
                        seatSitOffsets.Add(new Vector2(renderer.transform.localPosition.x, renderer.transform.localPosition.y));
                    }
                    else if (!blocked.Contains(offset))
                    {
                        blocked.Add(offset);
                    }
                }

                // Ghế trùng ô với thân thì bỏ ô thân đó ra: ưu tiên ngồi được.
                blocked.RemoveAll(c => seatCells.Contains(c));
                if (blocked.Count == 0) blocked.Add(Vector2Int.zero);

                var so = new SerializedObject(occupant);
                var blockedProp = so.FindProperty("_blockedCells");
                blockedProp.arraySize = blocked.Count;
                for (int i = 0; i < blocked.Count; i++) blockedProp.GetArrayElementAtIndex(i).vector2IntValue = blocked[i];

                var seatsProp = so.FindProperty("_seats");
                seatsProp.arraySize = seatCells.Count;
                for (int i = 0; i < seatCells.Count; i++)
                {
                    var element = seatsProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("cell").vector2IntValue = seatCells[i];
                    element.FindPropertyRelative("label").stringValue = seatNames[i];
                    element.FindPropertyRelative("sitOffset").vector2Value = seatSitOffsets[i];
                    element.FindPropertyRelative("chairArt").objectReferenceValue = seatChairs[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                report.AppendLine();
                report.Append($"  {name,-26} chặn:");
                foreach (var c in blocked) report.Append($" ({c.x},{c.y})");
                if (seatCells.Count > 0)
                {
                    report.Append("   ghế:");
                    for (int i = 0; i < seatCells.Count; i++) report.Append($" {seatNames[i]}({seatCells[i].x},{seatCells[i].y})");
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        private static float MeasureWidth(DecorItem item)
        {
            if (item.Prefab == null) return 0f;

            float min = float.MaxValue, max = float.MinValue;
            foreach (var renderer in item.Prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null) continue;

                float halfWidth = renderer.sprite.bounds.extents.x * Mathf.Abs(renderer.transform.lossyScale.x);
                float center = renderer.transform.localPosition.x + renderer.sprite.bounds.center.x;
                min = Mathf.Min(min, center - halfWidth);
                max = Mathf.Max(max, center + halfWidth);
            }

            return max > min ? max - min : 0f;
        }
    }
}
