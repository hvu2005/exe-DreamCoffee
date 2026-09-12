using DreamCafe.SystemControl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng sẵn Inventory Panel vào Canvas của scene đang mở + sinh prefab ô lưới.
    /// Chạy lại sẽ xoá panel cũ rồi dựng mới, nên sửa số liệu ở đây rồi chạy lại là được.
    /// </summary>
    public static class InventoryUISetup
    {
        private const string PanelName = "InventoryPanel";
        private const string SlotPrefabPath = "Assets/_Game/Prefabs/InventorySlot.prefab";

        private static readonly Color Cream = new(0.992f, 0.957f, 0.863f);
        private static readonly Color CardCream = new(1f, 0.99f, 0.949f);
        private static readonly Color Green = new(0.431f, 0.545f, 0.243f);
        private static readonly Color DarkBrown = new(0.290f, 0.212f, 0.125f);
        private static readonly Color MutedBrown = new(0.541f, 0.451f, 0.329f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.55f);

        private static readonly Vector2 CellSize = new(236f, 118f);
        private static readonly Vector2 DialogSize = new(780f, 500f);

        [MenuItem("DreamCafe/Setup/Inventory Panel UI")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Inventory Panel UI", "Không tìm thấy Canvas trong scene đang mở.", "OK");
                return;
            }

            var existing = canvas.transform.Find(PanelName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var slotPrefab = BuildSlotPrefab();
            var panel = BuildPanel(canvas.transform, slotPrefab);

            Undo.RegisterCreatedObjectUndo(panel, "Create Inventory Panel");
            Selection.activeGameObject = panel;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log($"[InventoryUISetup] Đã dựng '{PanelName}' trong Canvas và prefab tại {SlotPrefabPath}.");
        }

        // =====================================================================
        // Prefab ô lưới
        // =====================================================================

        private static InventorySlotView BuildSlotPrefab()
        {
            var root = NewUI("InventorySlot", null);
            SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, CellSize);
            AddImage(root, CardCream);

            var tag = NewUI("CategoryTag", root.transform);
            SetRect(tag, TopLeft, TopLeft, TopLeft, new Vector2(10f, -10f), new Vector2(108f, 20f));
            var tagImage = AddImage(tag, Green);
            var categoryLabel = AddText(NewUI("Label", tag.transform), "BASE", 11, Color.white, TextAlignmentOptions.Center);
            Stretch(categoryLabel.rectTransform);

            var iconGo = NewUI("Icon", root.transform);
            SetRect(iconGo, TopLeft, TopLeft, TopLeft, new Vector2(12f, -36f), new Vector2(64f, 64f));
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var nameLabel = AddText(NewUI("NameLabel", root.transform), "Item", 17, DarkBrown, TextAlignmentOptions.MidlineLeft);
            SetRect(nameLabel.gameObject, TopLeft, TopLeft, TopLeft, new Vector2(84f, -34f), new Vector2(142f, 24f));
            nameLabel.fontStyle = FontStyles.Bold;

            var infoLabel = AddText(NewUI("InfoLabel", root.transform), "Never expires", 11, MutedBrown, TextAlignmentOptions.MidlineLeft);
            SetRect(infoLabel.gameObject, TopLeft, TopLeft, TopLeft, new Vector2(84f, -58f), new Vector2(142f, 18f));

            var badge = NewUI("QuantityBadge", root.transform);
            SetRect(badge, BottomRight, BottomRight, BottomRight, new Vector2(-10f, 10f), new Vector2(96f, 26f));
            AddImage(badge, Green);
            var quantityLabel = AddText(NewUI("Label", badge.transform), "0", 13, Color.white, TextAlignmentOptions.Center);
            Stretch(quantityLabel.rectTransform);
            quantityLabel.fontStyle = FontStyles.Bold;

            var view = root.AddComponent<InventorySlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("categoryTag").objectReferenceValue = tagImage;
            so.FindProperty("categoryLabel").objectReferenceValue = categoryLabel;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("infoLabel").objectReferenceValue = infoLabel;
            so.FindProperty("quantityLabel").objectReferenceValue = quantityLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<InventorySlotView>();
        }

        // =====================================================================
        // Panel
        // =====================================================================

        private static GameObject BuildPanel(Transform canvas, InventorySlotView slotPrefab)
        {
            var panel = NewUI(PanelName, canvas);
            Stretch((RectTransform)panel.transform);

            var window = NewUI("Window", panel.transform);
            Stretch((RectTransform)window.transform);

            var dim = NewUI("Dim", window.transform);
            Stretch((RectTransform)dim.transform);
            AddImage(dim, Dim, rounded: false).raycastTarget = true;

            var dialog = NewUI("Dialog", window.transform);
            SetRect(dialog, Center, Center, Center, Vector2.zero, DialogSize);
            AddImage(dialog, Cream);

            // Header
            var header = NewUI("Header", dialog.transform);
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 60f);
            AddImage(header, Green);

            var title = AddText(NewUI("Title", header.transform), "INVENTORY", 26, Color.white, TextAlignmentOptions.Center);
            Stretch(title.rectTransform);
            title.fontStyle = FontStyles.Bold;

            var summary = AddText(NewUI("Summary", header.transform), "0 ingredients", 13,
                new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.MidlineRight);
            SetRect(summary.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-16f, 0f), new Vector2(180f, 30f));

            // Scroll view
            var scrollGo = NewUI("ScrollView", dialog.transform);
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(14f, 70f);
            scrollRect.offsetMax = new Vector2(-14f, -70f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 25f;

            var viewport = NewUI("Viewport", scrollGo.transform);
            Stretch((RectTransform)viewport.transform);
            viewport.AddComponent<RectMask2D>();
            // Graphic trong suốt nhưng vẫn nhận raycast — thiếu nó thì lăn chuột không cuộn được.
            AddImage(viewport, Color.clear, rounded: false).raycastTarget = true;

            var content = NewUI("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRect;

            // Close button
            var closeGo = NewUI("CloseButton", dialog.transform);
            SetRect(closeGo, BottomCenter, BottomCenter, BottomCenter, new Vector2(0f, 16f), new Vector2(190f, 42f));
            var closeImage = AddImage(closeGo, Green);
            closeImage.raycastTarget = true;
            var closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = AddText(NewUI("Label", closeGo.transform), "CLOSE", 17, Color.white, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform);
            closeLabel.fontStyle = FontStyles.Bold;

            var view = panel.AddComponent<InventoryPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("grid").objectReferenceValue = content.transform;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.FindProperty("summaryLabel").objectReferenceValue = summary;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("database").objectReferenceValue =
                Object.FindFirstObjectByType<Core.Database.DatabaseManager>(FindObjectsInactive.Include);
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        // =====================================================================
        // Helper
        // =====================================================================

        private static readonly Vector2 TopLeft = new(0f, 1f);
        private static readonly Vector2 BottomRight = new(1f, 0f);
        private static readonly Vector2 BottomCenter = new(0.5f, 0f);
        private static readonly Vector2 Center = new(0.5f, 0.5f);

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(GameObject go, Color color, bool rounded = true)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color,
            TextAlignmentOptions alignment)
        {
            var label = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}
