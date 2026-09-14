using System.Collections.Generic;
using System.IO;
using DreamCafe.DataControl;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh dữ liệu mẫu (Decor Items, Expansion Zones và DecorRepository) kèm Prefab hiển thị trong suốt (Transparent).
    /// Menu: DreamCafe > Setup > Generate Decor & Expansion Data
    /// </summary>
    public static class DecorDataGenerator
    {
        private const string DecorDataDir = "Assets/_Game/Data/Decor";
        private const string DecorPrefabDir = "Assets/_Game/Prefabs/Decor";
        private const string TransFurniturePath = "Assets/_Game/Art/Decor/cafe_furniture_pack_transparent.png";
        private const string TransCounterPath = "Assets/_Game/Art/Decor/barista_counter_pack_transparent.png";

        [MenuItem("DreamCafe/Setup/Generate Decor & Expansion Data")]
        public static void Generate()
        {
            Directory.CreateDirectory(DecorDataDir);
            Directory.CreateDirectory(DecorPrefabDir);

            // Đảm bảo atlas transparent được thiết lập
            SetupAtlases();

            // Load sub-sprites đã tách nền
            // Load sub-sprites đã tách nền và các asset mới
            var spriteCounterFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_counter.png");
            var spriteAcUnit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_ac_unit.png");
            var spriteWallArtLatte = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_wall_painting_latte.png");
            var spriteWallMenu = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_wall_menu.png");
            var spriteWallShelves = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_wall_shelves.png");
            var spritePlanterHydrangea = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/prop_planter_hydrangea.png");
            if (spritePlanterHydrangea == null)
            {
                spritePlanterHydrangea = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Decor/flower_tank.png");
            }

            var spriteCounter = LoadSubSprite(TransCounterPath, "sprite_counter_emerald");
            var spriteCoffeeMachine = LoadSubSprite(TransCounterPath, "sprite_coffee_machine");
            var spriteMenuBoard = LoadSubSprite(TransCounterPath, "sprite_menu_board");

            var spriteWoodRound = LoadSubSprite(TransFurniturePath, "sprite_table_wood_round");
            var spriteChairR = LoadSubSprite(TransFurniturePath, "sprite_chair_r");
            var spriteChairL = LoadSubSprite(TransFurniturePath, "sprite_chair_l");
            var spriteSofaR = LoadSubSprite(TransFurniturePath, "sprite_sofa_armchair_r");
            var spriteSofaL = LoadSubSprite(TransFurniturePath, "sprite_sofa_armchair_l");
            var spriteBakeryDisplay = LoadSubSprite(TransFurniturePath, "sprite_bakery_display");
            var spritePlantMonstera = LoadSubSprite(TransFurniturePath, "sprite_plant_monstera");

            // 1. Tạo Prefabs kết hợp (Composite Prefabs) không nền
            var prefabCounter = CreateCounterStationPrefab("Prefab_Counter_Emerald", spriteCounterFull, spriteCounter, spriteCoffeeMachine, spriteBakeryDisplay, spriteMenuBoard, spriteWallShelves);
            var prefabTable2Seats = CreateTable2SeatsPrefab("Prefab_Table_WoodRound", spriteWoodRound, spriteChairR, spriteChairL);
            var prefabTable4Seats = CreateTable4SeatsPrefab("Prefab_Table_Wood4Seats", spriteWoodRound, spriteChairR, spriteChairL);
            var prefabSofaSet = CreateSofaLoungePrefab("Prefab_Table_SofaSet", spriteWoodRound, spriteSofaR, spriteSofaL);
            var prefabBakery = CreateSingleSpritePrefab("Prefab_Bakery_Display", spriteBakeryDisplay, new Vector2(1.2f, 1.2f), 0.38f, 10);
            var prefabPlant = CreateSingleSpritePrefab("Prefab_Plant_Monstera", spritePlantMonstera, new Vector2(0.9f, 1.2f), 0.35f, 10);
            var prefabPlanter = CreateSingleSpritePrefab("Prefab_Planter_Hydrangea", spritePlanterHydrangea, new Vector2(1.2f, 0.9f), 1.0f, 12);
            var prefabAc = CreateSingleSpritePrefab("Prefab_AC_Unit", spriteAcUnit, new Vector2(0.8f, 1.4f), 0.72f, 7);
            var prefabWallArtLatte = CreateSingleSpritePrefab("Prefab_Wall_Painting_Latte", spriteWallArtLatte, new Vector2(0.8f, 1.0f), 1.0f, 20);
            var prefabWallMenu = CreateSingleSpritePrefab("Prefab_Wall_Menu", spriteWallMenu, new Vector2(0.8f, 1.0f), 1.0f, 20);
            var prefabWallShelves = CreateSingleSpritePrefab("Prefab_Wall_Shelves", spriteWallShelves, new Vector2(0.85f, 0.9f), 1.0f, 20);

            // 2. Tạo DecorItem ScriptableObjects
            var itemCounter = CreateOrGetDecor("item_counter_emerald", "Quầy Bar Xanh Ngọc",
                "Quầy pha chế gỗ sồi kết hợp xanh ngọc sang trọng, trang bị máy pha cà phê và quầy thanh toán.",
                DecorCategory.CounterStation, DecorTheme.ModernEmerald, 1, 0, 0, 200, true,
                0f, 0.05f, 0, 5f, spriteCounterFull != null ? spriteCounterFull : spriteCounter, prefabCounter);

            var itemWoodRound = CreateOrGetDecor("item_table_wood_round", "Bàn Gỗ Tròn 2 Ghế",
                "Bàn gỗ tự nhiên nhỏ gọn kèm 2 ghế đệm êm ái, thích hợp cho sinh viên và bạn trẻ.",
                DecorCategory.SeatingSet, DecorTheme.CozyWood, 1, 25000, 0, 100, true,
                0.05f, 0.03f, 2, 0f, spriteWoodRound, prefabTable2Seats);

            var itemWood4Seats = CreateOrGetDecor("item_table_wood_4seats", "Bàn Gỗ Lớn 4 Ghế",
                "Bàn ăn rộng rãi phục vụ nhóm khách 4 người, tăng sức chứa của quán.",
                DecorCategory.SeatingSet, DecorTheme.CozyWood, 2, 80000, 0, 300, false,
                0.10f, 0.06f, 4, 0f, spriteWoodRound, prefabTable4Seats);

            var itemSofaSet = CreateOrGetDecor("item_table_sofa_set", "Cụm Ghế Sofa Bọc Nỉ",
                "Cụm sofa nỉ cao cấp cực kỳ thoải mái, thu hút khách VIP và khách du lịch.",
                DecorCategory.SeatingSet, DecorTheme.CutePastel, 3, 150000, 0, 500, false,
                0.20f, 0.12f, 2, 0f, spriteSofaR, prefabSofaSet);

            var itemBakeryDisplay = CreateOrGetDecor("item_bakery_display", "Tủ Kính Bánh Ngọt",
                "Tủ trưng bày bánh tươi trong ngày giúp kích thích chi tiêu của khách.",
                DecorCategory.Appliance, DecorTheme.VintageClassic, 2, 120000, 0, 400, false,
                0.05f, 0.10f, 0, 15f, spriteBakeryDisplay, prefabBakery);

            var itemPlantMonstera = CreateOrGetDecor("item_plant_monstera", "Chậu Cây Monstera",
                "Chậu cây trầu bà lá xẻ thanh lọc không khí, tạo không gian xanh mát mẻ.",
                DecorCategory.FloorDecor, DecorTheme.CozyWood, 1, 30000, 0, 150, true,
                0.05f, 0.02f, 0, 2f, spritePlantMonstera, prefabPlant);

            var itemPlanterHydrangea = CreateOrGetDecor("item_planter_hydrangea", "Bồn Hoa Cẩm Tú Cầu",
                "Bồn hoa gỗ nở rộ cẩm tú cầu trắng, làm đẹp thêm cho mặt tiền quán với góc nhìn sàn Isometric.",
                DecorCategory.OutdoorPlanter, DecorTheme.CutePastel, 2, 50000, 0, 250, false,
                0.08f, 0.04f, 0, 3f, spritePlanterHydrangea, prefabPlanter);

            var itemAcUnit = CreateOrGetDecor("item_ac_unit", "Máy Lạnh Đứng",
                "Máy điều hòa đứng công suất lớn làm mát toàn bộ không gian quán.",
                DecorCategory.Appliance, DecorTheme.ModernEmerald, 1, 40000, 0, 120, true,
                0.05f, 0.05f, 0, 10f, spriteAcUnit, prefabAc);

            var itemWallArtLatte = CreateOrGetDecor("item_wall_art_latte", "Tranh Latte Nghệ Thuật",
                "Bức tranh cà phê latte trang nhã tạo cảm giác thư giãn cho không gian quán.",
                DecorCategory.WallDecor, DecorTheme.ModernEmerald, 1, 15000, 0, 80, true,
                0.05f, 0.02f, 0, 2f, spriteWallArtLatte, prefabWallArtLatte);

            var itemWallMenu = CreateOrGetDecor("item_wall_menu", "Bảng Menu Gỗ Treo Tường",
                "Bảng thực đơn gỗ sồi cao cấp giúp khách hàng dễ dàng gọi món yêu thích.",
                DecorCategory.WallDecor, DecorTheme.CozyWood, 1, 20000, 0, 120, true,
                0.08f, 0.03f, 0, 3f, spriteWallMenu, prefabWallMenu);

            var itemWallShelves = CreateOrGetDecor("item_wall_shelves", "Kệ Gỗ Ly Tách Treo Tường",
                "Kệ trưng bày ly tách gốm sứ nghệ thuật làm tăng vẻ sang trọng cho quán.",
                DecorCategory.WallDecor, DecorTheme.VintageClassic, 2, 45000, 0, 200, false,
                0.10f, 0.05f, 0, 5f, spriteWallShelves, prefabWallShelves);

            var allDecor = new[]
            {
                itemCounter, itemWoodRound, itemWood4Seats, itemSofaSet,
                itemBakeryDisplay, itemPlantMonstera, itemPlanterHydrangea, itemAcUnit,
                itemWallArtLatte, itemWallMenu, itemWallShelves
            };

            // 3. Tạo ExpansionZoneData ScriptableObjects (tất cả mở khóa mặc định để test slot tự do)
            var zoneStarter = CreateOrGetZone("zone_starter_1", ExpansionZoneId.Starter_Zone1,
                "Quầy Bar & Sảnh Ban Đầu", "Khu vực cơ bản ban đầu của quán cà phê gồm quầy bar và bàn đôi.",
                0, 0, 0, true, "slot_counter_main", "slot_table_01", "slot_table_02", "slot_table_03", "slot_ac_01", "slot_plant_01", "slot_planter_01", "slot_wall_01", "slot_wall_02");

            var zoneLounge = CreateOrGetZone("zone_lounge_2", ExpansionZoneId.Lounge_Zone2,
                "Sảnh Thưởng Thức Trong Nhà", "Mở rộng sảnh trong nhà với cụm sofa nỉ và bàn 4 chỗ ngồi cao cấp.",
                0, 0, 0, true, "slot_sofa_01", "slot_table_04", "slot_table_05", "slot_sofa_02", "slot_sofa_03", "slot_planter_02");

            var zoneOutdoor = CreateOrGetZone("zone_outdoor_3", ExpansionZoneId.OutdoorPatio_Zone3,
                "Sân Hiên Ngoài Trời", "Mở rộng không gian hè phố với lối vào, bậc thềm và bồn hoa tươi mát.",
                0, 0, 0, true, "slot_outdoor_01", "slot_outdoor_02");

            var allZones = new[] { zoneStarter, zoneLounge, zoneOutdoor };

            // 4. Tạo DecorRepository.asset
            string repoPath = $"{DecorDataDir}/DecorRepository.asset";
            var repo = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>(repoPath);
            if (repo == null)
            {
                repo = ScriptableObject.CreateInstance<ScriptableDecorRepository>();
                AssetDatabase.CreateAsset(repo, repoPath);
            }

            var soRepo = new SerializedObject(repo);
            var propItems = soRepo.FindProperty("_items");
            propItems.arraySize = allDecor.Length;
            for (int i = 0; i < allDecor.Length; i++)
            {
                propItems.GetArrayElementAtIndex(i).objectReferenceValue = allDecor[i];
            }

            var propZones = soRepo.FindProperty("_zones");
            propZones.arraySize = allZones.Length;
            for (int i = 0; i < allZones.Length; i++)
            {
                propZones.GetArrayElementAtIndex(i).objectReferenceValue = allZones[i];
            }
            soRepo.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DecorDataGenerator] Đã tạo thành công {allDecor.Length} nội thất, {allZones.Length} khu vực mở rộng tại '{DecorDataDir}'.");
        }

        private static void SetupAtlases()
        {
            DecorBackgroundRemover.SliceTransparentAtlases();
        }

        private static void SetupAtlas(string path, List<SpriteMetaData> metas)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;

