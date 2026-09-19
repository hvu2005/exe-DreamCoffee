using System.Collections.Generic;
using System.IO;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Customer;
using DreamCafe.SystemControl.Decor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng toàn bộ phần scene mà hệ thống khách hàng cần: mặt NavMesh để AI đi lại, điểm spawn/exit,
    /// các điểm đứng trước quầy order, và <see cref="CustomerSceneManager"/> đã nối sẵn ghế ngồi.
    ///
    /// Game là 2D mặt phẳng XY, còn NavMesh vốn là hệ 3D nằm trên XZ — nên mặt sàn điều hướng được
    /// xoay -90° quanh trục X để nằm đúng mặt phẳng XY (khớp với ghi chú trong CustomerController).
    ///
    /// Menu: DreamCafe > Setup > Customer System in Active Scene
    /// </summary>
    public static class CustomerSceneSetup
    {
        private const string MenuPath = "DreamCafe/Setup/Customer System in Active Scene";

        private const string NavFloorName = "NavMeshFloor";
        private const string WalkableAreaName = "WalkableArea";
        private const string SystemRootName = "CustomerSystem";
        private const string CounterSlotId = "slot_counter_main";

        /// <summary>
        /// Vùng sàn dùng để bake NavMesh (world XY). Bake theo agent mặc định (bán kính 0.5) nên vùng
        /// đi lại thật sẽ thụt vào ~0.5 mỗi cạnh — rect này đã chừa sẵn phần thụt đó.
        /// </summary>
        private static readonly Rect FloorRect = Rect.MinMaxRect(-1.5f, -4.0f, 5.7f, 0.5f);

        /// <summary>
        /// Điểm khách xuất hiện và cũng là điểm khách đi ra khi rời quán. Phải nằm trên một ô CÓ
        /// GẠCH SÀN — đường đi giờ chạy trên lưới ô, đứng ngoài sàn là không tìm nổi đường vào.
        /// (2.50, -2.88) là tâm ô gạch thấp nhất của quán, tức chỗ cửa ra vào.
        /// </summary>
        private static readonly Vector2 SpawnPosition = new(2.5f, -2.88f);

        /// <summary>
        /// Các chỗ đứng gọi món: hai ô gạch trống ngay TRƯỚC thân quầy. Quầy chiếm trọn hàng trong
        /// cùng (y = 0.00), nên chỗ đứng lùi xuống đúng một hàng ô (y = -0.25) — đứng ngang hoặc cao
        /// hơn thân quầy là khách lọt vào trong quầy, vừa bị quầy vẽ đè mất vừa vô lý về bố cục.
        /// <see cref="DecorStarterLayoutTool"/> dùng lại chính mảng này khi dựng bố cục.
        /// </summary>
        internal static readonly Vector2[] CounterStandPositions =
        {
            new(1.00f, -0.25f),
            new(2.00f, -0.25f)
        };

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var navSurface = BuildNavMeshFloor();
            var systemRoot = FindOrCreateRoot(SystemRootName);

            var manager = systemRoot.GetComponent<CustomerSceneManager>();
            if (manager == null) manager = Undo.AddComponent<CustomerSceneManager>(systemRoot);

            var spawnPoint = EnsureChild(systemRoot.transform, "SpawnPoint");
            spawnPoint.position = SpawnPosition;

            var poolRoot = EnsureChild(systemRoot.transform, "PoolRoot");
            poolRoot.localPosition = Vector3.zero;

            var counterPoint = BuildCounterPoint(systemRoot.transform);

            WireManager(manager, spawnPoint, poolRoot, counterPoint);

            EditorSceneManager.MarkSceneDirty(systemRoot.scene);
            Debug.Log($"[CustomerSceneSetup] Xong — NavMesh {(navSurface != null && navSurface.navMeshData != null ? "đã bake" : "CHƯA bake được")}, " +
                      $"{CounterStandPositions.Length} chỗ đứng quầy. Chỗ ngồi nay do GridOccupant trên từng món khai báo. " +
                      "Bấm Play rồi nhấn C để gọi khách.");
        }

        // =====================================================================
        // NAVMESH
        // =====================================================================

        private static NavMeshSurface BuildNavMeshFloor()
        {
            var floor = FindOrCreateRoot(NavFloorName);
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));

            var surface = floor.GetComponent<NavMeshSurface>();
            if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(floor);

            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            EnsureWalkableArea(floor.transform);

            surface.BuildNavMesh();
            SaveNavMeshAsset(surface);

            EditorUtility.SetDirty(surface);
            return surface;
        }

        /// <summary>
        /// Tấm mesh phẳng làm "sàn" cho NavMesh bake lên. Renderer tắt đi nên không nhìn thấy trong game;
        /// bake lấy hình dạng từ MeshCollider của nó.
        /// </summary>
        private static void EnsureWalkableArea(Transform parent)
        {
            var area = parent.Find(WalkableAreaName);
            if (area == null)
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = WalkableAreaName;
                Undo.RegisterCreatedObjectUndo(plane, "Create Walkable Area");
                plane.transform.SetParent(parent, false);
                area = plane.transform;
            }

            var renderer = area.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;

            if (area.GetComponent<MeshCollider>() == null) area.gameObject.AddComponent<MeshCollider>();

            // Mesh Plane gốc là 10x10 đơn vị trên mặt local XZ; sau khi cha xoay -90° quanh X thì
            // local X -> world X và local Z -> world Y.
            area.rotation = parent.rotation;
            area.position = new Vector3(FloorRect.center.x, FloorRect.center.y, 0f);
            area.localScale = new Vector3(FloorRect.width / 10f, 1f, FloorRect.height / 10f);
        }

        /// <summary>
        /// BuildNavMesh() chỉ dựng dữ liệu trong bộ nhớ. Ghi ra asset để scene mở lại vẫn còn NavMesh,
        /// không phải bake lại mỗi lần.
        /// </summary>
        private static void SaveNavMeshAsset(NavMeshSurface surface)
        {
            var data = surface.navMeshData;
            if (data == null || AssetDatabase.Contains(data)) return;

            string sceneFolder = Path.GetDirectoryName(surface.gameObject.scene.path);
            string sceneName = Path.GetFileNameWithoutExtension(surface.gameObject.scene.path);
            if (string.IsNullOrEmpty(sceneFolder) || string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[CustomerSceneSetup] Scene chưa được lưu nên không ghi được NavMesh asset — lưu scene rồi chạy lại menu này.");
                return;
            }

            string folder = Path.Combine(sceneFolder, sceneName).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(sceneFolder, sceneName);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NavMesh-{NavFloorName}.asset");
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        // QUẦY ORDER & GHẾ NGỒI
        // =====================================================================

        private static OrderCounterPoint BuildCounterPoint(Transform fallbackParent)
        {
            // Gắn vào chính slot quầy bar nếu tìm thấy, để dời quầy là chỗ đứng dời theo
            var counterSlot = FindSlot(CounterSlotId);
            var parent = counterSlot != null ? counterSlot.transform : fallbackParent;

            var host = EnsureChild(parent, "OrderCounterPoint");
            var point = host.GetComponent<OrderCounterPoint>();
            if (point == null) point = Undo.AddComponent<OrderCounterPoint>(host.gameObject);

            var stands = new List<Transform>(CounterStandPositions.Length);
            for (int i = 0; i < CounterStandPositions.Length; i++)
            {
                var stand = EnsureChild(host, $"Stand_{i + 1:00}");
                stand.position = CounterStandPositions[i];
                stands.Add(stand);
            }

            var so = new SerializedObject(point);
            var prop = so.FindProperty("_standPoints");
            prop.arraySize = stands.Count;
            for (int i = 0; i < stands.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = stands[i];
            so.ApplyModifiedProperties();

            return point;
        }

        private static void WireManager(CustomerSceneManager manager, Transform spawnPoint, Transform poolRoot,
            OrderCounterPoint counterPoint)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("_spawnPoint").objectReferenceValue = spawnPoint;
            so.FindProperty("_poolRoot").objectReferenceValue = poolRoot;

            var counters = so.FindProperty("_counters");
            counters.arraySize = counterPoint != null ? 1 : 0;
            if (counterPoint != null) counters.GetArrayElementAtIndex(0).objectReferenceValue = counterPoint;

            so.ApplyModifiedProperties();
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static DecorSlot FindSlot(string slotId)
        {
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot != null && slot.SlotId == slotId) return slot;
            }
            return null;
        }

        private static GameObject FindOrCreateRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
