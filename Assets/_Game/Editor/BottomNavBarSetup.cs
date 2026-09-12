using System.Linq;
using DreamCafe.SystemControl.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng thanh nút bấm ở chân màn hình: 4 nút thường + nút Shop nổi trên vòng tròn ở giữa.
    /// Chạy lại sẽ xoá thanh cũ rồi dựng mới.
    /// </summary>
    public static class BottomNavBarSetup
    {
        private const string BarName = "BottomNavBar";
        private const string IconFolder = "Assets/_Game/Art/UI/";

        private static readonly Color BarGray = new(0.792f, 0.792f, 0.792f);
        private static readonly Color CircleDark = new(0.302f, 0.302f, 0.302f);

        private const float BarHeight = 96f;
        private const float ButtonSize = 64f;
        private const float CircleSize = 104f;
        private const float ShopIconSize = 68f;

        [MenuItem("DreamCafe/Setup/Bottom Nav Bar")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(BarName, "Không tìm thấy Canvas trong scene đang mở.", "OK");
                return;
            }

            var existing = canvas.transform.Find(BarName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var bar = NewUI(BarName, canvas.transform);
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, BarHeight);
            var barImage = bar.AddComponent<Image>();
            barImage.color = BarGray;

            AddFlatButton(bar.transform, "CrownButton", "tag_hard 2.png", 0.1f);
            AddFlatButton(bar.transform, "SpecialButton", "ico_special 2.png", 0.3f);
            var collection = AddFlatButton(bar.transform, "CollectionButton", "ico_collection 3.png", 0.7f);
            AddFlatButton(bar.transform, "DailyButton", "widget_daily 3.png", 0.9f);
            AddShopButton(bar.transform);

            WireInventoryToggle(canvas.transform, collection);

            // Panel kho phải nằm cuối để lớp dim che được thanh nav khi mở.
            var panel = canvas.transform.Find("InventoryPanel");
            if (panel != null) panel.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(bar, "Create Bottom Nav Bar");
            Selection.activeGameObject = bar;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log($"[BottomNavBarSetup] Đã dựng '{BarName}' trong Canvas.");
        }

        private static Button AddFlatButton(Transform parent, string name, string iconFile, float anchorX)
        {
            var go = NewUI(name, parent);
            SetRect(go, new Vector2(anchorX, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(ButtonSize, ButtonSize));

            var image = go.AddComponent<Image>();
            image.sprite = LoadSprite(iconFile);
            image.preserveAspect = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void AddShopButton(Transform parent)
        {
            // Neo vào cạnh trên của thanh nav rồi kéo xuống một chút — nửa trên vòng tròn nhô lên.
            var go = NewUI("ShopButton", parent);
            SetRect(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f),
                new Vector2(CircleSize, CircleSize));

            var circle = go.AddComponent<Image>();
            circle.sprite = LoadSpriteAtPath("Assets/_Game/Art/Sprites/circle.png");
            circle.color = CircleDark;
            circle.preserveAspect = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = circle;

            var iconGo = NewUI("Icon", go.transform);
            SetRect(iconGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(ShopIconSize, ShopIconSize));
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = LoadSprite("ico_shop 2.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        /// <summary>Nối sẵn nút Collection vào panel kho nếu panel đã tồn tại — đổi lại trong Inspector được.</summary>
        private static void WireInventoryToggle(Transform canvas, Button button)
        {
            var panel = canvas.Find("InventoryPanel");
            var view = panel != null ? panel.GetComponent<InventoryPanelView>() : null;
            if (view == null) return;

            UnityEventTools.AddPersistentListener(button.onClick, view.Toggle);
        }

        // =====================================================================
        // Helper
        // =====================================================================

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Sprite LoadSprite(string fileName) => LoadSpriteAtPath(IconFolder + fileName);

        /// <summary>Ảnh import ở chế độ Multiple nên main asset là Texture2D — phải lấy sprite con.</summary>
        private static Sprite LoadSpriteAtPath(string path)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) Debug.LogWarning($"[BottomNavBarSetup] Không tìm thấy sprite tại '{path}'.");
            return sprite;
        }
    }
}