#pragma warning disable CS0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static Sprite LoadSubSprite(string texturePath, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            foreach (var a in assets)
            {
                if (a is Sprite s && s.name == spriteName)
                    return s;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        }

        private static GameObject CreateSingleSpritePrefab(string name, Sprite sprite, Vector2 colliderSize, float scale = 0.35f, int sortingOrder = 10)
        {
            string path = $"{DecorPrefabDir}/{name}.prefab";
            var go = new GameObject(name);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = colliderSize;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateTable2SeatsPrefab(string name, Sprite tableSprite, Sprite chairR, Sprite chairL)
        {
            string path = $"{DecorPrefabDir}/{name}.prefab";
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);

            // Bàn chính
            var tableGo = new GameObject("Table");
            tableGo.transform.SetParent(root.transform, false);
            tableGo.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
            var srTable = tableGo.AddComponent<SpriteRenderer>();
            srTable.sprite = tableSprite;
            srTable.sortingOrder = 15;

            // Ghế bên trái (sau bàn, hướng phải vào bàn)
            var chairGoL = new GameObject("Chair_L");
            chairGoL.transform.SetParent(root.transform, false);
            chairGoL.transform.localPosition = new Vector3(-0.18f, 0.05f, 0f);
            chairGoL.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var srChairL = chairGoL.AddComponent<SpriteRenderer>();
            srChairL.sprite = chairR != null ? chairR : chairL;
            srChairL.sortingOrder = 14;

            // Ghế bên phải (trước bàn, hướng trái vào bàn)
            var chairGoR = new GameObject("Chair_R");
            chairGoR.transform.SetParent(root.transform, false);
            chairGoR.transform.localPosition = new Vector3(0.18f, -0.06f, 0f);
            chairGoR.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var srChairR = chairGoR.AddComponent<SpriteRenderer>();
            srChairR.sprite = chairL != null ? chairL : chairR;
            srChairR.sortingOrder = 16;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTable4SeatsPrefab(string name, Sprite tableSprite, Sprite chairR, Sprite chairL)
        {
            string path = $"{DecorPrefabDir}/{name}.prefab";
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 0.8f);

            var tableGo = new GameObject("Table");
            tableGo.transform.SetParent(root.transform, false);
            tableGo.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            var srTable = tableGo.AddComponent<SpriteRenderer>();
            srTable.sprite = tableSprite;
            srTable.sortingOrder = 15;

            // 4 Ghế xung quanh bàn
            Vector3[] pos = { new Vector3(-0.20f, 0.08f, 0), new Vector3(0.20f, 0.08f, 0),
                              new Vector3(-0.16f, -0.09f, 0), new Vector3(0.18f, -0.07f, 0) };
            Sprite[] sprites = { chairR, chairL, chairR, chairL };
            int[] orders = { 14, 14, 16, 16 };

            for (int i = 0; i < 4; i++)
            {
                var ch = new GameObject($"Chair_{i + 1}");
                ch.transform.SetParent(root.transform, false);
                ch.transform.localPosition = pos[i];
                ch.transform.localScale = new Vector3(0.27f, 0.27f, 1f);
                var sr = ch.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i];
                sr.sortingOrder = orders[i];
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateSofaLoungePrefab(string name, Sprite tableSprite, Sprite sofaR, Sprite sofaL)
        {
            string path = $"{DecorPrefabDir}/{name}.prefab";
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.1f, 0.8f);

            var tableGo = new GameObject("CoffeeTable");
            tableGo.transform.SetParent(root.transform, false);
            tableGo.transform.localScale = new Vector3(0.27f, 0.27f, 1f);
            var srTable = tableGo.AddComponent<SpriteRenderer>();
            srTable.sprite = tableSprite;
            srTable.sortingOrder = 15;

            var sofaLGo = new GameObject("Armchair_L");
            sofaLGo.transform.SetParent(root.transform, false);
            sofaLGo.transform.localPosition = new Vector3(-0.28f, 0.05f, 0f);
            sofaLGo.transform.localScale = new Vector3(0.33f, 0.33f, 1f);
            var srSofaL = sofaLGo.AddComponent<SpriteRenderer>();
            srSofaL.sprite = sofaR;
            srSofaL.sortingOrder = 14;

            var sofaRGo = new GameObject("Armchair_R");
            sofaRGo.transform.SetParent(root.transform, false);
            sofaRGo.transform.localPosition = new Vector3(0.28f, -0.05f, 0f);
            sofaRGo.transform.localScale = new Vector3(0.33f, 0.33f, 1f);
            var srSofaR = sofaRGo.AddComponent<SpriteRenderer>();
            srSofaR.sprite = sofaL;
            srSofaR.sortingOrder = 16;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCounterStationPrefab(string name, Sprite counterFull, Sprite counter, Sprite espresso, Sprite bakery, Sprite menu, Sprite shelves)
        {
            string path = $"{DecorPrefabDir}/{name}.prefab";
            var root = new GameObject(name);
            root.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.0f, 1.8f);
            col.offset = new Vector2(0f, 0.35f);

            if (counterFull != null)
            {
                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = counterFull;
                sr.sortingOrder = 8;
            }
            else
            {
                var counterGo = new GameObject("CounterMain");
                counterGo.transform.SetParent(root.transform, false);
                var srCounter = counterGo.AddComponent<SpriteRenderer>();
                srCounter.sprite = counter;
                srCounter.sortingOrder = 8;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static DecorItem CreateOrGetDecor(string id, string name, string desc, DecorCategory cat, DecorTheme theme,
            int tier, int price, int reqRep, int repBonus, bool unlocked,
            float patience, float tip, int seats, float mps, Sprite icon, GameObject prefab)
        {
            string path = $"{DecorDataDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<DecorItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<DecorItem>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_category").enumValueIndex = (int)cat;
            so.FindProperty("_theme").enumValueIndex = (int)theme;
            so.FindProperty("_tier").intValue = tier;
            so.FindProperty("_price").intValue = price;
            so.FindProperty("_requiredReputation").intValue = reqRep;
            so.FindProperty("_reputationBonus").intValue = repBonus;
            so.FindProperty("_isDefaultUnlocked").boolValue = unlocked;
            so.FindProperty("_patienceBonusPercent").floatValue = patience;
            so.FindProperty("_tipChanceBonus").floatValue = tip;
            so.FindProperty("_seatingCapacity").intValue = seats;
            so.FindProperty("_moneyPerSecondBonus").floatValue = mps;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            return item;
        }

        private static ExpansionZoneData CreateOrGetZone(string fileId, ExpansionZoneId zoneId, string name, string desc,
            int cost, int reqRep, int repReward, bool defaultUnlocked, params string[] slotIds)
        {
            string path = $"{DecorDataDir}/{fileId}.asset";
            var zone = AssetDatabase.LoadAssetAtPath<ExpansionZoneData>(path);
            if (zone == null)
            {
                zone = ScriptableObject.CreateInstance<ExpansionZoneData>();
                AssetDatabase.CreateAsset(zone, path);
            }

            var so = new SerializedObject(zone);
            so.FindProperty("_zoneId").enumValueIndex = (int)zoneId;
            so.FindProperty("_zoneName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_unlockPrice").intValue = cost;
            so.FindProperty("_requiredReputation").intValue = reqRep;
            so.FindProperty("_reputationBonus").intValue = repReward;
            so.FindProperty("_isDefaultUnlocked").boolValue = defaultUnlocked;

            var propSlots = so.FindProperty("_slotIdsInZone");
            propSlots.arraySize = slotIds.Length;
            for (int i = 0; i < slotIds.Length; i++)
            {
                propSlots.GetArrayElementAtIndex(i).stringValue = slotIds[i];
            }
            so.ApplyModifiedProperties();
            return zone;
        }
    }
}
