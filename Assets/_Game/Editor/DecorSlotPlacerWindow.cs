using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Cửa sổ xếp chỗ đặt nội thất (<see cref="DecorSlot"/>) **trực tiếp lên tilemap sàn** trong
    /// Scene view: rê chuột lên sàn là thấy ô đang trỏ và vết chân món đồ sẽ chiếm, bấm một cái là
    /// có slot nằm đúng tâm ô đó.
    ///
    /// Bấm đâu đồ nằm đó — ô bạn bấm chính là ô món đồ được vẽ giữa. Riêng **ô bị chiếm** (ô khách
    /// phải né) thì thường tụt xuống ô chéo dưới, vì art trong bộ tài nguyên này ngồi hơi cao trong
    /// khung của nó nên điểm chạm sàn nằm thấp hơn tâm. Vết chân đó được vẽ sẵn bằng ô đỏ/xanh lúc
    /// rê chuột, lấy thẳng từ <see cref="GridOccupant"/> của prefab nên đúng y lúc chạy game.
    ///
    /// Menu: Tools > DreamCafe > Xếp slot lên sàn
    /// </summary>
    public sealed class DecorSlotPlacerWindow : EditorWindow
    {
        private enum Mode
        {
            /// <summary>Bấm vào ô trống để thêm slot mới.</summary>
            Place = 0,

            /// <summary>Bấm vào slot để nhấc lên, bấm ô khác để thả xuống.</summary>
            Move = 1,

            /// <summary>Bấm vào slot để gỡ khỏi scene.</summary>
            Remove = 2
        }

        private const string ContentPath = "ShopLayout/Zone1_Starter/Content";
        private const string FloorTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Floor_Template.prefab";
        private const string WallTemplate = "Assets/_Game/Prefabs/Decor/Prefab_Slot_Wall_Template.prefab";

        private static readonly Color FloorTint = new(1f, 1f, 1f, 0.10f);
        private static readonly Color HoverTint = new(0.35f, 0.9f, 1f, 0.55f);
        private static readonly Color BodyTint = new(0.95f, 0.35f, 0.25f, 0.55f);
        private static readonly Color SeatTint = new(0.3f, 0.85f, 1f, 0.45f);
        private static readonly Color SlotTint = new(1f, 0.85f, 0.15f, 0.35f);
        private static readonly Color ClashTint = new(1f, 0.1f, 0.1f, 0.7f);

        private Mode _mode = Mode.Place;
        private DecorCategory _category = DecorCategory.SeatingSet;
        private ExpansionZoneId _zone = ExpansionZoneId.Starter_Zone1;
        private WallPerspective _perspective = WallPerspective.LeftWall;
        private DecorItem[] _catalog = System.Array.Empty<DecorItem>();
        private int _itemIndex;
        private string _slotId = string.Empty;
        private bool _autoId = true;
        private bool _drawAllCells = true;

        private ShopGrid _grid;
        private DecorSlot _carried;
        private Vector3Int _hoverCell;
        private bool _hoverValid;

        [MenuItem("Tools/DreamCafe/Xếp slot lên sàn", priority = 1)]
        private static void Open()
        {
            var window = GetWindow<DecorSlotPlacerWindow>(false, "Xếp slot", true);
            window.minSize = new Vector2(320f, 380f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshCatalog();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _carried = null;
        }

        /// <summary>Lưới đang dùng. Tìm lại mỗi lần vì đổi scene là tham chiếu cũ chết.</summary>
        private ShopGrid Grid
        {
            get
            {
                if (_grid == null) _grid = FindFirstObjectByType<ShopGrid>();
                return _grid;
            }
        }

        private DecorItem SelectedItem =>
            _catalog.Length > 0 ? _catalog[Mathf.Clamp(_itemIndex, 0, _catalog.Length - 1)] : null;

        private bool IsWallMode => _category == DecorCategory.WallDecor;

        // =====================================================================
        // BẢNG ĐIỀU KHIỂN
        // =====================================================================

        private void OnGUI()
        {
            if (Grid == null)
            {
                EditorGUILayout.HelpBox("Không thấy ShopGrid trong scene. Mở SampleScene rồi thử lại.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Chế độ", EditorStyles.boldLabel);
            var mode = (Mode)GUILayout.Toolbar((int)_mode, new[] { "Đặt", "Di chuyển", "Xoá" });
            if (mode != _mode)
            {
                _mode = mode;
                _carried = null;
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Slot sắp đặt", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _category = (DecorCategory)EditorGUILayout.EnumPopup("Loại đồ cho phép", _category);
            if (EditorGUI.EndChangeCheck()) RefreshCatalog();

            _zone = (ExpansionZoneId)EditorGUILayout.EnumPopup("Khu vực", _zone);

            if (IsWallMode)
            {
                _perspective = (WallPerspective)EditorGUILayout.EnumPopup("Mặt tường", _perspective);
                EditorGUILayout.HelpBox(
                    "Đồ treo tường không bám ô: bấm thẳng vào chỗ muốn treo trên mảng tường.",
                    MessageType.Info);
            }
            else if (_catalog.Length > 0)
            {
                var names = new string[_catalog.Length];
                for (int i = 0; i < _catalog.Length; i++) names[i] = _catalog[i].DisplayName;
                _itemIndex = EditorGUILayout.Popup("Xem trước theo món", Mathf.Clamp(_itemIndex, 0, names.Length - 1), names);
                EditorGUILayout.LabelField(" ", FootprintSummary(), EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("Chưa có món nội thất nào thuộc loại này trong kho dữ liệu.", MessageType.Warning);
            }

            _autoId = EditorGUILayout.Toggle("Tự đánh mã slot", _autoId);
            using (new EditorGUI.DisabledScope(_autoId))
            {
                _slotId = EditorGUILayout.TextField("Mã slot", _autoId ? NextSlotId() : _slotId);
            }

            EditorGUILayout.Space(6f);
            _drawAllCells = EditorGUILayout.Toggle("Vẽ cả lưới sàn", _drawAllCells);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                _hoverValid
                    ? $"Ô đang trỏ: ({_hoverCell.x}, {_hoverCell.y}) → {Grid.CellCenter(_hoverCell):0.00}"
                    : "Ô đang trỏ: (ngoài sàn)",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Đang có {CountSlots()} slot trong scene", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dựng lại bản đồ ô")) RefreshScene();
                if (GUILayout.Button("Nối lại danh sách")) { WireManagers(); RefreshScene(); }
            }

            if (GUILayout.Button("Về bố cục chuẩn"))
            {
                if (EditorUtility.DisplayDialog("Xếp lại từ đầu?",
                        "Toàn bộ slot hiện tại sẽ bị xoá và dựng lại theo bảng bố cục chuẩn. Không giữ lại chỉnh tay.",
                        "Xếp lại", "Thôi"))
                {
                    DecorStarterLayoutTool.Rebuild();
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                _mode switch
                {
                    Mode.Place => "Bấm ô nào thì món đồ đứng đúng ô đó. Ô đỏ là phần đồ chiếm chỗ (khách phải né), ô xanh là chỗ ngồi. Muốn nhích riêng hình vẽ thì sửa _artOffset trong prefab, ô không đổi theo.",
                    Mode.Move => _carried != null
                        ? $"Đang cầm '{_carried.SlotId}'. Bấm ô mới để thả, Esc để bỏ."
                        : "Bấm vào một slot để nhấc lên.",
                    _ => "Bấm vào một slot để xoá."
                },
                MessageType.None);
        }

        private string FootprintSummary()
        {
            var occupant = OccupantOf(SelectedItem);
            if (occupant == null) return "Món này không khai ô — slot sẽ chiếm đúng ô bạn bấm.";

            return $"Chiếm {occupant.BlockedCells.Count} ô thân" +
                   (occupant.SeatCount > 0 ? $" + {occupant.SeatCount} ô ghế." : ".");
        }

        // =====================================================================
        // TƯƠNG TÁC TRONG SCENE VIEW
        // =====================================================================

        private void OnSceneGUI(SceneView view)
        {
            if (Grid == null) return;

            int control = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(control);

            var e = Event.current;
            _hoverValid = TryPointerCell(e.mousePosition, out _hoverCell, out Vector3 pointerWorld);

            // Scene view chỉ vẽ lại khi có việc, nên nếu không tự xin vẽ lại thì ô sáng đứng im ở
            // chỗ cũ trong khi con trỏ đã đi tiếp — nhìn hệt như công cụ đặt lệch ô, dù toạ độ lúc
            // bấm vẫn đúng.
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                view.Repaint();
                Repaint();
            }

            if (e.type == EventType.Repaint) Draw(pointerWorld);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _carried != null)
            {
                _carried = null;
                e.Use();
                Repaint();
            }

            if (e.type != EventType.MouseDown || e.button != 0 || e.alt) return;

            Click(pointerWorld);
            e.Use();
            view.Repaint();
            Repaint();
        }

        private void Click(Vector3 pointerWorld)
        {
            switch (_mode)
            {
                case Mode.Place:
                    Place(pointerWorld);
                    break;

                case Mode.Move:
                    if (_carried == null) _carried = SlotUnder(pointerWorld);
                    else { MoveCarried(pointerWorld); _carried = null; }
                    break;

                case Mode.Remove:
                    var victim = SlotUnder(pointerWorld);
                    if (victim != null)
                    {
                        Undo.DestroyObjectImmediate(victim.gameObject);
                        WireManagers();
                        RefreshScene();
                    }
                    break;
            }
        }

        private void Place(Vector3 pointerWorld)
        {
            var parent = GameObject.Find(ContentPath);
            if (parent == null)
            {
                Debug.LogError($"[XếpSlot] Không thấy '{ContentPath}' trong scene.");
                return;
            }

            string templatePath = IsWallMode ? WallTemplate : FloorTemplate;
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null)
            {
                Debug.LogError($"[XếpSlot] Thiếu template {templatePath}");
                return;
            }

            if (!IsWallMode)
            {
                if (!_hoverValid) return;
                if (Clashes(AnchorFor(_hoverCell), null))
                {
                    Debug.LogWarning("[XếpSlot] Ô này đã có đồ khác chiếm — chọn chỗ khác cho khỏi đè nhau.");
                    return;
                }
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template, parent.transform);
            instance.name = _autoId ? NextSlotId() : _slotId;
            instance.transform.position = IsWallMode
                ? new Vector3(pointerWorld.x, pointerWorld.y, 0f)
                : Grid.CellCenter(AnchorFor(_hoverCell));

            // Ép về không xoay, không co giãn. Một slot lỡ bị xoay vài độ thì mọi thứ gắn lên nó
            // lệch theo, mà nhìn trong Scene gần như không nhận ra.
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Configure(instance.GetComponent<DecorSlot>(), instance.name);
            Undo.RegisterCreatedObjectUndo(instance, "Đặt decor slot");

            Selection.activeGameObject = instance;
            WireManagers();
            RefreshScene();
        }

        private void MoveCarried(Vector3 pointerWorld)
        {
            if (_carried == null) return;

            bool wall = _carried.AllowedCategory == DecorCategory.WallDecor;
            if (!wall && !_hoverValid) return;

            Vector3 target = wall
                ? new Vector3(pointerWorld.x, pointerWorld.y, 0f)
                : Grid.CellCenter(AnchorFor(_hoverCell));

            Undo.RecordObject(_carried.transform, "Dời decor slot");
            _carried.transform.position = target;
            EditorUtility.SetDirty(_carried.transform);
            RefreshScene();
        }

        /// <summary>
        /// Điền dữ liệu riêng cho slot mới. Ghế chỉ khai cho bàn — slot khác vẫn giữ object Seat_x
        /// của template nhưng bỏ trống mảng, không thì hệ khách hàng mời khách ngồi lên cái tủ lạnh.
        /// </summary>
        private void Configure(DecorSlot slot, string id)
        {
            var so = new SerializedObject(slot);
            so.FindProperty("_slotId").stringValue = id;
            so.FindProperty("_allowedCategory").enumValueIndex = (int)_category;
            so.FindProperty("_requiredZone").enumValueIndex = (int)_zone;
            so.FindProperty("_wallPerspective").enumValueIndex = (int)_perspective;

            bool seating = _category == DecorCategory.SeatingSet;
            var seats = so.FindProperty("_seatAnchors");
            seats.arraySize = seating ? 2 : 0;
            for (int i = 0; i < seats.arraySize; i++)
            {
                var anchor = slot.transform.Find($"Seat_{i + 1}");
                if (anchor != null) seats.GetArrayElementAtIndex(i).objectReferenceValue = anchor;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // VẼ
        // =====================================================================

        private void Draw(Vector3 pointerWorld)
        {
            var grid = Grid;
            var floorCells = FloorCells();

            if (_drawAllCells)
            {
                foreach (var cell in floorCells) DrawCell(cell, FloorTint);
            }

            // Ô đang bị chiếm: vẽ trước để vết chân xem trước nằm đè lên trên, thấy ngay chỗ đụng.
            foreach (var pair in grid.Cells)
            {
                DrawCell(pair.Key, pair.Value == ShopCellKind.Furniture ? BodyTint : SeatTint);
            }

            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot.AllowedCategory == DecorCategory.WallDecor) continue;
                DrawCell(grid.WorldToCell(slot.transform.position), SlotTint);
            }

            if (IsWallMode)
            {
                Handles.color = HoverTint;
                Handles.DrawWireDisc(new Vector3(pointerWorld.x, pointerWorld.y, 0f), Vector3.forward, 0.18f);
                return;
            }

            if (!_hoverValid) return;

            var anchor = AnchorFor(_hoverCell);
            bool clash = _mode == Mode.Place && Clashes(anchor, null);
            DrawCell(_hoverCell, clash ? ClashTint : HoverTint);
            DrawPreviewFootprint(anchor, clash);

            // Chấm trắng = đúng điểm slot sẽ rơi xuống. Có nó thì khỏi phải đoán xem ô sáng đang
            // nói về tâm ô nào.
            Vector3 drop = Grid.CellCenter(anchor);
            Handles.color = Color.white;
            Handles.DrawLine(drop + Vector3.left * 0.12f, drop + Vector3.right * 0.12f);
            Handles.DrawLine(drop + Vector3.down * 0.06f, drop + Vector3.up * 0.06f);
        }

        /// <summary>Vẽ đúng những ô mà món đang chọn sẽ chiếm nếu thả xuống đây.</summary>
        private void DrawPreviewFootprint(Vector3Int anchor, bool clash)
        {
            var occupant = OccupantOf(SelectedItem);
            if (occupant == null) return;

            Color body = clash ? ClashTint : BodyTint;
            foreach (var offset in occupant.BlockedCells)
            {
                DrawCell(new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, 0), body);
            }

            for (int i = 0; i < occupant.SeatCount; i++)
            {
                var offset = occupant.SeatCellOffset(i);
                DrawCell(new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, 0), SeatTint);
            }
        }

        private void DrawCell(Vector3Int cell, Color color)
        {
            var grid = Grid;
            Vector3 c = grid.CellCenter(cell);
            Vector3 half = grid.CellHalfExtents;

            Handles.color = color;
            Handles.DrawAAConvexPolygon(
                c + Vector3.left * half.x,
                c + Vector3.up * half.y,
                c + Vector3.right * half.x,
                c + Vector3.down * half.y);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Ô gốc của slot = đúng ô vừa bấm, và cũng là **ô món đồ đứng lên**: mọi prefab nội thất
        /// đều đã quy về <c>_blockedCells[0] = (0,0)</c>. Hình vẽ nhô lên khỏi ô bao nhiêu là do
        /// <c>GridOccupant._artOffset</c> của riêng prefab, không ảnh hưởng tới ô.
        /// </summary>
        private Vector3Int AnchorFor(Vector3Int clickedCell) => clickedCell;

        /// <summary>Đặt ở đây thì có đè lên slot khác, hay đè lên ô đồ đang có không.</summary>
        private bool Clashes(Vector3Int anchor, DecorSlot ignore)
        {
            var grid = Grid;

            // Hai slot trống chồng nhau thì bản đồ ô không thấy gì cả (slot rỗng không chiếm ô nào),
            // nhưng mua đồ vào là hai món lồng lên nhau. Chặn ngay từ lúc xếp.
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot == ignore || slot.AllowedCategory == DecorCategory.WallDecor) continue;
                if (grid.WorldToCell(slot.transform.position) == anchor) return true;
            }

            var occupant = OccupantOf(SelectedItem);
            if (occupant == null) return grid.GetKind(anchor) == ShopCellKind.Furniture;

            // Ô thân: chỉ cấm đè lên đồ khác. KHÔNG cấm ô nằm ngoài sàn — chân art thường thò ra
            // khỏi mép sàn vài centimet, cấm luôn thì cả hai hàng ô sát mép trước không kê được gì.
            foreach (var o in occupant.BlockedCells)
            {
                if (grid.GetKind(new Vector3Int(anchor.x + o.x, anchor.y + o.y, 0)) == ShopCellKind.Furniture) return true;
            }

            // Ô ghế thì ngược lại, phải nằm trên sàn và đi vào được — ghế lọt ra ngoài sàn là khách
            // không bao giờ tới ngồi nổi, mà nhìn ngoài scene không thấy gì sai cả.
            for (int i = 0; i < occupant.SeatCount; i++)
            {
                var offset = occupant.SeatCellOffset(i);
                var cell = new Vector3Int(anchor.x + offset.x, anchor.y + offset.y, 0);
                if (!grid.IsFloor(cell) || grid.GetKind(cell) == ShopCellKind.Furniture) return true;
            }

            return false;
        }

        private DecorSlot SlotUnder(Vector3 world)
        {
            DecorSlot best = null;
            float bestDistance = float.MaxValue;

            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(slot.transform.position, world);
                if (d < bestDistance) { bestDistance = d; best = slot; }
            }

            // Bán kính nới rộng theo chiều ngang cho hợp hình thoi của ô isometric.
            return bestDistance <= 0.6f ? best : null;
        }

        /// <summary>Đổi vị trí chuột trong Scene view thành ô sàn đang trỏ tới.</summary>
        private bool TryPointerCell(Vector2 mouse, out Vector3Int cell, out Vector3 world)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            float t = Mathf.Approximately(ray.direction.z, 0f) ? 0f : -ray.origin.z / ray.direction.z;
            world = ray.origin + ray.direction * t;
            world.z = 0f;

            cell = Grid.WorldToCell(world);
            return Grid.IsFloor(cell);
        }

        private List<Vector3Int> FloorCells()
        {
            var cells = new List<Vector3Int>();
            var tilemap = Object.FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null) return cells;

            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cell)) cells.Add(cell);
            }
            return cells;
        }

        private GridOccupant OccupantOf(DecorItem item) =>
            item != null && item.Prefab != null ? item.Prefab.GetComponent<GridOccupant>() : null;

        private void RefreshCatalog()
        {
            var found = new List<DecorItem>();
            foreach (string guid in AssetDatabase.FindAssets("t:DecorItem"))
            {
                var item = AssetDatabase.LoadAssetAtPath<DecorItem>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && item.Category == _category) found.Add(item);
            }

            found.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            _catalog = found.ToArray();
            _itemIndex = 0;
        }

        private int CountSlots() =>
            Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        /// <summary>Mã slot kế tiếp chưa ai dùng, đặt theo loại đồ cho dễ đọc trong Hierarchy.</summary>
        private string NextSlotId()
        {
            string prefix = _category switch
            {
                DecorCategory.SeatingSet => "slot_table",
                DecorCategory.CounterStation => "slot_counter",
                DecorCategory.Appliance => "slot_decor",
                DecorCategory.FloorDecor => "slot_plant",
                DecorCategory.WallDecor => "slot_wall",
                _ => "slot_planter"
            };

            var used = new HashSet<string>();
            foreach (var slot in Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                used.Add(slot.SlotId);
            }

            for (int i = 1; i < 100; i++)
            {
                string candidate = $"{prefix}_{i:00}";
                if (!used.Contains(candidate)) return candidate;
            }
            return prefix + "_new";
        }

        /// <summary>Dựng lại bản đồ ô + preview để nhìn thấy kết quả ngay, rồi đánh dấu scene bẩn.</summary>
        private void RefreshScene()
        {
            DecorDefaultPlacementPreview.Refresh();
            if (Grid != null) Grid.RebuildFromScene();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Nối lại danh sách slot cho khu vực và cho DecorSceneManager. Thêm/xoá slot mà quên bước
        /// này thì slot mới không bao giờ bật lên khi mở khoá khu vực, còn slot đã xoá để lại ô
        /// trống null trong mảng.
        /// </summary>
        private void WireManagers()
        {
            var slots = new List<DecorSlot>(
                Object.FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            slots.Sort((a, b) => string.CompareOrdinal(a.SlotId, b.SlotId));

            var zone = Object.FindFirstObjectByType<ExpansionZoneView>();
            if (zone != null) WriteSlotArray(new SerializedObject(zone), slots);

            var manager = Object.FindFirstObjectByType<DecorSceneManager>();
            if (manager != null) WriteSlotArray(new SerializedObject(manager), slots);
        }

        private static void WriteSlotArray(SerializedObject so, List<DecorSlot> slots)
        {
            var prop = so.FindProperty("_slots");
            if (prop == null) return;

            prop.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
