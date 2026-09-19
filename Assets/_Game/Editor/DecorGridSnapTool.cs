using System.Collections.Generic;
using System.Text;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Kéo mọi <see cref="DecorSlot"/> và điểm neo ghế của chúng về đúng tâm ô gạch của
    /// <see cref="Grid"/> sàn, theo quy ước một món chiếm một ô (thân bàn một ô, mỗi ghế một ô).
    ///
    /// Đặt đúng ô là điều kiện cần để <see cref="ShopGrid"/> khoét NavMesh chuẩn: lệch nửa ô thì
    /// vật cản đè sang ô bên cạnh và khách tự nhiên bị chặn ở chỗ trống.
    ///
    /// Menu: DreamCafe > Decor > Snap nội thất vào lưới | Kiểm tra lệch lưới
    /// </summary>
    public static class DecorGridSnapTool
    {
        private const string MenuSnap = "DreamCafe/Decor/Snap nội thất vào lưới";
        private const string MenuCheck = "DreamCafe/Decor/Kiểm tra lệch lưới";

        [MenuItem(MenuCheck, priority = 20)]
        private static void Check()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorGridSnap] Scene không có Grid nào để căn theo.");
                return;
            }

            var report = new StringBuilder("[DecorGridSnap] Độ lệch so với tâm ô:\n");
            int offGrid = 0;

            foreach (var slot in CollectSlots())
            {
                foreach (var (label, t) in AnchorsOf(slot))
                {
                    float offset = Offset(grid, t.position);
                    if (offset > 0.01f) offGrid++;
                    report.AppendLine($"  {label,-26} lệch {offset:0.000} → ô {grid.WorldToCell(t.position)}");
                }
            }

            report.AppendLine($"  Tổng: {offGrid} điểm chưa nằm đúng tâm ô.");
            Debug.Log(report.ToString());
        }

        [MenuItem(MenuSnap, priority = 21)]
        private static void Snap()
        {
            var grid = Object.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
            if (grid == null)
            {
                Debug.LogWarning("[DecorGridSnap] Scene không có Grid nào để căn theo.");
                return;
            }

            var slots = CollectSlots();
            if (slots.Count == 0)
            {
                Debug.LogWarning("[DecorGridSnap] Không tìm thấy DecorSlot nào trong scene.");
                return;
            }

            int moved = 0;
            var used = new Dictionary<Vector3Int, string>();
            var report = new StringBuilder("[DecorGridSnap] Đã căn vào lưới:\n");

            foreach (var slot in slots)
            {
                foreach (var (label, t) in AnchorsOf(slot))
                {
                    Undo.RecordObject(t, "Snap decor to grid");

                    Vector3 before = t.position;
                    var cell = grid.WorldToCell(before);
                    Vector3 center = grid.GetCellCenterWorld(cell);
                    t.position = new Vector3(center.x, center.y, before.z);

                    if (Vector3.Distance(before, t.position) > 0.0001f) moved++;

                    if (used.TryGetValue(cell, out string owner))
                    {
                        Debug.LogWarning($"[DecorGridSnap] Ô {cell} bị trùng: '{label}' và '{owner}' cùng một ô — kê lệch ra một ô.");
                    }
                    else
                    {
                        used[cell] = label;
                    }

                    report.AppendLine($"  {label,-26} {before.x:0.00},{before.y:0.00} → {t.position.x:0.00},{t.position.y:0.00}  (ô {cell.x},{cell.y})");
                    EditorUtility.SetDirty(t);
                }
            }

            report.AppendLine($"  Tổng: {moved} điểm được dời, {used.Count} ô bị chiếm.");
            Debug.Log(report.ToString());

            EditorSceneManager.MarkSceneDirty(slots[0].gameObject.scene);
        }

        /// <summary>Mỗi slot đóng góp: thân món (mount point) + từng điểm neo ghế.</summary>
        private static IEnumerable<(string label, Transform t)> AnchorsOf(DecorSlot slot)
        {
            var body = slot.MountPoint != null ? slot.MountPoint : slot.transform;
            yield return (slot.SlotId, body);

            // Chỉ slot bàn ghế mới có ghế thật; chậu cây kế thừa điểm neo từ prefab template.
            if (slot.AllowedCategory != DreamCafe.DataControl.DecorCategory.SeatingSet) yield break;

            var seats = slot.SeatAnchors;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] != null) yield return ($"{slot.SlotId}/ghế{i}", seats[i]);
            }
        }

        private static float Offset(Grid grid, Vector3 world)
        {
            Vector3 center = grid.GetCellCenterWorld(grid.WorldToCell(world));
            return Vector2.Distance(new Vector2(world.x, world.y), new Vector2(center.x, center.y));
        }

        private static List<DecorSlot> CollectSlots()
        {
            var slots = new List<DecorSlot>(
                Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            slots.Sort((a, b) => string.CompareOrdinal(a.SlotId, b.SlotId));
            return slots;
        }
    }
}
