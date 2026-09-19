using System.Collections.Generic;
using System.Text;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Nối lại các <see cref="DecorSlot"/> rời rạc trong scene về đúng prefab template của chúng,
    /// để sửa một lần trên prefab là mọi slot theo.
    ///
    /// Các slot trong scene từng bị unpack (tên còn chữ "Prefab_..." nhưng đã mất liên kết), nên mỗi
    /// lần đổi cấu trúc slot phải sửa tay từng cái. Lệnh này dựng instance mới từ template rồi chép
    /// sang toàn bộ dữ liệu riêng của từng slot — id, loại, khu vực, vị trí ghế, độ lệch từng món —
    /// và **mang theo cả đồ đang đặt trên slot** thay vì dựng lại.
    ///
    /// Menu: DreamCafe > Decor > Nối slot vào prefab template
    /// </summary>
    public static class DecorSlotRelinkTool
    {
        private const string FloorTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Floor_Template.prefab";
        private const string WallTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Wall_Template.prefab";

        /// <summary>Các trường của DecorSlot trỏ tới object con — phải trỏ lại vào instance mới.</summary>
        private static readonly string[] ChildRefFields = { "_mountPoint", "_promptBubble", "_emptyIndicator" };

        [MenuItem("DreamCafe/Decor/Nối slot vào prefab template", priority = 40)]
        private static void Relink()
        {
            var slots = new List<DecorSlot>(
                Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            var report = new StringBuilder("[DecorSlotRelink] Kết quả:");
            int done = 0, skipped = 0;
            Scene scene = default;

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                if (PrefabUtility.IsPartOfPrefabInstance(slot.gameObject))
                {
                    skipped++;
                    continue;
                }

                scene = slot.gameObject.scene;
                if (Relink(slot, report)) done++;
            }

            report.AppendLine();
            report.Append($"  Xong: {done} slot được nối lại, {skipped} slot vốn đã là prefab instance.");

            if (done > 0 && scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(report.ToString());
        }

        private static bool Relink(DecorSlot old, StringBuilder report)
        {
            string templatePath = old.AllowedCategory == DecorCategory.WallDecor ? WallTemplate : FloorTemplate;
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null)
            {
                report.AppendLine();
                report.Append($"  {old.SlotId,-18} KHÔNG tìm thấy template {templatePath}");
                return false;
            }

            var oldGo = old.gameObject;
            var parent = oldGo.transform.parent;
            int siblingIndex = oldGo.transform.GetSiblingIndex();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template, parent);
            instance.name = oldGo.name;
            instance.transform.SetSiblingIndex(siblingIndex);
            instance.transform.SetPositionAndRotation(oldGo.transform.position, oldGo.transform.rotation);
            instance.transform.localScale = oldGo.transform.localScale;

            CopySlotData(old, instance.GetComponent<DecorSlot>(), report);
            CopyChildTransforms(oldGo.transform, instance.transform);
            MoveMountedItems(old, instance.GetComponent<DecorSlot>());
            MoveExtraChildren(oldGo.transform, instance.transform, report);

            Undo.RegisterCreatedObjectUndo(instance, "Relink decor slot");
            Object.DestroyImmediate(oldGo);

            report.AppendLine();
            report.Append($"  {instance.GetComponent<DecorSlot>().SlotId,-18} đã nối vào {System.IO.Path.GetFileNameWithoutExtension(templatePath)}");
            return true;
        }

        /// <summary>
        /// Chép mọi trường dữ liệu của slot cũ sang slot mới, rồi trỏ lại các tham chiếu con.
        /// </summary>
        private static void CopySlotData(DecorSlot from, DecorSlot to, StringBuilder report)
        {
            var src = new SerializedObject(from);
            var dst = new SerializedObject(to);

            var iterator = src.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyPath == "m_Script") continue;
                dst.CopyFromSerializedProperty(iterator);
            }
            dst.ApplyModifiedPropertiesWithoutUndo();

            // Tham chiếu vừa chép vẫn trỏ vào con của slot CŨ (sắp bị xoá) — trỏ lại theo tên.
            foreach (string field in ChildRefFields)
            {
                RemapChildReference(dst, field, from.transform, to.transform);
            }

            var seats = dst.FindProperty("_seatAnchors");
            for (int i = 0; i < seats.arraySize; i++)
            {
                var element = seats.GetArrayElementAtIndex(i);
                var oldRef = element.objectReferenceValue as Transform;
                if (oldRef == null) continue;

                var remapped = to.transform.Find(GetPathUnder(from.transform, oldRef));
                if (remapped != null) element.objectReferenceValue = remapped;
                else report.Append($"  [!] {to.SlotId}: không tìm thấy ghế '{oldRef.name}' trong template");
            }

            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemapChildReference(SerializedObject dst, string field, Transform oldRoot, Transform newRoot)
        {
            var prop = dst.FindProperty(field);
            if (prop == null || prop.objectReferenceValue == null) return;

            Transform oldChild = prop.objectReferenceValue as Transform;
            if (oldChild == null && prop.objectReferenceValue is GameObject go) oldChild = go.transform;
            if (oldChild == null) return;

            string path = GetPathUnder(oldRoot, oldChild);
            var newChild = string.IsNullOrEmpty(path) ? newRoot : newRoot.Find(path);
            if (newChild == null) return;

            prop.objectReferenceValue = prop.objectReferenceValue is GameObject ? (Object)newChild.gameObject : newChild;
            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetPathUnder(Transform root, Transform child)
        {
            if (child == root) return string.Empty;

            var parts = new List<string>();
            var cursor = child;
            while (cursor != null && cursor != root)
            {
                parts.Insert(0, cursor.name);
                cursor = cursor.parent;
            }
            return string.Join("/", parts);
        }

        /// <summary>Chép vị trí/scale các con cùng tên (MountPoint, EmptyIndicator, Seat_x...).</summary>
        private static void CopyChildTransforms(Transform from, Transform to)
        {
            foreach (Transform oldChild in from)
            {
                var newChild = to.Find(oldChild.name);
                if (newChild == null) continue;

                newChild.localPosition = oldChild.localPosition;
                newChild.localRotation = oldChild.localRotation;
                newChild.localScale = oldChild.localScale;
                newChild.gameObject.SetActive(oldChild.gameObject.activeSelf);
            }
        }

        /// <summary>
        /// Mang sang những object con mà template KHÔNG có — ví dụ 'OrderCounterPoint' với các chỗ
        /// đứng gọi món chỉ gắn trên slot quầy. Không có bước này thì chúng biến mất cùng slot cũ,
        /// và hệ khách hàng mất luôn chỗ đứng.
        /// </summary>
        private static void MoveExtraChildren(Transform from, Transform to, StringBuilder report)
        {
            for (int i = from.childCount - 1; i >= 0; i--)
            {
                var child = from.GetChild(i);
                if (to.Find(child.name) != null) continue;

                Vector3 local = child.localPosition;
                child.SetParent(to, false);
                child.localPosition = local;

                report.Append($"  [giữ lại '{child.name}']");
            }
        }

        /// <summary>
        /// Chuyển đồ đang bày trên slot cũ sang mount point mới. Bản preview (DontSave) thì bỏ qua,
        /// nó sẽ tự dựng lại.
        /// </summary>
        private static void MoveMountedItems(DecorSlot from, DecorSlot to)
        {
            var oldMount = from.MountPoint;
            var newMount = to.MountPoint;
            if (oldMount == null || newMount == null) return;

            for (int i = oldMount.childCount - 1; i >= 0; i--)
            {
                var child = oldMount.GetChild(i);
                if (child.gameObject.hideFlags != HideFlags.None) continue;

                Vector3 local = child.localPosition;
                child.SetParent(newMount, false);
                child.localPosition = local;
            }
        }
    }
}
