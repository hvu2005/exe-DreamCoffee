using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Customer;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng lại toàn bộ chỗ đặt nội thất (<see cref="DecorSlot"/>) của quán theo một bố cục chuẩn,
    /// khai báo bằng **toạ độ ô gạch** chứ không phải toạ độ thế giới gõ tay.
    ///
    /// Mỗi dòng trong bảng là một ô gạch: ô nào ghi ở đây thì món đồ đứng đúng ô đó — không còn
    /// khoản lùi ngược nào nữa. Hình vẽ có nhô lên khỏi ô bao nhiêu là chuyện riêng của
    /// <c>GridOccupant._artOffset</c> trong từng prefab (xem <see cref="DecorArtOffsetTool"/>).
    ///
    /// Bố cục: quầy bar chạy dọc hàng trong cùng, lối đi giữa để trống thẳng từ cửa lên quầy, bàn
    /// ghế chia hai bên lối đi, cây cối kê sát tường và hai bồn hoa ôm hai bên cửa ra vào.
    ///
    /// Menu: DreamCafe > Decor > Dựng lại bố cục slot chuẩn
    /// </summary>
    public static class DecorStarterLayoutTool
    {
        private const string MenuPath = "DreamCafe/Decor/Dựng lại bố cục slot chuẩn";

        private const string FloorTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Floor_Template.prefab";
        private const string WallTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Wall_Template.prefab";

        private const string ContentPath = "ShopLayout/Zone1_Starter/Content";
        private const string CounterSlotId = "slot_counter_main";

        /// <summary>
        /// Một chỗ đặt đồ. Slot sàn khai theo ô; slot tường khai theo toạ độ thế giới vì tường là
        /// mặt phẳng nghiêng của art, không chia ô.
        /// </summary>
        private readonly struct SlotPlan
        {
            public readonly string Id;
            public readonly DecorCategory Category;
            public readonly Vector2Int Cell;
            public readonly Vector2 WallPosition;
            public readonly WallPerspective Perspective;
            public readonly bool HasSeats;
            public readonly string Note;

            /// <summary>Slot trên sàn: khai đúng ô mà món đồ sẽ đứng.</summary>
            public SlotPlan(string id, DecorCategory category, Vector2Int cell, bool hasSeats, string note)
            {
                Id = id;
                Category = category;
                Cell = cell;
                WallPosition = Vector2.zero;
                Perspective = WallPerspective.LeftWall;
                HasSeats = hasSeats;
                Note = note;
            }

            /// <summary>Slot treo tường: khai thẳng điểm treo trên mảng tường.</summary>
            public SlotPlan(string id, Vector2 wallPosition, WallPerspective perspective, string note)
            {
                Id = id;
                Category = DecorCategory.WallDecor;
                Cell = Vector2Int.zero;
                WallPosition = wallPosition;
                Perspective = perspective;
                HasSeats = false;
                Note = note;
            }

            public bool IsWall => Category == DecorCategory.WallDecor;

            /// <summary>Ô của slot — cũng chính là ô món đồ đứng lên.</summary>
            public Vector3Int AnchorCell => new(Cell.x, Cell.y, 0);
        }

        /// <summary>
        /// Bố cục quán. Sàn là hình thoi 7 x 10 ô, cửa ra vào ở đỉnh dưới (ô -4,-9), hai mảng tường
        /// sau gặp nhau ở hõm chữ V phía trên. Lối đi giữa (world x = 2.5) cố ý bỏ trống suốt từ cửa
        /// lên quầy để khách không phải len giữa hai dãy bàn.
        /// </summary>
        private static readonly SlotPlan[] Layout =
        {
            // --- Hàng trong cùng: quầy pha chế ---------------------------------------------------
            // Quầy dài 3 ô nằm ngang, chiếm trọn hàng sát tường sau.
            new(CounterSlotId, DecorCategory.CounterStation, new Vector2Int(1, -2), false,
                "quầy bar, chiếm 3 ô hàng trong cùng"),

            // --- Đồ cao kê vào hai góc bên -------------------------------------------------------
            // Cố ý KHÔNG kê gì dọc hàng sát tường sau: đồ cao ở đó che mất mảng tường, mà tường là
            // chỗ duy nhất treo được tranh. Hai góc trái/phải vừa trống vừa không chắn lối đi.
            new("slot_decor_01", DecorCategory.Appliance, new Vector2Int(-4, -1), false,
                "tủ bánh / máy lạnh, góc trái"),
            new("slot_plant_01", DecorCategory.FloorDecor, new Vector2Int(2, -9), false,
                "chậu cây, góc phải"),

            // --- Hai dãy bàn hai bên lối đi ------------------------------------------------------
            new("slot_table_01", DecorCategory.SeatingSet, new Vector2Int(-2, -3), true,
                "bàn giữa, bên trái lối đi"),
            new("slot_table_02", DecorCategory.SeatingSet, new Vector2Int(1, -6), true,
                "bàn giữa, bên phải lối đi"),
            new("slot_table_03", DecorCategory.SeatingSet, new Vector2Int(-4, -3), true,
                "bàn sát tường trái"),
            new("slot_table_04", DecorCategory.SeatingSet, new Vector2Int(-3, -6), true,
                "bàn khởi đầu, trái cửa"),
            new("slot_table_05", DecorCategory.SeatingSet, new Vector2Int(-1, -8), true,
                "bàn khởi đầu, phải cửa"),

            // --- Hai bồn hoa ôm cửa ra vào --------------------------------------------------------
            // Chừa ô (-3,-8) ở giữa làm lối vào, bịt nốt là khách kẹt ngay chỗ spawn.
            new("slot_planter_01", DecorCategory.OutdoorPlanter, new Vector2Int(-4, -7), false,
                "bồn hoa, trái cửa"),
            new("slot_planter_02", DecorCategory.OutdoorPlanter, new Vector2Int(-2, -9), false,
                "bồn hoa, phải cửa"),

            // --- Tranh ảnh treo tường -------------------------------------------------------------
            // Tường không chia ô nên khai thẳng toạ độ, canh theo mép sàn của từng mảng tường: mảng
            // trái dốc lên (y = -0.5 + 0.5(x + 1.5)), mảng phải dốc xuống (y = 0.25 - 0.5(x - 2)).
            // Mỗi bức treo cao hơn mép sàn ~0.9 cho ngang tầm mắt. Khoảng giữa hai mảng bị hình cái
            // quầy che kín nên không đặt gì ở đó.
            new("slot_wall_01", new Vector2(-1.00f, 0.70f), WallPerspective.LeftWall, "tường trái"),
            new("slot_wall_02", new Vector2(3.70f, 0.35f), WallPerspective.RightWall, "tường phải, gần quầy"),
            new("slot_wall_new", new Vector2(4.85f, -0.22f), WallPerspective.RightWall, "tường phải, gần góc")
        };

        /// <summary>Ghế của bàn tròn nằm chéo trên hai bên tâm bàn — khớp với art.</summary>
        private static readonly Vector2[] SeatLocalPositions =
        {
            new(-0.5f, 0.25f),
            new(0.5f, 0.25f)
        };

        [MenuItem(MenuPath, priority = 30)]
        internal static void Rebuild()
        {
            var grid = Object.FindFirstObjectByType<ShopGrid>();
            if (grid == null)
            {
                Debug.LogError("[DecorLayout] Không thấy ShopGrid trong scene — mở SampleScene rồi chạy lại.");
                return;
            }

            var content = GameObject.Find(ContentPath);
            if (content == null)
            {
                Debug.LogError($"[DecorLayout] Không thấy '{ContentPath}' — scene không đúng cấu trúc.");
                return;
            }

            // Gộp cả lượt dựng thành MỘT bước Undo. Không gộp thì mỗi slot là một bước riêng, bấm
            // Ctrl+Z vài cái là bố cục rụng dần từng món mà nhìn không ra chuyện gì đang xảy ra.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Dựng lại bố cục slot");
            int undoGroup = Undo.GetCurrentGroup();

            RemoveExistingSlots();

            var built = new List<DecorSlot>(Layout.Length);
            var report = new System.Text.StringBuilder("[DecorLayout] Đã dựng lại bố cục:");

            foreach (var plan in Layout)
            {
                var slot = Build(plan, content.transform, grid);
                if (slot == null) continue;

                built.Add(slot);
                report.AppendLine();
                report.Append($"  {plan.Id,-18} {slot.transform.position.ToString("0.00"),-22} {plan.Note}");
            }

            WireCounterPoint(built, grid);
            WireManagers(built);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(content.scene);
            grid.RebuildFromScene();
            DecorDefaultPlacementPreview.Refresh();

            report.AppendLine();
            report.Append($"  Tổng {built.Count} slot. Nhớ Ctrl+S để lưu scene.");
            Debug.Log(report.ToString());
        }

        private static void RemoveExistingSlots()
        {
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot != null) Undo.DestroyObjectImmediate(slot.gameObject);
            }
        }

        private static DecorSlot Build(SlotPlan plan, Transform parent, ShopGrid grid)
        {
            string templatePath = plan.IsWall ? WallTemplate : FloorTemplate;
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null)
            {
                Debug.LogError($"[DecorLayout] Thiếu template {templatePath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template, parent);
            instance.name = plan.Id;
            instance.transform.position = plan.IsWall
                ? new Vector3(plan.WallPosition.x, plan.WallPosition.y, 0f)
                : grid.CellCenter(plan.AnchorCell);
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var slot = instance.GetComponent<DecorSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("_slotId").stringValue = plan.Id;
            so.FindProperty("_allowedCategory").enumValueIndex = (int)plan.Category;
            so.FindProperty("_requiredZone").enumValueIndex = (int)ExpansionZoneId.Starter_Zone1;
            so.FindProperty("_wallPerspective").enumValueIndex = (int)plan.Perspective;

            // Chỉ bàn ghế mới khai điểm ngồi. Slot khác vẫn giữ object Seat_x của template cho đồng
            // bộ, nhưng bỏ trống mảng để hệ khách hàng không mời khách ngồi lên cái tủ bánh.
            var seats = so.FindProperty("_seatAnchors");
            seats.arraySize = plan.HasSeats ? SeatLocalPositions.Length : 0;
            for (int i = 0; i < seats.arraySize; i++)
            {
                var anchor = instance.transform.Find($"Seat_{i + 1}");
                if (anchor == null) continue;

                anchor.localPosition = SeatLocalPositions[i];
                seats.GetArrayElementAtIndex(i).objectReferenceValue = anchor;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(instance, "Dựng bố cục slot");
            return slot;
        }

        /// <summary>
        /// Dựng lại chỗ đứng gọi món ngay trước quầy. Gắn vào chính slot quầy nên dời quầy là chỗ
        /// đứng dời theo, không bị bỏ quên ở toạ độ cũ.
        /// </summary>
        private static void WireCounterPoint(List<DecorSlot> slots, ShopGrid grid)
        {
            DecorSlot counter = null;
            foreach (var slot in slots)
            {
                if (slot.SlotId == CounterSlotId) { counter = slot; break; }
            }
            if (counter == null) return;

            var host = new GameObject("OrderCounterPoint");
            host.transform.SetParent(counter.transform, false);
            host.transform.localPosition = Vector3.zero;

            var point = host.AddComponent<OrderCounterPoint>();
            var stands = new List<Transform>(CustomerSceneSetup.CounterStandPositions.Length);

            for (int i = 0; i < CustomerSceneSetup.CounterStandPositions.Length; i++)
            {
                var stand = new GameObject($"Stand_{i + 1:00}");
                stand.transform.SetParent(host.transform, false);
                stand.transform.position = CustomerSceneSetup.CounterStandPositions[i];
                stands.Add(stand.transform);
            }

            var so = new SerializedObject(point);
            var prop = so.FindProperty("_standPoints");
            prop.arraySize = stands.Count;
            for (int i = 0; i < stands.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = stands[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var manager = Object.FindFirstObjectByType<CustomerSceneManager>();
            if (manager == null) return;

            var mso = new SerializedObject(manager);
            var counters = mso.FindProperty("_counters");
            counters.arraySize = 1;
            counters.GetArrayElementAtIndex(0).objectReferenceValue = point;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Nối danh sách slot vào khu vực và vào DecorSceneManager — bỏ bước này là mở khoá
        /// khu vực xong slot vẫn tắt ngóm, và bảng chọn nội thất không thấy chỗ nào để đặt.</summary>
        private static void WireManagers(List<DecorSlot> slots)
        {
            var zone = Object.FindFirstObjectByType<ExpansionZoneView>();
            if (zone != null) WriteSlotArray(new SerializedObject(zone), "_slots", slots);

            var manager = Object.FindFirstObjectByType<DecorSceneManager>();
            if (manager != null) WriteSlotArray(new SerializedObject(manager), "_slots", slots);
        }

        private static void WriteSlotArray(SerializedObject so, string field, List<DecorSlot> slots)
        {
            var prop = so.FindProperty(field);
            prop.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
