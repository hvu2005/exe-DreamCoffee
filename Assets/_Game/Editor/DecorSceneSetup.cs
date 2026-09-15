using System;
using System.Collections.Generic;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using DreamCafe.SystemControl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ dựng tự động scene kiểm thử Hệ thống Decor và Mở Rộng Không Gian.
    /// Thiết kế chuẩn Isometric 2.5D theo đúng 100% ảnh mẫu:
    /// - Grid sàn IsometricZAsY chuẩn từ SampleScene, dùng tile floor (image 34_0).
    /// - Tường bao 4 dải liền mạch (Nook, Step, Counter, Right) từ wall_seamless_perimeter.png.
    /// - Góc nhìn Camera orthographicSize = 2.85f, background = #DDDDDD (#0.867, #0.867, #0.867).
    /// - Bậc thềm lối vào, khu vực khách hàng chờ kèm bóng thoại (!), nhãn customer-spawn.
    /// - Quầy bar ngọc bích, máy lạnh đứng, cây monstera, bồn hoa cẩm tú cầu, bàn tròn 2 ghế, bàn tròn 4 ghế, 3 bộ sofa nỉ.
    /// Menu: DreamCafe > Setup > Create Dev Decor & Expansion Scene
    /// </summary>
    public static class DecorSceneSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Dev_DecorExpansionTest.unity";
        private const string TopBarPrefabPath = "Assets/_Game/Prefabs/TopBarCurrencyPanel.prefab";
        private const string FloorTilePath = "Assets/_Game/Tilemap/image 34_0.asset";
        private const string SeamlessWallSpritePath = "Assets/_Game/Art/Decor/wall_seamless_perimeter.png";
        private const string StairsSpritePath = "Assets/_Game/Art/Decor/bậc.png";

        [MenuItem("DreamCafe/Setup/Create Dev Decor & Expansion Scene")]
        public static void CreateScene()
        {
            // Bảo đảm tường liền mạch đã được tạo
            DecorWallGenerator.Generate();

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera góc nhìn Isometric 2.5D, căn chỉnh góc nhìn và màu nền #DDDDDD y hệt ảnh mẫu
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 2.95f;
            cam.backgroundColor = new Color(0.867f, 0.867f, 0.867f); // #DDDDDD canvas grey
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            camGo.transform.position = new Vector3(1.20f, -0.70f, -10f);

            // 2. DatabaseManager & GameSystemsProvider
            var dbGo = new GameObject("DatabaseManagerSystem");
            var db = dbGo.AddComponent<DatabaseManager>();
            dbGo.AddComponent<GameSystemsProvider>();

            var soDb = new SerializedObject(db);
            var recordsProp = soDb.FindProperty("records");
            recordsProp.arraySize = 5;
            recordsProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableCustomerRepository>("Assets/_Game/Data/Customers/CustomerRepository.asset");
            recordsProp.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>("Assets/_Game/Data/Recipes/RecipeRepository.asset");
            recordsProp.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableInventoryItemRepository>("Assets/_Game/Data/InventoryItem/InventoryItemRepository.asset");
            recordsProp.GetArrayElementAtIndex(3).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>("Assets/_Game/Data/Decor/DecorRepository.asset");
            recordsProp.GetArrayElementAtIndex(4).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableCurrencyRepository>("Assets/_Game/Data/Currency/CurrencyRepository.asset");
            soDb.ApplyModifiedProperties();

            // 3. Canvas & TopBar Currency UI
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            if (topBarPrefab != null)
            {
                var topBar = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, canvas.transform);
                topBar.name = "TopBarCurrencyPanel";
            }

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            // 4. Grid & Ground Tilemap Isometric chuẩn như SampleScene
            var gridGo = new GameObject("Grid");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellLayout = Grid.CellLayout.IsometricZAsY;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            var tm = groundGo.AddComponent<Tilemap>();
            var tr = groundGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = 0;
            tr.sortOrder = TilemapRenderer.SortOrder.TopRight;
            tr.mode = TilemapRenderer.Mode.Chunk;

            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>(FloorTilePath);
            if (floorTile != null)
            {
                // Sàn phòng chính 6x6 (y = -7..-2, x = -3..2)
                for (int y = -7; y <= -2; y++)
                {
                    for (int x = -3; x <= 2; x++)
                    {
                        tm.SetTile(new Vector3Int(x, y, 0), floorTile);
                    }
                }
                // Nook mở rộng góc trái 4x2 (y = -1..0, x = -3..0)
                for (int y = -1; y <= 0; y++)
                {
                    for (int x = -3; x <= 0; x++)
                    {
                        tm.SetTile(new Vector3Int(x, y, 0), floorTile);
                    }
                }
            }

            // 5. Tường Xanh Liền Mạch (Seamless Walls) chuẩn Isometric khớp chính xác mép chân sàn
            var wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SeamlessWallSpritePath);
            var wallsGo = new GameObject("Walls");
            wallsGo.transform.SetParent(gridGo.transform, false);
            wallsGo.transform.localPosition = Vector3.zero;

            if (wallSprite != null)
            {
                var srWall = wallsGo.AddComponent<SpriteRenderer>();
                srWall.sprite = wallSprite;
                srWall.sortingOrder = 1;
            }

            // 6. Bậc thềm cửa vào (Entrance Steps) từ bậc.png tại mép trái phòng chính
            var stairsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StairsSpritePath);
            if (stairsSprite != null)
            {
                var stairsGo = new GameObject("EntranceSteps");
                stairsGo.transform.SetParent(gridGo.transform, false);
                stairsGo.transform.position = new Vector3(-0.15f, -1.85f, 0f);
                var srStairs = stairsGo.AddComponent<SpriteRenderer>();
                srStairs.sprite = stairsSprite;
                srStairs.sortingOrder = 11;
            }

            // Khu vực khách hàng chờ vào quán (Customer Spawn Area) theo ảnh mẫu
            var customerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Data/Customer/cus_sv_1.png");
            var bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_bubble_exclamation.png");

            var customerGo = new GameObject("CustomerSpawnPreview");
            customerGo.transform.position = new Vector3(-1.45f, -2.00f, 0f);
            customerGo.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
            var srCustomer = customerGo.AddComponent<SpriteRenderer>();
            srCustomer.sprite = customerSprite;
            srCustomer.sortingOrder = 12;

            if (bubbleSprite != null)
            {
                var bubbleGo = new GameObject("SpeechBubble");
                bubbleGo.transform.SetParent(customerGo.transform, false);
                bubbleGo.transform.localPosition = new Vector3(-0.85f, 0.85f, 0f);
                bubbleGo.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
                var srBubble = bubbleGo.AddComponent<SpriteRenderer>();
                srBubble.sprite = bubbleSprite;
                srBubble.sortingOrder = 14;
            }

            var labelGo = new GameObject("CustomerSpawnLabel");
            labelGo.transform.position = new Vector3(-3.0f, 1.8f, 0f);
            var tmpLabel = labelGo.AddComponent<TextMeshPro>();
            tmpLabel.text = "customer-spawn";
            tmpLabel.fontSize = 2.4f;
            tmpLabel.color = new Color(0.45f, 0.48f, 0.52f, 0.85f);
            tmpLabel.sortingOrder = 50;

            // 7. ShopLayout & Hệ Thống Slot-Based Decor & Expansion theo đúng vị trí ảnh mẫu
            var layoutRoot = new GameObject("ShopLayout");
            var sceneMgr = layoutRoot.AddComponent<DecorSceneManager>();

            var decorRepo = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>("Assets/_Game/Data/Decor/DecorRepository.asset");

            // --- ZONE 1: KHỞI NGHIỆP (Mở mặc định) ---
            var z1Go = new GameObject("Zone1_Starter");
            z1Go.transform.SetParent(layoutRoot.transform, false);
            var z1View = z1Go.AddComponent<ExpansionZoneView>();
            var soZ1 = new SerializedObject(z1View);
            soZ1.FindProperty("_zoneId").enumValueIndex = (int)ExpansionZoneId.Starter_Zone1;

            var z1Content = new GameObject("Content");
            z1Content.transform.SetParent(z1Go.transform, false);
            soZ1.FindProperty("_zoneContent").objectReferenceValue = z1Content;

            // Slots Zone 1: Quầy Bar, Máy lạnh, Cây monstera, Bồn hoa lối vào, Cụm sofa nook, 3 Bàn tròn 2 ghế, 2 Ô tranh tường
            var slotCounter = CreateSlot("slot_counter_main", DecorCategory.CounterStation, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(2.05f, 0.15f, 0f), 0);
            var slotAc = CreateSlot("slot_ac_01", DecorCategory.Appliance, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(0.35f, 0.45f, 0f), 0);
            var slotPlant1 = CreateSlot("slot_plant_01", DecorCategory.FloorDecor, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(-1.15f, -0.90f, 0f), 0);
            var slotPlanter1 = CreateSlot("slot_planter_01", DecorCategory.OutdoorPlanter, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(-0.85f, -1.50f, 0f), 0);
            var slotSofa1 = CreateSlot("slot_sofa_01", DecorCategory.SeatingSet, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(-0.55f, -0.95f, 0f), 2);
            var slotTable1 = CreateSlot("slot_table_01", DecorCategory.SeatingSet, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(-0.35f, 0.10f, 0f), 2);
            var slotTable2 = CreateSlot("slot_table_02", DecorCategory.SeatingSet, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(0.15f, -0.25f, 0f), 2);
            var slotTable3 = CreateSlot("slot_table_03", DecorCategory.SeatingSet, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(0.70f, -0.55f, 0f), 2);
            var slotWall1 = CreateSlot("slot_wall_01", DecorCategory.WallDecor, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(-0.90f, 0.70f, 0f), 0);
            var slotWall2 = CreateSlot("slot_wall_02", DecorCategory.WallDecor, ExpansionZoneId.Starter_Zone1, z1Content.transform,
                new Vector3(2.80f, 0.25f, 0f), 0);

            var propZ1Slots = soZ1.FindProperty("_slots");
            propZ1Slots.arraySize = 10;
            propZ1Slots.GetArrayElementAtIndex(0).objectReferenceValue = slotCounter;
            propZ1Slots.GetArrayElementAtIndex(1).objectReferenceValue = slotAc;
            propZ1Slots.GetArrayElementAtIndex(2).objectReferenceValue = slotPlant1;
            propZ1Slots.GetArrayElementAtIndex(3).objectReferenceValue = slotPlanter1;
            propZ1Slots.GetArrayElementAtIndex(4).objectReferenceValue = slotSofa1;
            propZ1Slots.GetArrayElementAtIndex(5).objectReferenceValue = slotTable1;
            propZ1Slots.GetArrayElementAtIndex(6).objectReferenceValue = slotTable2;
            propZ1Slots.GetArrayElementAtIndex(7).objectReferenceValue = slotTable3;
            propZ1Slots.GetArrayElementAtIndex(8).objectReferenceValue = slotWall1;
            propZ1Slots.GetArrayElementAtIndex(9).objectReferenceValue = slotWall2;
            soZ1.ApplyModifiedProperties();

            // --- ZONE 2: SẢNH TRONG NHÀ (Không khóa barrier) ---
            var z2Go = new GameObject("Zone2_Lounge");
            z2Go.transform.SetParent(layoutRoot.transform, false);
            var z2View = z2Go.AddComponent<ExpansionZoneView>();
            var soZ2 = new SerializedObject(z2View);
            soZ2.FindProperty("_zoneId").enumValueIndex = (int)ExpansionZoneId.Lounge_Zone2;

            var z2Content = new GameObject("Content");
            z2Content.transform.SetParent(z2Go.transform, false);
            soZ2.FindProperty("_zoneContent").objectReferenceValue = z2Content;

            // Slots Zone 2: 2 Bàn tròn 4 ghế, 2 Cụm Sofa dưới, Bồn hoa góc đáy
            var slotTable4 = CreateSlot("slot_table_04", DecorCategory.SeatingSet, ExpansionZoneId.Lounge_Zone2, z2Content.transform,
                new Vector3(1.10f, -1.35f, 0f), 4);
            var slotTable5 = CreateSlot("slot_table_05", DecorCategory.SeatingSet, ExpansionZoneId.Lounge_Zone2, z2Content.transform,
                new Vector3(2.15f, -1.15f, 0f), 4);
            var slotSofa2 = CreateSlot("slot_sofa_02", DecorCategory.SeatingSet, ExpansionZoneId.Lounge_Zone2, z2Content.transform,
                new Vector3(2.40f, -2.05f, 0f), 2);
            var slotSofa3 = CreateSlot("slot_sofa_03", DecorCategory.SeatingSet, ExpansionZoneId.Lounge_Zone2, z2Content.transform,
                new Vector3(3.55f, -1.45f, 0f), 2);
            var slotPlanter2 = CreateSlot("slot_planter_02", DecorCategory.OutdoorPlanter, ExpansionZoneId.Lounge_Zone2, z2Content.transform,
                new Vector3(1.65f, -2.55f, 0f), 0);

            var propZ2Slots = soZ2.FindProperty("_slots");
            propZ2Slots.arraySize = 5;
            propZ2Slots.GetArrayElementAtIndex(0).objectReferenceValue = slotTable4;
            propZ2Slots.GetArrayElementAtIndex(1).objectReferenceValue = slotTable5;
            propZ2Slots.GetArrayElementAtIndex(2).objectReferenceValue = slotSofa2;
            propZ2Slots.GetArrayElementAtIndex(3).objectReferenceValue = slotSofa3;
            propZ2Slots.GetArrayElementAtIndex(4).objectReferenceValue = slotPlanter2;
            soZ2.ApplyModifiedProperties();

            // --- ZONE 3: SÂN HIÊN NGOÀI TRỜI (Không khóa barrier) ---
            var z3Go = new GameObject("Zone3_Outdoor");
            z3Go.transform.SetParent(layoutRoot.transform, false);
            var z3View = z3Go.AddComponent<ExpansionZoneView>();
            var soZ3 = new SerializedObject(z3View);
            soZ3.FindProperty("_zoneId").enumValueIndex = (int)ExpansionZoneId.OutdoorPatio_Zone3;

            var z3Content = new GameObject("Content");
            z3Content.transform.SetParent(z3Go.transform, false);
            soZ3.FindProperty("_zoneContent").objectReferenceValue = z3Content;

            var slotOutdoor1 = CreateSlot("slot_outdoor_01", DecorCategory.SeatingSet, ExpansionZoneId.OutdoorPatio_Zone3, z3Content.transform, new Vector3(-2.8f, -2.0f, 0f), 2);
            var slotOutdoor2 = CreateSlot("slot_outdoor_02", DecorCategory.SeatingSet, ExpansionZoneId.OutdoorPatio_Zone3, z3Content.transform, new Vector3(-1.8f, -2.5f, 0f), 2);

            var propZ3Slots = soZ3.FindProperty("_slots");
            propZ3Slots.arraySize = 2;
            propZ3Slots.GetArrayElementAtIndex(0).objectReferenceValue = slotOutdoor1;
            propZ3Slots.GetArrayElementAtIndex(1).objectReferenceValue = slotOutdoor2;
            soZ3.ApplyModifiedProperties();

            // Trang bị nội thất ban đầu (Chủ động để trống slot_wall_01, slot_wall_02, slot_table_03, slot_sofa_03 để test đặt đồ)
            if (decorRepo != null)
            {
                slotCounter.DisplayItem(decorRepo.GetDecor("item_counter_emerald"));
                slotAc.DisplayItem(decorRepo.GetDecor("item_ac_unit"));
                slotPlant1.DisplayItem(decorRepo.GetDecor("item_plant_monstera"));
                slotPlanter1.DisplayItem(decorRepo.GetDecor("item_planter_hydrangea"));
                slotSofa1.DisplayItem(decorRepo.GetDecor("item_table_sofa_set"));
                slotTable1.DisplayItem(decorRepo.GetDecor("item_table_wood_round"));
                slotTable2.DisplayItem(decorRepo.GetDecor("item_table_wood_round"));
                // slotTable3: Để trống hiển thị ô footprint sàn kèm dấu '+'
                slotTable4.DisplayItem(decorRepo.GetDecor("item_table_wood_4seats"));
                slotTable5.DisplayItem(decorRepo.GetDecor("item_table_wood_4seats"));
                slotSofa2.DisplayItem(decorRepo.GetDecor("item_table_sofa_set"));
                // slotSofa3: Để trống hiển thị ô footprint sàn kèm dấu '+'
                slotPlanter2.DisplayItem(decorRepo.GetDecor("item_planter_hydrangea"));
                // slotWall1, slotWall2: Để trống hiển thị khung viền tường kèm dấu '+'
            }

            // 8. Tạo DecorPlacementPanel trên Canvas
            var fontLilita = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/Font/LilitaOne-Regular SDF.asset");
            var placementPanel = CreatePlacementPanel(canvas.transform, fontLilita);
            sceneMgr.PlacementPanel = placementPanel;

            // 9. Thu thập objects vào DecorSceneManager
            sceneMgr.CollectSceneObjects();

            // 10. Test Driver
            var driverGo = new GameObject("DecorTestDriver");
            var driver = driverGo.AddComponent<DecorTestDriver>();
            var soDriver = new SerializedObject(driver);
            soDriver.FindProperty("_runOnStart").boolValue = false;
            soDriver.FindProperty("_sceneManager").objectReferenceValue = sceneMgr;
            soDriver.ApplyModifiedProperties();

            // Lưu scene
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[DecorSceneSetup] Đã dựng thành công Scene chuẩn Isometric 100% khớp ảnh mẫu tại '{ScenePath}'!");
        }

        private static DecorSlot CreateSlot(string id, DecorCategory cat, ExpansionZoneId zone, Transform parent, Vector3 localPos, int seats)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var slot = go.AddComponent<DecorSlot>();
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.1f, 0.9f);

            var soSlot = new SerializedObject(slot);
            soSlot.FindProperty("_slotId").stringValue = id;
            soSlot.FindProperty("_allowedCategory").enumValueIndex = (int)cat;
            soSlot.FindProperty("_requiredZone").enumValueIndex = (int)zone;

            var mount = new GameObject("MountPoint");
            mount.transform.SetParent(go.transform, false);
            soSlot.FindProperty("_mountPoint").objectReferenceValue = mount.transform;

            // Tạo Empty Indicator (Chỉ báo ô trống)
            var indGo = new GameObject("EmptyIndicator");
            indGo.transform.SetParent(go.transform, false);
            indGo.transform.localPosition = Vector3.zero;
            var srInd = indGo.AddComponent<SpriteRenderer>();

            if (cat == DecorCategory.WallDecor)
            {
                srInd.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/slot_empty_wall.png");
                srInd.sortingOrder = 18;
            }
            else
            {
                srInd.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/slot_empty_floor.png");
                srInd.sortingOrder = 2; // Nằm sát trên sàn, dưới vật phẩm
            }

            soSlot.FindProperty("_emptyIndicator").objectReferenceValue = indGo;

            if (seats > 0)
            {
                var propSeats = soSlot.FindProperty("_seatAnchors");
                propSeats.arraySize = seats;
                float offset = 0.25f;
                for (int i = 0; i < seats; i++)
                {
                    var seat = new GameObject($"Seat_{i + 1}");
                    seat.transform.SetParent(go.transform, false);
                    float x = (i % 2 == 0) ? -offset : offset;
                    float y = (i >= 2) ? offset : -offset;
                    seat.transform.localPosition = new Vector3(x, y, 0);
                    propSeats.GetArrayElementAtIndex(i).objectReferenceValue = seat.transform;
                }
            }

            soSlot.ApplyModifiedProperties();
            return slot;
        }

        private static DecorPlacementPanel CreatePlacementPanel(Transform canvasTransform, TMP_FontAsset font)
        {
            var panelGo = new GameObject("DecorPlacementPanel");
            panelGo.transform.SetParent(canvasTransform, false);

            var rtPanel = panelGo.AddComponent<RectTransform>();
            rtPanel.anchorMin = new Vector2(0.5f, 0f);
            rtPanel.anchorMax = new Vector2(0.5f, 0f);
            rtPanel.pivot = new Vector2(0.5f, 0f);
            rtPanel.sizeDelta = new Vector2(860f, 320f);
            rtPanel.anchoredPosition = new Vector2(0f, 25f);

            var imgPanel = panelGo.AddComponent<UnityEngine.UI.Image>();
            imgPanel.color = new Color(0.10f, 0.12f, 0.17f, 0.96f);

            // Header Container
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panelGo.transform, false);
            var rtHeader = headerGo.AddComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0f, 1f);
            rtHeader.anchorMax = new Vector2(1f, 1f);
            rtHeader.pivot = new Vector2(0.5f, 1f);
            rtHeader.sizeDelta = new Vector2(-40f, 60f);
            rtHeader.anchoredPosition = new Vector2(0f, -8f);

            // Title
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(headerGo.transform, false);
            var rtTitle = titleGo.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0.5f);
            rtTitle.anchorMax = new Vector2(0f, 0.5f);
            rtTitle.pivot = new Vector2(0f, 0.5f);
            rtTitle.sizeDelta = new Vector2(500f, 28f);
            rtTitle.anchoredPosition = new Vector2(0f, 10f);
            var tmpTitle = titleGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpTitle.font = font;
            tmpTitle.text = "CHỌN NỘI THẤT KHU VỰC";
            tmpTitle.fontSize = 20f;
            tmpTitle.color = new Color(0.97f, 0.78f, 0.27f);

            // Subtitle
            var subGo = new GameObject("SubtitleText");
            subGo.transform.SetParent(headerGo.transform, false);
            var rtSub = subGo.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0.5f);
            rtSub.anchorMax = new Vector2(0f, 0.5f);
            rtSub.pivot = new Vector2(0f, 0.5f);
            rtSub.sizeDelta = new Vector2(500f, 22f);
            rtSub.anchoredPosition = new Vector2(0f, -14f);
            var tmpSub = subGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpSub.font = font;
            tmpSub.text = "Chọn món nội thất phù hợp để Mua hoặc Trang bị";
            tmpSub.fontSize = 13f;
            tmpSub.color = new Color(0.74f, 0.76f, 0.78f);

            // Close Button [X]
            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(headerGo.transform, false);
            var rtClose = closeGo.AddComponent<RectTransform>();
            rtClose.anchorMin = new Vector2(1f, 0.5f);
            rtClose.anchorMax = new Vector2(1f, 0.5f);
            rtClose.pivot = new Vector2(1f, 0.5f);
            rtClose.sizeDelta = new Vector2(34f, 34f);
            rtClose.anchoredPosition = new Vector2(0f, 0f);
            var imgClose = closeGo.AddComponent<UnityEngine.UI.Image>();
            imgClose.color = new Color(0.85f, 0.25f, 0.20f);
            var btnClose = closeGo.AddComponent<UnityEngine.UI.Button>();

            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var rtCloseText = closeTextGo.AddComponent<RectTransform>();
            rtCloseText.anchorMin = Vector2.zero;
            rtCloseText.anchorMax = Vector2.one;
            rtCloseText.sizeDelta = Vector2.zero;
            var tmpClose = closeTextGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpClose.font = font;
            tmpClose.text = "X";
            tmpClose.fontSize = 18f;
            tmpClose.alignment = TextAlignmentOptions.Center;
            tmpClose.color = Color.white;

            // Unequip Button [GỠ BỎ]
            var unequipGo = new GameObject("UnequipButton");
            unequipGo.transform.SetParent(headerGo.transform, false);
            var rtUnequip = unequipGo.AddComponent<RectTransform>();
            rtUnequip.anchorMin = new Vector2(1f, 0.5f);
            rtUnequip.anchorMax = new Vector2(1f, 0.5f);
            rtUnequip.pivot = new Vector2(1f, 0.5f);
            rtUnequip.sizeDelta = new Vector2(110f, 32f);
            rtUnequip.anchoredPosition = new Vector2(-44f, 0f);
            var imgUnequip = unequipGo.AddComponent<UnityEngine.UI.Image>();
            imgUnequip.color = new Color(0.40f, 0.45f, 0.50f);
            var btnUnequip = unequipGo.AddComponent<UnityEngine.UI.Button>();

            var unequipTextGo = new GameObject("Text");
            unequipTextGo.transform.SetParent(unequipGo.transform, false);
            var rtUnequipText = unequipTextGo.AddComponent<RectTransform>();
            rtUnequipText.anchorMin = Vector2.zero;
            rtUnequipText.anchorMax = Vector2.one;
            rtUnequipText.sizeDelta = Vector2.zero;
            var tmpUnequip = unequipTextGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpUnequip.font = font;
            tmpUnequip.text = "GỠ BỎ";
            tmpUnequip.fontSize = 13f;
            tmpUnequip.alignment = TextAlignmentOptions.Center;
            tmpUnequip.color = Color.white;

            // Cards Container
            var cardsGo = new GameObject("CardsContainer");
            cardsGo.transform.SetParent(panelGo.transform, false);
            var rtCards = cardsGo.AddComponent<RectTransform>();
            rtCards.anchorMin = new Vector2(0f, 0f);
            rtCards.anchorMax = new Vector2(1f, 1f);
            rtCards.pivot = new Vector2(0.5f, 0.5f);
            rtCards.offsetMin = new Vector2(25f, 15f);
            rtCards.offsetMax = new Vector2(-25f, -70f);
            var hlg = cardsGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Card Template
            var cardTplGo = new GameObject("CardTemplate");
            cardTplGo.transform.SetParent(panelGo.transform, false);
            var rtCardTpl = cardTplGo.AddComponent<RectTransform>();
            rtCardTpl.sizeDelta = new Vector2(240f, 220f);
            var imgCardBg = cardTplGo.AddComponent<UnityEngine.UI.Image>();
            imgCardBg.color = new Color(0.16f, 0.19f, 0.25f, 0.95f);

            // Icon Image
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cardTplGo.transform, false);
            var rtIcon = iconGo.AddComponent<RectTransform>();
            rtIcon.anchorMin = new Vector2(0.5f, 1f);
            rtIcon.anchorMax = new Vector2(0.5f, 1f);
            rtIcon.pivot = new Vector2(0.5f, 1f);
            rtIcon.sizeDelta = new Vector2(65f, 65f);
            rtIcon.anchoredPosition = new Vector2(0f, -8f);
            var imgIcon = iconGo.AddComponent<UnityEngine.UI.Image>();
            imgIcon.preserveAspect = true;

            // Name
            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(cardTplGo.transform, false);
            var rtName = nameGo.AddComponent<RectTransform>();
            rtName.anchorMin = new Vector2(0.5f, 1f);
            rtName.anchorMax = new Vector2(0.5f, 1f);
            rtName.pivot = new Vector2(0.5f, 1f);
            rtName.sizeDelta = new Vector2(220f, 24f);
            rtName.anchoredPosition = new Vector2(0f, -78f);
            var tmpName = nameGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpName.font = font;
            tmpName.fontSize = 15f;
            tmpName.alignment = TextAlignmentOptions.Center;
            tmpName.color = Color.white;

            // Theme
            var themeGo = new GameObject("ThemeText");
            themeGo.transform.SetParent(cardTplGo.transform, false);
            var rtTheme = themeGo.AddComponent<RectTransform>();
            rtTheme.anchorMin = new Vector2(0.5f, 1f);
            rtTheme.anchorMax = new Vector2(0.5f, 1f);
            rtTheme.pivot = new Vector2(0.5f, 1f);
            rtTheme.sizeDelta = new Vector2(220f, 18f);
            rtTheme.anchoredPosition = new Vector2(0f, -102f);
            var tmpTheme = themeGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpTheme.font = font;
            tmpTheme.fontSize = 11f;
            tmpTheme.alignment = TextAlignmentOptions.Center;
            tmpTheme.color = new Color(0.65f, 0.70f, 0.75f);

            // Stats
            var statsGo = new GameObject("StatsText");
            statsGo.transform.SetParent(cardTplGo.transform, false);
            var rtStats = statsGo.AddComponent<RectTransform>();
            rtStats.anchorMin = new Vector2(0.5f, 1f);
            rtStats.anchorMax = new Vector2(0.5f, 1f);
            rtStats.pivot = new Vector2(0.5f, 1f);
            rtStats.sizeDelta = new Vector2(220f, 36f);
            rtStats.anchoredPosition = new Vector2(0f, -122f);
            var tmpStats = statsGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpStats.font = font;
            tmpStats.fontSize = 11f;
            tmpStats.alignment = TextAlignmentOptions.Center;
            tmpStats.color = new Color(0.30f, 0.85f, 0.50f);

            // Action Button
            var actionBtnGo = new GameObject("ActionButton");
            actionBtnGo.transform.SetParent(cardTplGo.transform, false);
            var rtActBtn = actionBtnGo.AddComponent<RectTransform>();
            rtActBtn.anchorMin = new Vector2(0.5f, 0f);
            rtActBtn.anchorMax = new Vector2(0.5f, 0f);
            rtActBtn.pivot = new Vector2(0.5f, 0f);
            rtActBtn.sizeDelta = new Vector2(210f, 34f);
            rtActBtn.anchoredPosition = new Vector2(0f, 10f);
            var imgAct = actionBtnGo.AddComponent<UnityEngine.UI.Image>();
            imgAct.color = new Color(0.88f, 0.50f, 0.12f);
            var btnAct = actionBtnGo.AddComponent<UnityEngine.UI.Button>();

            var actTextGo = new GameObject("Text");
            actTextGo.transform.SetParent(actionBtnGo.transform, false);
            var rtActText = actTextGo.AddComponent<RectTransform>();
            rtActText.anchorMin = Vector2.zero;
            rtActText.anchorMax = Vector2.one;
            rtActText.sizeDelta = Vector2.zero;
            var tmpActText = actTextGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpActText.font = font;
            tmpActText.fontSize = 14f;
            tmpActText.alignment = TextAlignmentOptions.Center;
            tmpActText.color = Color.white;

            // Serialize DecorItemCardUI
            var cardUi = cardTplGo.AddComponent<DecorItemCardUI>();
            var soCard = new SerializedObject(cardUi);
            soCard.FindProperty("_iconImage").objectReferenceValue = imgIcon;
            soCard.FindProperty("_nameText").objectReferenceValue = tmpName;
            soCard.FindProperty("_themeText").objectReferenceValue = tmpTheme;
            soCard.FindProperty("_statsText").objectReferenceValue = tmpStats;
            soCard.FindProperty("_actionButton").objectReferenceValue = btnAct;
            soCard.FindProperty("_actionButtonBg").objectReferenceValue = imgAct;
            soCard.FindProperty("_actionButtonText").objectReferenceValue = tmpActText;
            soCard.ApplyModifiedProperties();

            cardTplGo.SetActive(false);

            // Serialize DecorPlacementPanel
            var panel = panelGo.AddComponent<DecorPlacementPanel>();
            var soPanel = new SerializedObject(panel);
            soPanel.FindProperty("_titleText").objectReferenceValue = tmpTitle;
            soPanel.FindProperty("_subtitleText").objectReferenceValue = tmpSub;
            soPanel.FindProperty("_closeButton").objectReferenceValue = btnClose;
            soPanel.FindProperty("_unequipButton").objectReferenceValue = btnUnequip;
            soPanel.FindProperty("_cardsContainer").objectReferenceValue = cardsGo.transform;
            soPanel.FindProperty("_cardTemplate").objectReferenceValue = cardTplGo;
            soPanel.ApplyModifiedProperties();

            panelGo.SetActive(false);
            return panel;
        }
    }
}
