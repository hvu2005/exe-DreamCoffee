using System.Collections.Generic;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using DreamCafe.SystemControl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ tự động tích hợp Hệ thống Decor và Mở Rộng Quán từ Dev_DecorExpansionTest.unity sang SampleScene.unity.
    /// Menu: DreamCafe > Setup > Integrate Decor Into SampleScene
    /// </summary>
    public static class DecorIntegrationTool
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string DevScenePath = "Assets/_Game/Scenes/Dev_DecorExpansionTest.unity";
        private const string TopBarPrefabPath = "Assets/_Game/Prefabs/TopBarCurrencyPanel.prefab";

        [MenuItem("DreamCafe/Setup/Integrate Decor Into SampleScene")]
        public static void Integrate()
        {
            Debug.Log("[DecorIntegrationTool] Bắt đầu tích hợp Decor vào SampleScene...");

            // 1. Mở SampleScene làm Scene chính
            Scene sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            if (!sampleScene.IsValid())
            {
                Debug.LogError($"[DecorIntegrationTool] Không thể mở scene: {SampleScenePath}");
                return;
            }

            // 2. Mở DevScene dạng Additive để đọc và sao chép cấu trúc
            Scene devScene = EditorSceneManager.OpenScene(DevScenePath, OpenSceneMode.Additive);
            if (!devScene.IsValid())
            {
                Debug.LogError($"[DecorIntegrationTool] Không thể mở dev scene: {DevScenePath}");
                return;
            }

            try
            {
                // A. Cấu hình Main Camera
                ConfigureCamera(sampleScene);

                // A2. Cấu hình Ánh Sáng 2D (chiếu sáng toàn bộ sorting layers)
                ConfigureLighting(sampleScene);

                // B. Cấu hình DatabaseManagerSystem
                ConfigureDatabaseManager(sampleScene);

                // C. Cấu hình Walls dưới Grid
                ConfigureWalls(sampleScene, devScene);

                // D. Sao chép ShopLayout và các Slot
                var (shopLayoutGo, placementPanelGo) = TransferShopLayoutAndUI(sampleScene, devScene);

                // E. Đóng DevScene (không lưu thay đổi)
                EditorSceneManager.CloseScene(devScene, true);

                // F. Lưu SampleScene
                EditorSceneManager.MarkSceneDirty(sampleScene);
                EditorSceneManager.SaveScene(sampleScene);
                AssetDatabase.SaveAssets();

                Debug.Log("<color=#44FF44><b>[DecorIntegrationTool] TÍCH HỢP THÀNH CÔNG HỆ THỐNG DECOR VÀO SAMPLE SCENE!</b></color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DecorIntegrationTool] Lỗi trong quá trình tích hợp: {ex}");
                if (devScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(devScene, true);
                }
            }
        }

        private static void ConfigureCamera(Scene scene)
        {
            var rootGos = scene.GetRootGameObjects();
            Camera mainCam = null;
            foreach (var go in rootGos)
            {
                if (go.CompareTag("MainCamera") || go.name == "Main Camera")
                {
                    mainCam = go.GetComponent<Camera>();
                    break;
                }
            }

            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camGo, scene);
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = 2.95f;
            mainCam.backgroundColor = new Color(0.867f, 0.867f, 0.867f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.transform.position = new Vector3(1.20f, -0.70f, -10f);

            if (mainCam.GetComponent<Physics2DRaycaster>() == null)
            {
                mainCam.gameObject.AddComponent<Physics2DRaycaster>();
                Debug.Log("[DecorIntegrationTool] Đã thêm Physics2DRaycaster vào Main Camera.");
            }
        }

        private static void ConfigureLighting(Scene scene)
        {
            var rootGos = scene.GetRootGameObjects();
            UnityEngine.Rendering.Universal.Light2D light = null;
            foreach (var go in rootGos)
            {
                light = go.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
                if (light != null) break;
            }

            if (light != null)
            {
                var allLayers = SortingLayer.layers;
                int[] layerIds = new int[allLayers.Length];
                for (int i = 0; i < allLayers.Length; i++) layerIds[i] = allLayers[i].id;
                light.targetSortingLayers = layerIds;
                EditorUtility.SetDirty(light);
                Debug.Log($"[DecorIntegrationTool] Đã cập nhật Global Light 2D chiếu sáng toàn bộ {allLayers.Length} Sorting Layers.");
            }
        }

        private static void ConfigureDatabaseManager(Scene scene)
        {
            var rootGos = scene.GetRootGameObjects();
            DatabaseManager db = null;
            GameObject dbGo = null;

            foreach (var go in rootGos)
            {
                db = go.GetComponent<DatabaseManager>();
                if (db != null)
                {
                    dbGo = go;
                    break;
                }
            }

            if (dbGo == null)
            {
                dbGo = new GameObject("DatabaseManagerSystem");
                db = dbGo.AddComponent<DatabaseManager>();
                SceneManager.MoveGameObjectToScene(dbGo, scene);
            }

            if (dbGo.GetComponent<GameSystemsProvider>() == null)
            {
                dbGo.AddComponent<GameSystemsProvider>();
            }

            var soDb = new SerializedObject(db);
            var recordsProp = soDb.FindProperty("records");
            recordsProp.arraySize = 5;
            recordsProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableCustomerRepository>("Assets/_Game/Data/Customers/CustomerRepository.asset");
            recordsProp.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>("Assets/_Game/Data/Recipes/RecipeRepository.asset");
            recordsProp.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableInventoryItemRepository>("Assets/_Game/Data/InventoryItem/InventoryItemRepository.asset");
            recordsProp.GetArrayElementAtIndex(3).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>("Assets/_Game/Data/Decor/DecorRepository.asset");
            recordsProp.GetArrayElementAtIndex(4).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableCurrencyRepository>("Assets/_Game/Data/Currency/CurrencyRepository.asset");
            soDb.ApplyModifiedProperties();

            Debug.Log("[DecorIntegrationTool] Đã nạp đủ 5 repositories vào DatabaseManager.");
        }

        private static void ConfigureWalls(Scene sampleScene, Scene devScene)
        {
            // Tìm Grid trong SampleScene
            Transform sampleGrid = null;
            foreach (var go in sampleScene.GetRootGameObjects())
            {
                if (go.name == "Grid")
                {
                    sampleGrid = go.transform;
                    break;
                }
            }

            if (sampleGrid == null)
            {
                Debug.LogWarning("[DecorIntegrationTool] Không tìm thấy Grid trong SampleScene để gắn Walls!");
                return;
            }

            // Kiểm tra Walls đã có chưa
            var existingWalls = sampleGrid.Find("Walls");
            if (existingWalls != null)
            {
                Debug.Log("[DecorIntegrationTool] Walls đã tồn tại dưới Grid.");
                return;
            }

            // Tìm Walls trong devScene
            Transform devWalls = null;
            foreach (var go in devScene.GetRootGameObjects())
            {
                if (go.name == "Grid")
                {
                    devWalls = go.transform.Find("Walls");
                    break;
                }
            }

            if (devWalls != null)
            {
                var cloneWalls = Object.Instantiate(devWalls.gameObject, sampleGrid);
                cloneWalls.name = "Walls";
                cloneWalls.transform.localPosition = devWalls.localPosition;
                cloneWalls.transform.localRotation = devWalls.localRotation;
                cloneWalls.transform.localScale = devWalls.localScale;
                Debug.Log("[DecorIntegrationTool] Đã sao chép Walls vào Grid.");
            }
            else
            {
                // Tự tạo nếu devScene không có
                var wallsGo = new GameObject("Walls");
                wallsGo.transform.SetParent(sampleGrid, false);
                wallsGo.transform.localPosition = new Vector3(0.26f, -0.15f, 0f);
                wallsGo.transform.localScale = new Vector3(1.14f, 1.14f, 1.14f);
                var sr = wallsGo.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/wall_seamless_perimeter.png");
                sr.sortingOrder = 0;
                Debug.Log("[DecorIntegrationTool] Đã tạo mới Walls dưới Grid.");
            }
        }

        private static (GameObject shopLayout, GameObject placementPanel) TransferShopLayoutAndUI(Scene sampleScene, Scene devScene)
        {
            // 1. Tìm Canvas trong SampleScene
            Canvas sampleCanvas = null;
            foreach (var go in sampleScene.GetRootGameObjects())
            {
                sampleCanvas = go.GetComponent<Canvas>();
                if (sampleCanvas != null) break;
            }

            if (sampleCanvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                sampleCanvas = canvasGo.GetComponent<Canvas>();
                sampleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                SceneManager.MoveGameObjectToScene(canvasGo, sampleScene);
            }

            // Đảm bảo có EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(esGo, sampleScene);
            }

            // 2. Thêm TopBarCurrencyPanel nếu chưa có
            var existingTopBar = sampleCanvas.transform.Find("TopBarCurrencyPanel");
            if (existingTopBar == null)
            {
                var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
                if (topBarPrefab != null)
                {
                    var topBar = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, sampleCanvas.transform);
                    topBar.name = "TopBarCurrencyPanel";
                    topBar.transform.SetAsFirstSibling();
                    Debug.Log("[DecorIntegrationTool] Đã thêm TopBarCurrencyPanel vào Canvas.");
                }
            }

            // 3. Sao chép DecorPlacementPanel từ devScene
            GameObject devPlacementPanel = null;
            foreach (var go in devScene.GetRootGameObjects())
            {
                if (go.name == "Canvas")
                {
                    var t = go.transform.Find("DecorPlacementPanel");
                    if (t != null) devPlacementPanel = t.gameObject;
                    break;
                }
            }

            GameObject samplePlacementPanel = null;
            var existingPanel = sampleCanvas.transform.Find("DecorPlacementPanel");
            if (existingPanel != null)
            {
                samplePlacementPanel = existingPanel.gameObject;
            }
            else if (devPlacementPanel != null)
            {
                samplePlacementPanel = Object.Instantiate(devPlacementPanel, sampleCanvas.transform);
                samplePlacementPanel.name = "DecorPlacementPanel";
                samplePlacementPanel.SetActive(false);
                samplePlacementPanel.transform.SetAsLastSibling();
                Debug.Log("[DecorIntegrationTool] Đã sao chép DecorPlacementPanel vào Canvas.");
            }

            // 4. Xóa ShopLayout cũ trong SampleScene nếu có
            foreach (var go in sampleScene.GetRootGameObjects())
            {
                if (go.name == "ShopLayout")
                {
                    Object.DestroyImmediate(go);
                    break;
                }
            }

            // 5. Sao chép ShopLayout từ devScene
            GameObject devShopLayout = null;
            foreach (var go in devScene.GetRootGameObjects())
            {
                if (go.name == "ShopLayout")
                {
                    devShopLayout = go;
                    break;
                }
            }

            GameObject sampleShopLayout = null;
            if (devShopLayout != null)
            {
                sampleShopLayout = Object.Instantiate(devShopLayout);
                sampleShopLayout.name = "ShopLayout";
                SceneManager.MoveGameObjectToScene(sampleShopLayout, sampleScene);
                sampleShopLayout.transform.position = devShopLayout.transform.position;
                sampleShopLayout.transform.rotation = devShopLayout.transform.rotation;
                sampleShopLayout.transform.localScale = devShopLayout.transform.localScale;

                // Đồng bộ tham chiếu DecorSceneManager tới DecorPlacementPanel
                var dsm = sampleShopLayout.GetComponent<DecorSceneManager>();
                if (dsm != null && samplePlacementPanel != null)
                {
                    dsm.PlacementPanel = samplePlacementPanel.GetComponent<DecorPlacementPanel>();
                    dsm.ValidateOrCollectSlots();
                }

                Debug.Log("[DecorIntegrationTool] Đã sao chép toàn bộ ShopLayout và liên kết DecorSceneManager.");
            }
            else
            {
                Debug.LogError("[DecorIntegrationTool] Không tìm thấy ShopLayout trong devScene!");
            }

            return (sampleShopLayout, samplePlacementPanel);
        }
    }
}
