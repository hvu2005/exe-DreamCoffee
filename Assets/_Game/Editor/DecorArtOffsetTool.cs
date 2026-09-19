using System.Collections.Generic;
using System.Text;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Tách bạch **ô lưới** và **hình vẽ** cho mọi prefab nội thất.
    ///
    /// Trước đây hai thứ này dính vào nhau: gốc prefab nằm ở đâu đó giữa tấm hình, còn ô mà món đồ
    /// thật sự chiếm thì khai lệch đi vài ô cho khớp chân art — và phần lệch ấy nằm rải trong
    /// localPosition của từng sprite con. Hệ quả là công cụ nào chạm vào art là hình tự dịch, không
    /// truy ra được vì sao.
    ///
    /// Sau khi chuẩn hoá:
    /// <list type="bullet">
    /// <item>Gốc prefab = **ô sàn** của món. <c>_blockedCells[0]</c> luôn là (0,0).</item>
    /// <item>Toàn bộ sprite nằm dưới một nút con tên <c>Art</c>.</item>
    /// <item>Muốn nhích hình cho đẹp thì sửa đúng một chỗ: <c>GridOccupant._artOffset</c>.</item>
    /// </list>
    /// Hình vẽ **không xê dịch** sau khi chạy: phần lệch cũ được dồn nguyên vào _artOffset.
    ///
    /// Menu: Tools > DreamCafe > Chuẩn hoá ô sàn + offset hình
    /// </summary>
    public static class DecorArtOffsetTool
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Decor";
        private const string ArtNodeName = "Art";

        /// <summary>Đổi một ô lệch sang khoảng cách thật trong thế giới (lưới isometric 1 x 0.5).</summary>
        private static Vector2 CellToWorld(Vector2Int cell) =>
            new(0.5f * (cell.x - cell.y), 0.25f * (cell.x + cell.y));

        [MenuItem("Tools/DreamCafe/Chuẩn hoá ô sàn + offset hình", priority = 2)]
        private static void Normalize()
        {
            var report = new StringBuilder("[ChuẩnHoáArt] Kết quả:");
            int done = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Normalize(path, report)) done++;
            }

            AssetDatabase.SaveAssets();
            report.AppendLine();
            report.Append($"  Xong {done} prefab.");
            Debug.Log(report.ToString());
        }

        private static bool Normalize(string path, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var occupant = root.GetComponent<GridOccupant>();
                if (occupant == null) return false;

                var so = new SerializedObject(occupant);
                var blocked = so.FindProperty("_blockedCells");
                if (blocked.arraySize == 0) return false;

                Vector2Int floor = blocked.GetArrayElementAtIndex(0).vector2IntValue;
                Vector2 shift = CellToWorld(floor);

                var art = EnsureArtNode(root);
                if (art == null)
                {
                    report.AppendLine();
                    report.Append($"  {root.name,-26} KHÔNG có sprite nào, bỏ qua");
                    return false;
                }

                FlattenRootScale(root.transform, art);

                // Gốc prefab dời lên đúng ô sàn, nên hình phải lùi lại đúng chừng ấy mới đứng yên.
                Vector2 artOffset = (Vector2)so.FindProperty("_artOffset").vector2Value - shift;
                so.FindProperty("_artOffset").vector2Value = artOffset;
                so.FindProperty("_artRoot").objectReferenceValue = art;

                Rebase(blocked, floor);
                RebaseSeats(so.FindProperty("_seats"), floor, shift);
                so.ApplyModifiedPropertiesWithoutUndo();

                occupant.ApplyArtOffset();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine();
                report.Append($"  {root.name,-26} ô sàn {floor} → (0,0), _artOffset = {artOffset:F3}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Gom mọi sprite vào một nút con tên "Art". Prefab nào vẽ thẳng sprite lên gốc thì chuyển
        /// hẳn component xuống nút con — giữ nguyên mọi thiết lập bằng copy/paste component, chứ
        /// dựng lại tay là mất sprite, sorting layer, màu...
        /// </summary>
        private static Transform EnsureArtNode(GameObject root)
        {
            var existing = root.transform.Find(ArtNodeName);
            if (existing != null) return existing;

            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return null;

            var art = new GameObject(ArtNodeName);
            art.transform.SetParent(root.transform, false);
            art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity;
            art.transform.localScale = Vector3.one;

            var rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                ComponentUtility.CopyComponent(rootRenderer);
                ComponentUtility.PasteComponentAsNew(art);
                Object.DestroyImmediate(rootRenderer);
            }

            // Chỉ nhặt con trực tiếp của gốc: cây con bên trong một mảnh art thì để nguyên.
            var movers = new List<Transform>();
            foreach (Transform child in root.transform)
            {
                if (child == art.transform) continue;
                if (child.GetComponentInChildren<SpriteRenderer>(true) != null) movers.Add(child);
            }
            foreach (var child in movers) child.SetParent(art.transform, false);

            return art.transform;
        }

        /// <summary>
        /// Dồn scale của gốc prefab xuống nút hình, để gốc còn đúng scale 1.
        ///
        /// Bắt buộc phải làm: <c>_artOffset</c> được ghi vào localPosition của nút hình, mà
        /// localPosition thì bị scale của cha nhân vào. Gốc tủ lạnh đang scale 0.6 thì khai offset
        /// 0.5 lại chỉ nhích được 0.3 — con số trong Inspector không còn nghĩa gì. Đưa scale xuống
        /// dưới thì offset trở thành đơn vị thế giới, gõ bao nhiêu nhích bấy nhiêu.
        /// </summary>
        private static void FlattenRootScale(Transform root, Transform art)
        {
            Vector3 scale = root.localScale;
            if (scale == Vector3.one) return;

            art.localScale = Vector3.Scale(art.localScale, scale);
            art.localPosition = Vector3.Scale(art.localPosition, scale);
            root.localScale = Vector3.one;
        }

        /// <summary>Quy các ô thân về gốc mới, ô sàn thành (0,0).</summary>
        private static void Rebase(SerializedProperty blocked, Vector2Int floor)
        {
            for (int i = 0; i < blocked.arraySize; i++)
            {
                var element = blocked.GetArrayElementAtIndex(i);
                element.vector2IntValue -= floor;
            }
        }

        /// <summary>
        /// Ghế cũng quy về gốc mới. Điểm ngồi tính theo gốc món nên phải bù đúng quãng gốc vừa dời,
        /// bằng không khách ngồi lệch đi một ô so với cái ghế.
        /// </summary>
        private static void RebaseSeats(SerializedProperty seats, Vector2Int floor, Vector2 shift)
        {
            for (int i = 0; i < seats.arraySize; i++)
            {
                var seat = seats.GetArrayElementAtIndex(i);
                var cell = seat.FindPropertyRelative("cell");
                cell.vector2IntValue -= floor;

                var sit = seat.FindPropertyRelative("sitOffset");
                sit.vector2Value -= shift;
            }
        }
    }
}
