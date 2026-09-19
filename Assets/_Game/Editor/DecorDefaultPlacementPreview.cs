using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng sẵn những món nội thất mặc định (khai báo ở <c>DecorRepository._defaultPlacements</c>) ngay
    /// trong Scene View lúc chưa bấm Play, để nhìn bố cục quán mà không phải chạy game.
    ///
    /// Các bản preview được gắn <see cref="HideFlags.DontSave"/> nên KHÔNG bị ghi vào file scene —
    /// scene không phình ra và git không thấy diff. Trước khi vào Play mode chúng tự xoá, nhường chỗ
    /// cho đồ thật do <see cref="DecorController"/> sinh ra.
    ///
    /// Menu: DreamCafe > Decor > ...
    /// </summary>
    [InitializeOnLoad]
    public static class DecorDefaultPlacementPreview
    {
        private const string EnabledPrefKey = "DreamCafe.DecorPreview.Enabled";
        private const string PreviewSuffix = " (Preview)";

        private const string MenuToggle = "DreamCafe/Decor/Hiện nội thất mặc định trong Scene";
        private const string MenuRefresh = "DreamCafe/Decor/Làm mới preview nội thất";
        private const string MenuClear = "DreamCafe/Decor/Xoá preview nội thất";
        private const string MenuBake = "DreamCafe/Decor/Chốt preview thành đồ thật trong scene";

        static DecorDefaultPlacementPreview()
        {
            EditorApplication.delayCall += Refresh;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>Có bật preview hay không (lưu theo máy, không ảnh hưởng người khác trong team).</summary>
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        [MenuItem(MenuToggle, priority = 0)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;
            Refresh();
        }

        [MenuItem(MenuToggle, validate = true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuToggle, Enabled);
            return true;
        }

        [MenuItem(MenuRefresh, priority = 1)]
        private static void RefreshMenu() => Refresh();

        [MenuItem(MenuClear, priority = 2)]
        private static void ClearMenu() => Clear();

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Refresh();

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Rời edit mode -> dọn preview để không chồng lên đồ thật khi vào game.
            // Quay lại edit mode -> dựng lại.
            if (state == PlayModeStateChange.ExitingEditMode) Clear();
            else if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Refresh;
        }

        /// <summary>Xoá preview cũ rồi dựng lại theo cấu hình hiện tại của DecorRepository.</summary>
        public static void Refresh()
        {
            Clear();

            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var repository = FindRepository();
            if (repository == null) return;

            var placements = repository.GetDefaultPlacements();
            if (placements == null || placements.Length == 0) return;

            var slots = BuildSlotLookup();
            int spawned = 0;

            foreach (var placement in placements)
            {
                if (placement == null || placement.item == null || string.IsNullOrEmpty(placement.slotId)) continue;
                if (placement.item.Prefab == null) continue;
                if (!slots.TryGetValue(placement.slotId, out var slot) || slot == null) continue;

                if (SpawnPreview(slot, placement.item)) spawned++;
            }

            if (spawned > 0) SceneView.RepaintAll();
        }

        /// <summary>Gỡ toàn bộ bản preview khỏi mọi DecorSlot trong scene đang mở.</summary>
        public static void Clear()
        {
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mount = slot.MountPoint;
                if (mount == null) continue;

                for (int i = mount.childCount - 1; i >= 0; i--)
                {
                    var child = mount.GetChild(i);
                    if (child.name.EndsWith(PreviewSuffix))
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private static bool SpawnPreview(DecorSlot slot, DecorItem item)
        {
            var mount = slot.MountPoint;
            if (mount == null) return false;

            // Slot đã có sẵn model thật lưu trong scene (vd quầy bar được đặt tay) thì thôi,
            // dựng thêm preview chỉ tổ chồng hai cái lên nhau.
            if (mount.childCount > 0) return false;

            // InstantiatePrefab (không phải Instantiate thường): giữ liên kết prefab nên object hiện
            // màu xanh trong Hierarchy và sửa được như mọi prefab instance khác.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(item.Prefab, mount);
            instance.name = item.Prefab.name + PreviewSuffix;
            instance.transform.localPosition = slot.GetEffectiveOffset(item);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = item.Prefab.transform.localScale;

            // Dùng lại đúng logic hiển thị của slot để preview khớp 100% với lúc chạy game
            slot.ApplyWallPerspectiveToInstance(instance, item);
            slot.ApplySortingToInstance(instance);

            MarkDontSave(instance.transform);
            return true;
        }

        /// <summary>
        /// Đánh dấu cả cây con là "đừng lưu vào scene" — preview chỉ sống trong phiên Editor này.
        /// Cố ý KHÔNG dùng <see cref="HideFlags.NotEditable"/>: cờ đó khoá cứng object, chọn vào là
        /// không sửa được gì, rất khó chịu khi muốn nắn lại vị trí bàn ghế.
        /// </summary>
        private static void MarkDontSave(Transform root)
        {
            root.gameObject.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < root.childCount; i++)
            {
                MarkDontSave(root.GetChild(i));
            }
        }

        /// <summary>
        /// Biến các bản preview đang hiện thành object thật của scene: bỏ cờ DontSave và bỏ hậu tố
        /// "(Preview)". Sau đó chúng được lưu vào file scene như mọi prefab instance khác, sửa gì
        /// cũng giữ — và hệ preview sẽ tự bỏ qua slot đó vì nó đã có đồ sẵn.
        /// </summary>
        [MenuItem(MenuBake, priority = 3)]
        private static void Bake()
        {
            int baked = 0;
            Scene scene = default;

            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mount = slot.MountPoint;
                if (mount == null) continue;

                for (int i = 0; i < mount.childCount; i++)
                {
                    var child = mount.GetChild(i);
                    if (!child.name.EndsWith(PreviewSuffix)) continue;

                    child.name = child.name.Substring(0, child.name.Length - PreviewSuffix.Length);
                    ClearHideFlags(child);
                    Undo.RegisterCreatedObjectUndo(child.gameObject, "Bake decor preview");
                    scene = slot.gameObject.scene;
                    baked++;
                }
            }

            if (baked == 0)
            {
                Debug.Log("[DecorPreview] Không có bản preview nào để chốt.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[DecorPreview] Đã chốt {baked} món thành object thật trong scene — nhớ Ctrl+S để lưu.");
        }

        private static void ClearHideFlags(Transform root)
        {
            root.gameObject.hideFlags = HideFlags.None;
            for (int i = 0; i < root.childCount; i++) ClearHideFlags(root.GetChild(i));
        }

        private static Dictionary<string, DecorSlot> BuildSlotLookup()
        {
            var lookup = new Dictionary<string, DecorSlot>();
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot != null && !string.IsNullOrEmpty(slot.SlotId)) lookup[slot.SlotId] = slot;
            }
            return lookup;
        }

        private static ScriptableDecorRepository FindRepository()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableDecorRepository"))
            {
                var repo = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>(AssetDatabase.GUIDToAssetPath(guid));
                if (repo != null) return repo;
            }
            return null;
        }
    }
}
