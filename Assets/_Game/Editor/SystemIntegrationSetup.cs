using System.Collections.Generic;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Tiện ích tích hợp toàn bộ các hệ thống vào scene đang mở và tạo Dev Scene riêng.
    /// Menu: DreamCafe > Setup > Integrate Systems into Active Scene
    /// Menu: DreamCafe > Setup > Create Dev Systems Test Scene
    /// </summary>
    public static class SystemIntegrationSetup
    {
        private const string CustomerRepoPath = "Assets/_Game/Data/Customers/CustomerRepository.asset";
        private const string RecipeRepoPath = "Assets/_Game/Data/Recipes/RecipeRepository.asset";
        private const string InventoryRepoPath = "Assets/_Game/Data/InventoryItem/InventoryItemRepository.asset";
        private const string DevScenePath = "Assets/_Game/Scenes/Dev_SystemsTest.unity";

        [MenuItem("DreamCafe/Setup/Integrate Systems into Active Scene")]
        public static void IntegrateActiveScene()
        {
            var db = Object.FindFirstObjectByType<DatabaseManager>(FindObjectsInactive.Include);
            if (db == null)
            {
                var dbGo = new GameObject("DatabaseManagerSystem");
                db = dbGo.AddComponent<DatabaseManager>();
                Undo.RegisterCreatedObjectUndo(dbGo, "Create DatabaseManagerSystem");
            }

            // Gắn GameSystemsProvider nếu chưa có
            var provider = db.GetComponent<GameSystemsProvider>();
            if (provider == null)
            {
                provider = Undo.AddComponent<GameSystemsProvider>(db.gameObject);
            }

            // Gán repositories vào records của DatabaseManager
            var soDb = new SerializedObject(db);
            var recordsProp = soDb.FindProperty("records");

            var custRepo = AssetDatabase.LoadAssetAtPath<ScriptableCustomerRepository>(CustomerRepoPath);
            var recipeRepo = AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>(RecipeRepoPath);
            var invRepo = AssetDatabase.LoadAssetAtPath<ScriptableInventoryItemRepository>(InventoryRepoPath);

            AddRecordIfMissing(recordsProp, custRepo);
            AddRecordIfMissing(recordsProp, recipeRepo);
            AddRecordIfMissing(recordsProp, invRepo);

            soDb.ApplyModifiedProperties();

            // Đảm bảo có TopBarCurrencyPanel
            TopBarUISetup.Build();

            EditorSceneManager.MarkSceneDirty(db.gameObject.scene);
            Debug.Log("[SystemIntegrationSetup] Đã tích hợp thành công GameSystemsProvider và Repositories vào scene đang mở!");
        }

        [MenuItem("DreamCafe/Setup/Create Dev Systems Test Scene")]
        public static void CreateDevScene()
        {
            // Tạo scene mới hoàn toàn riêng biệt để tránh conflict
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Tạo Canvas & EventSystem
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            // 2. Tạo DatabaseManagerSystem & GameSystemsProvider
            var dbGo = new GameObject("DatabaseManagerSystem");
            var db = dbGo.AddComponent<DatabaseManager>();
            dbGo.AddComponent<GameSystemsProvider>();

            var soDb = new SerializedObject(db);
            var recordsProp = soDb.FindProperty("records");

            var custRepo = AssetDatabase.LoadAssetAtPath<ScriptableCustomerRepository>(CustomerRepoPath);
            var recipeRepo = AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>(RecipeRepoPath);
            var invRepo = AssetDatabase.LoadAssetAtPath<ScriptableInventoryItemRepository>(InventoryRepoPath);

            AddRecordIfMissing(recordsProp, custRepo);
            AddRecordIfMissing(recordsProp, recipeRepo);
            AddRecordIfMissing(recordsProp, invRepo);

            soDb.ApplyModifiedProperties();

            // 3. Dựng Top Bar UI
            TopBarUISetup.Build();

            // 4. Gắn Driver script chạy test tự động
            var testDriverGo = new GameObject("SystemsTestDriver");
            testDriverGo.AddComponent<SystemsTestDriver>();

            // Lưu scene
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(newScene, DevScenePath);
            Debug.Log($"[SystemIntegrationSetup] Đã tạo Scene riêng biệt cho Dev tại '{DevScenePath}' thành công!");
        }

        private static void AddRecordIfMissing(SerializedProperty listProp, ScriptableObject asset)
        {
            if (asset == null) return;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                {
                    return; // Đã tồn tại
                }
            }

            int index = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(index);
            listProp.GetArrayElementAtIndex(index).objectReferenceValue = asset;
        }
    }
}
