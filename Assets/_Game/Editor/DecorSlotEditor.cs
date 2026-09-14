using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// Custom Editor cho DecorSlot: Cung cấp "Visual Offset Studio" giúp designer căn chỉnh
    /// tọa độ spawn riêng của từng món nội thất trực quan ngay trên Scene View bằng mũi tên kéo thả (Handles).
    /// </summary>
    [CustomEditor(typeof(DecorSlot))]
    public sealed class DecorSlotEditor : UnityEditor.Editor
    {
        private const string RepoAssetPath = "Assets/_Game/Data/Decor/DecorRepository.asset";
        private const string PreviewObjectName = "__OFFSET_STUDIO_PREVIEW__";

        private DecorSlot _slot;
        private ScriptableDecorRepository _repo;
        private DecorItem _activePreviewItem;
        private GameObject _previewInstance;
        private bool _showAllCategories = false;
        private bool _showConfiguredList = true;
        private Vector2 _scrollPos;

        private void OnEnable()
        {
            _slot = (DecorSlot)target;
            LoadRepository();
            CleanupStrayPreviews();
        }

        private void OnDisable()
        {
            StopPreview();
        }

        private void LoadRepository()
        {
            if (_repo == null)
            {
                _repo = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>(RepoAssetPath);
            }
        }

        private void CleanupStrayPreviews()
        {
            if (_slot == null || _slot.MountPoint == null) return;

            Transform mount = _slot.MountPoint;
            for (int i = mount.childCount - 1; i >= 0; i--)
            {
                Transform child = mount.GetChild(i);
                if (child != null && child.name.StartsWith("__OFFSET_STUDIO_PREVIEW__"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // 1. Vẽ các thuộc tính mặc định của DecorSlot
            DrawDefaultInspector();

            EditorGUILayout.Space(12);

            // 2. Tiêu đề Studio
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 BỘ CĂN CHỈNH TỌA ĐỘ NỘI THẤT (OFFSET STUDIO)", headerStyle);
            EditorGUILayout.LabelField("Kéo thả mũi tên trực tiếp trên Scene View để tinh chỉnh vị trí từng món đồ.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2.5 Góc nhìn tường (nếu là slot tường)
            DrawWallPerspectiveBar();

            // 3. Khu vực đang Preview / Căn chỉnh
            DrawActivePreviewPanel();

            EditorGUILayout.Space(8);

            // 4. Danh sách các món nội thất để chọn xem trước
            DrawItemSelectionList();

            EditorGUILayout.Space(8);

            // 5. Danh sách các món đã cấu hình offset riêng trên slot này
            DrawConfiguredOffsetsSummary();
        }

        private void DrawWallPerspectiveBar()
        {
            if (_slot == null || _slot.AllowedCategory != DecorCategory.WallDecor) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            bool isLeft = _slot.WallPerspectiveSetting == WallPerspective.LeftWall;
            EditorGUILayout.LabelField("Góc nhìn tường:", EditorStyles.boldLabel, GUILayout.Width(110));

            string btnText = isLeft
                ? "◀ TƯỜNG TRÁI (Slope +0.5)"
                : "TƯỜNG PHẢI (Slope -0.5 / Lật Y) ▶";

            GUI.backgroundColor = isLeft ? new Color(0.7f, 0.9f, 1f) : new Color(1f, 0.85f, 0.6f);
            if (GUILayout.Button(btnText, GUILayout.Height(26)))
            {
                Undo.RecordObject(_slot, "Toggle Wall Perspective");
                _slot.ToggleWallPerspective();
                EditorUtility.SetDirty(_slot);

                if (_previewInstance != null && _activePreviewItem != null)
                {
                    _slot.ApplyWallPerspectiveToInstance(_previewInstance, _activePreviewItem);
                }

                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(isLeft
                ? "• Tường trái/sau: Tranh dốc lên +26.5° sang phải."
                : "• Tường phải: Tranh dốc xuống -26.5° sang phải (hoặc lật Y 180°).", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private void DrawActivePreviewPanel()
        {
            if (_activePreviewItem == null || _previewInstance == null)
            {
                EditorGUILayout.HelpBox("Chưa có món nào đang được xem trước. Hãy chọn một món nội thất bên dưới để bắt đầu căn chỉnh trên Scene View.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header panel active
            GUIStyle activeTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.4f) }
            };
            EditorGUILayout.LabelField($"🔍 ĐANG CĂN CHỈNH: {_activePreviewItem.DisplayName}", activeTitleStyle);
            EditorGUILayout.LabelField($"Mã Item: {_activePreviewItem.Id} | Danh mục: {_activePreviewItem.Category}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // Nhập số liệu trực tiếp
            Vector2 currentOffset = _slot.GetEffectiveOffset(_activePreviewItem);
            EditorGUI.BeginChangeCheck();
            Vector2 newOffset = EditorGUILayout.Vector2Field("Tọa độ Offset (Local X, Y):", currentOffset);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyOffsetChange(newOffset);
            }

            EditorGUILayout.Space(4);

            // Các nút thao tác nhanh
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset về (0, 0)", GUILayout.Height(24)))
            {
                ApplyOffsetChange(Vector2.zero);
            }

            if (_slot.HasCustomOffset(_activePreviewItem.Id))
            {
                if (GUILayout.Button("Dùng Offset mặc định của Item", GUILayout.Height(24)))
                {
                    Undo.RecordObject(_slot, "Remove Custom Offset");
                    _slot.RemoveItemOffset(_activePreviewItem.Id);
                    EditorUtility.SetDirty(_slot);
                    UpdatePreviewPosition();
                }
            }

            if (GUILayout.Button("Lưu làm Mặc định cho Item", GUILayout.Height(24)))
            {
                SaveAsItemDefaultOffset(_activePreviewItem, currentOffset);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
            if (GUILayout.Button("✓ HOÀN TẤT & ĐÓNG XEM TRƯỚC", GUILayout.Height(30)))
            {
                StopPreview();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawItemSelectionList()
        {
            if (_repo == null)
            {
                EditorGUILayout.HelpBox($"Không tìm thấy DecorRepository tại '{RepoAssetPath}'.", MessageType.Warning);
                if (GUILayout.Button("Thử tải lại DecorRepository"))
                {
                    LoadRepository();
                }
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📦 Danh mục nội thất tương thích:", EditorStyles.boldLabel);
            _showAllCategories = EditorGUILayout.ToggleLeft("Xem toàn bộ item", _showAllCategories, GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();

            DecorItem[] items = _showAllCategories
                ? _repo.GetAllDecor()
                : _repo.GetDecorByCategory(_slot.AllowedCategory);

            if (items == null || items.Length == 0)
            {
                EditorGUILayout.LabelField($"Không có item nào thuộc danh mục {_slot.AllowedCategory}.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(220));

            foreach (var item in items)
            {
                if (item == null) continue;

                bool isCurrentPreview = (_activePreviewItem != null && _activePreviewItem.Id == item.Id);
                bool hasCustom = _slot.HasCustomOffset(item.Id);
                Vector2 effOffset = _slot.GetEffectiveOffset(item);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Icon nếu có
                if (item.Icon != null)
                {
                    GUILayout.Label(AssetPreview.GetAssetPreview(item.Icon) ?? item.Icon.texture, GUILayout.Width(28), GUILayout.Height(28));
                }

                // Thông tin tên và offset hiện tại
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(item.DisplayName, EditorStyles.boldLabel);
                string offsetStatus = hasCustom
                    ? $"[Đã chỉnh riêng: ({effOffset.x:F2}, {effOffset.y:F2})]"
                    : $"[Mặc định: ({item.DefaultSpawnOffset.x:F2}, {item.DefaultSpawnOffset.y:F2})]";
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = hasCustom ? new Color(0.1f, 0.7f, 0.3f) : Color.gray }
                };
                EditorGUILayout.LabelField(offsetStatus, statusStyle);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Nút Preview / Căn chỉnh
                if (isCurrentPreview)
                {
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("✕ Đóng", GUILayout.Width(65), GUILayout.Height(26)))
                    {
                        StopPreview();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = hasCustom ? new Color(0.7f, 0.9f, 1f) : Color.white;
                    if (GUILayout.Button("👁 Căn chỉnh", GUILayout.Width(85), GUILayout.Height(26)))
                    {
                        StartPreview(item);
                    }
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawConfiguredOffsetsSummary()
        {
            var offsets = _slot.ItemOffsets;
            if (offsets == null || offsets.Count == 0) return;

            _showConfiguredList = EditorGUILayout.Foldout(_showConfiguredList, $"📋 Danh sách offset đã lưu trên slot này ({offsets.Count} món)", true);
            if (!_showConfiguredList) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string itemToRemove = null;

            for (int i = 0; i < offsets.Count; i++)
            {
                var entry = offsets[i];
                if (entry == null) continue;

                var decorItem = _repo != null ? _repo.GetDecor(entry.itemId) : null;
                string displayName = decorItem != null ? decorItem.DisplayName : entry.itemId;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {displayName}: ({entry.offset.x:F3}, {entry.offset.y:F3})", EditorStyles.label);

                if (GUILayout.Button("👁", GUILayout.Width(28), GUILayout.Height(18)))
                {
                    if (decorItem != null) StartPreview(decorItem);
                }

                if (GUILayout.Button("Xóa", GUILayout.Width(45), GUILayout.Height(18)))
                {
                    itemToRemove = entry.itemId;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(itemToRemove))
            {
                Undo.RecordObject(_slot, "Remove Item Offset");
                _slot.RemoveItemOffset(itemToRemove);
                EditorUtility.SetDirty(_slot);
                if (_activePreviewItem != null && _activePreviewItem.Id == itemToRemove)
                {
                    UpdatePreviewPosition();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI()
        {
            if (_activePreviewItem == null || _previewInstance == null || _slot == null) return;

            Transform mount = _slot.MountPoint;
            Vector2 currentOffset = _slot.GetEffectiveOffset(_activePreviewItem);
            Vector3 worldPos = mount.TransformPoint(new Vector3(currentOffset.x, currentOffset.y, 0f));

            // Vẽ chỉ báo tâm slot (MountPoint)
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Handles.DrawWireDisc(mount.position, Vector3.forward, 0.15f);
            Handles.DrawDottedLine(mount.position, worldPos, 4f);

            // Vẽ nhãn thông tin tọa độ tại vị trí item
            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            Handles.Label(worldPos + Vector3.up * 0.45f, $"[{_activePreviewItem.DisplayName}]\nOffset: ({currentOffset.x:F3}, {currentOffset.y:F3})", labelStyle);

            // Position Handle để designer nắm kéo trực tiếp
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, mount.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 newLocalPos = mount.InverseTransformPoint(newWorldPos);
                Vector2 newOffset = new Vector2(
                    Mathf.Round(newLocalPos.x * 1000f) / 1000f,
                    Mathf.Round(newLocalPos.y * 1000f) / 1000f
                );

                ApplyOffsetChange(newOffset);
            }
        }

        private void StartPreview(DecorItem item)
        {
            if (item == null || item.Prefab == null)
            {
                Debug.LogWarning($"[DecorSlotEditor] Item '{item?.name}' không có Prefab để xem trước.");
                return;
            }

            StopPreview();

            _activePreviewItem = item;
            Transform mount = _slot.MountPoint;

            // Nếu slot đang có một item hiển thị sẵn (ví dụ trong Play Mode), tạm ẩn đi để preview item mới
            if (_slot.SpawnedInstance != null)
            {
                _slot.SpawnedInstance.SetActive(false);
            }

            _previewInstance = Instantiate(item.Prefab, mount);
            _previewInstance.name = PreviewObjectName;
            _previewInstance.hideFlags = HideFlags.DontSave;

            UpdatePreviewPosition();

            // Áp dụng góc nhìn tường nếu là slot tường
            if (_slot.AllowedCategory == DecorCategory.WallDecor)
            {
                _slot.ApplyWallPerspectiveToInstance(_previewInstance, item);
            }

            // Áp dụng sorting layer (chậu hoa lên trước bàn ghế)
            _slot.ApplySortingToInstance(_previewInstance);

            if (_slot.EmptyIndicator != null)
            {
                _slot.EmptyIndicator.SetActive(false);
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void UpdatePreviewPosition()
        {
            if (_previewInstance != null && _activePreviewItem != null)
            {
                Vector2 effOffset = _slot.GetEffectiveOffset(_activePreviewItem);
                _previewInstance.transform.localPosition = new Vector3(effOffset.x, effOffset.y, 0f);
            }
        }

        private void ApplyOffsetChange(Vector2 newOffset)
        {
            if (_activePreviewItem == null) return;

            Undo.RecordObject(_slot, "Adjust Decor Item Spawn Offset");
            _slot.SetItemOffset(_activePreviewItem.Id, newOffset);
            EditorUtility.SetDirty(_slot);

            UpdatePreviewPosition();
            SceneView.RepaintAll();
            Repaint();
        }

        private void SaveAsItemDefaultOffset(DecorItem item, Vector2 offset)
        {
            if (item == null) return;

            SerializedObject so = new SerializedObject(item);
            SerializedProperty prop = so.FindProperty("_defaultSpawnOffset");
            if (prop != null)
            {
                so.Update();
                prop.vector2Value = offset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssets();
                Debug.Log($"[DecorSlotEditor] Đã lưu offset mặc định ({offset.x:F3}, {offset.y:F3}) cho ScriptableObject '{item.DisplayName}'.");
            }
        }

        private void StopPreview()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _activePreviewItem = null;

            if (_slot != null)
            {
                // Khôi phục item hiển thị ban đầu nếu có
                if (_slot.SpawnedInstance != null)
                {
                    _slot.SpawnedInstance.SetActive(true);
                }

                if (_slot.EmptyIndicator != null)
                {
                    _slot.EmptyIndicator.SetActive(_slot.CurrentDecorItem == null);
                }
            }

            SceneView.RepaintAll();
        }
    }
}
