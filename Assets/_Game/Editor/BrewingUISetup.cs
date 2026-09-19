using System.Linq;
using DreamCafe.SystemControl.Brew;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Dựng sẵn Brewing Panel vào Canvas của scene đang mở + sinh prefab ô nguyên liệu.
    /// Chạy lại sẽ xoá panel cũ rồi dựng mới, nên sửa số liệu ở đây rồi chạy lại là được.
    ///
    /// Bố cục bám theo mock: khay nguyên liệu bên trái, ca pha chế chia tầng ở giữa, bảng gợi ý +
    /// dải "công thức đang thử" bên phải, BREW ở dưới, và popup kết quả phủ lên trên.
    /// Toàn bộ sprite lấy từ <see cref="ArtDir"/> — art nào thiếu thì rơi về hình mặc định của UI.
    /// </summary>
    public static class BrewingUISetup
    {
        private const string PanelName = "BrewingPanel";
        private const string SlotPrefabPath = "Assets/_Game/Prefabs/BrewIngredientSlot.prefab";
        private const string ChipPrefabPath = "Assets/_Game/Prefabs/BrewDiscoveryChip.prefab";
        private const string ArtDir = "Assets/_Game/Art/Brew";
        private const string SpritesDir = "Assets/_Game/Art/Sprites";
        /// <summary>Nơi chứa mấy sprite tự sinh (khói, vòng chờ) — art không phải vẽ tay mấy hình này.</summary>
        private const string GeneratedDir = ArtDir + "/Generated";

        private static readonly Color Cream = new(0.992f, 0.957f, 0.863f);
        private static readonly Color DarkBrown = new(0.290f, 0.212f, 0.125f);
        private static readonly Color Brown = new(0.478f, 0.345f, 0.216f);
        private static readonly Color Green = new(0.478f, 0.635f, 0.290f);
        private static readonly Color Red = new(0.902f, 0.435f, 0.478f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.55f);
        private static readonly Color PopupDim = new(0f, 0f, 0f, 0.45f);

        // Bảng công thức mới: thẻ kem nhạt, dải tiêu đề màu cát, số liệu vàng/xanh.
        private static readonly Color DiscoveryDim = new(0f, 0f, 0f, 0.72f);
        private static readonly Color CardCream = new(0.996f, 0.976f, 0.914f);
        private static readonly Color TanStrip = new(0.937f, 0.886f, 0.784f);
        private static readonly Color Gold = new(0.957f, 0.749f, 0.212f);
        private static readonly Color RewardGreen = new(0.353f, 0.588f, 0.176f);

        // Khung nền panel là ảnh 1861x921 (tỉ lệ 2.021) -> khung phải cùng tỉ lệ, không thì hoạ tiết
        // bị kéo méo. 1820x900 = 2.022, và rộng ~95% màn hình 1920 đúng như bản thiết kế.
        private static readonly Vector2 DialogSize = new(1820f, 900f);
        // Khay gỗ 295x233 (tỉ lệ 1.266) -> ô giữ đúng tỉ lệ đó để khung gỗ không bị bóp.
        private static readonly Vector2 CellSize = new(168f, 132f);
        // Tấm thẻ của bảng công thức mới — nút ADD TO MENU thò ra khỏi mép dưới nên chừa chỗ sẵn.
        private static readonly Vector2 CardSize = new(940f, 560f);
        // Ca pha chế: đúng tỉ lệ 600x520 của bộ ảnh do PitcherArtGenerator vẽ ra.
        private static readonly Vector2 CupSize = new(467f, 405f);
        private static readonly Vector2 CupPosition = new(694f, -364f);

        // Tấm panel trượt vào lúc đang pha.
        private static readonly Vector2 LoadingPanelSize = new(560f, 360f);
        private static readonly Color LoadingDim = new(0.04f, 0.03f, 0.02f, 0.78f);

        [MenuItem("DreamCafe/Setup/Brewing Panel UI")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Brewing Panel UI", "Không tìm thấy Canvas trong scene đang mở.", "OK");
                return;
            }

            var existing = canvas.transform.Find(PanelName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var slotPrefab = BuildIngredientSlotPrefab();
            var chipPrefab = BuildDiscoveryChipPrefab();
            var panel = BuildPanel(canvas.transform, slotPrefab, chipPrefab);

            Undo.RegisterCreatedObjectUndo(panel, "Create Brewing Panel");
            Selection.activeGameObject = panel;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log($"[BrewingUISetup] Đã dựng '{PanelName}' trong Canvas và prefab tại {SlotPrefabPath}. Bấm B trong Play Mode để mở.");
        }

        // =====================================================================
        // Prefab ô nguyên liệu (khay gỗ)
        // =====================================================================

        private static BrewIngredientSlotView BuildIngredientSlotPrefab()
        {
            var root = NewUI("BrewIngredientSlot", null);
            SetRect(root, Center, Center, Center, Vector2.zero, CellSize);
            var frame = AddSprite(root, $"{ArtDir}/tray.png", Color.white);
            frame.raycastTarget = true;

            // Icon căng theo ô thay vì cố định một cỡ: đổi CellSize là món tự to/nhỏ theo, không
            // còn cảnh khay gỗ phình ra mà món bên trong vẫn bé tí. Chừa lề dưới cho gờ đỡ của khay.
            var iconGo = NewUI("Icon", root.transform);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(24f, 30f);
            iconRect.offsetMax = new Vector2(-24f, -14f);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Số lượng nằm giữa gờ khay (thiết kế để ở giữa, không phải nhét góc trái).
            var quantityLabel = AddText(NewUI("QuantityLabel", root.transform), "x0", 22, Color.white,
                TextAlignmentOptions.Center);
            var qtyRect = quantityLabel.rectTransform;
            qtyRect.anchorMin = new Vector2(0f, 0f);
            qtyRect.anchorMax = new Vector2(1f, 0f);
            qtyRect.pivot = new Vector2(0.5f, 0f);
            qtyRect.anchoredPosition = new Vector2(0f, 5f);
            qtyRect.sizeDelta = new Vector2(-40f, 30f);
            quantityLabel.fontStyle = FontStyles.Bold;
            Outline(quantityLabel, DarkBrown, 0.25f);

            var button = root.AddComponent<Button>();
            button.targetGraphic = frame;

            var view = root.AddComponent<BrewIngredientSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("quantityLabel").objectReferenceValue = quantityLabel;
            so.FindProperty("button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<BrewIngredientSlotView>();
        }

        // =====================================================================
        // Panel
        // =====================================================================

        private static GameObject BuildPanel(Transform canvas, BrewIngredientSlotView slotPrefab,
            DiscoveryIngredientChipView chipPrefab)
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
            AddSprite(dialog, $"{ArtDir}/Background (1).png", Color.white, fallback: Cream);

            var content = BuildShelf(dialog.transform);
            var cupLayers = BuildCup(dialog.transform);
            var (hintLabel, resultLabel) = BuildHintBoard(dialog.transform);
            var mixtureSlots = BuildMixtureStrip(dialog.transform);

            var brewButton = BuildButton(dialog.transform, "BrewButton", "BREW",
                BottomCenter, new Vector2(0f, 40f), new Vector2(438f, 109f), 37f);

            // Nút đóng (X) tròn góc trên phải, tràn ra ngoài mép panel như trong mock.
            var closeGo = NewUI("CloseButton", dialog.transform);
            SetRect(closeGo, TopRight, TopRight, TopRight, new Vector2(9f, 92f), new Vector2(108f, 108f));
            var closeImage = AddSprite(closeGo, $"{SpritesDir}/circle.png", Red);
            closeImage.raycastTarget = true;
            var closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = AddText(NewUI("Label", closeGo.transform), "X", 46, Color.white, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform);
            closeLabel.fontStyle = FontStyles.Bold;

            // Popup nằm trong Window (không phải trong Dialog) để lớp mờ phủ kín cả màn hình.
            var popup = BuildResultPopup(window.transform);
            // Bảng chi tiết dựng SAU popup ăn mừng nên nằm trên nó trong thứ tự vẽ — đúng thứ tự
            // người chơi gặp: tắt popup ăn mừng thì bảng này hiện ra.
            var discovery = BuildDiscoveryPopup(window.transform, chipPrefab);
            // Màn chờ dựng cuối cùng nên nằm trên cả hai popup — nó chạy trước chúng trong luồng
            // pha chế, và lớp mờ của nó phải che được mọi thứ phía dưới.
            var loading = BuildLoadingOverlay(window.transform);

            var view = panel.AddComponent<BrewingPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("ingredientGrid").objectReferenceValue = content.transform;
            so.FindProperty("ingredientSlotPrefab").objectReferenceValue = slotPrefab;

            var slotsProp = so.FindProperty("mixtureSlots");
            slotsProp.arraySize = mixtureSlots.Length;
            for (int i = 0; i < mixtureSlots.Length; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = mixtureSlots[i];
            }

            var layersProp = so.FindProperty("cupLayers");
            layersProp.arraySize = cupLayers.Length;
            for (int i = 0; i < cupLayers.Length; i++)
            {
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = cupLayers[i];
            }

            so.FindProperty("hintLabel").objectReferenceValue = hintLabel;
            so.FindProperty("resultLabel").objectReferenceValue = resultLabel;
            so.FindProperty("loadingView").objectReferenceValue = loading;
            so.FindProperty("resultPopup").objectReferenceValue = popup;
            so.FindProperty("discoveryPopup").objectReferenceValue = discovery;
            so.FindProperty("brewButton").objectReferenceValue = brewButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("database").objectReferenceValue =
                Object.FindFirstObjectByType<Core.Database.DatabaseManager>(FindObjectsInactive.Include);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Tắt Window ngay lúc dựng để panel không che Scene view — sửa mấy thứ khác trong
            // scene mới thao tác được. Tắt đúng Window chứ KHÔNG tắt node gốc: gốc tắt thì
            // Update() không chạy, bấm phím tắt cũng không mở lại được panel.
            window.SetActive(false);

            return panel;
        }

        /// <summary>Kệ nguyên liệu: scroll view 2 cột, trả về node Content để panel đổ ô vào.</summary>
        private static GameObject BuildShelf(Transform dialog)
        {
            var scrollGo = NewUI("IngredientShelf", dialog);
            // Cột trái chiếm ~20% bề ngang và ~70% bề cao panel, đúng tỉ lệ lưới 2x3 trong thiết kế.
            SetRect(scrollGo, TopLeft, TopLeft, TopLeft, new Vector2(64f, -57f), new Vector2(368f, 632f));
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
            contentRect.sizeDelta = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(16f, 20f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRect;

            return content;
        }

        /// <summary>
        /// Tách cà phê ở giữa. Ba lớp xếp từ dưới lên: thân tách rỗng (mug-back) -> các tầng nước
        /// -> vành và vệt sáng (mug-front). Nhờ lớp trên cùng vẽ ĐÈ lên nước mà vành tách vẫn ăn
        /// lên mặt nước thay vì bị nước phủ mất.
        ///
        /// Mỗi tầng dùng một ảnh đã cắt sẵn theo lòng tách (xem <see cref="CupArtGenerator"/>) chứ
        /// không phải ô chữ nhật: tách LOE ra trên, hình chữ nhật sẽ thò ra ngoài thành ở miệng.
        /// Ô UI neo đúng dải mà <see cref="PitcherArtGenerator.LiquidBand"/> khai báo nên ảnh khớp tuyệt
        /// đối, và fillAmount 0..1 vẫn chạy được trong phạm vi riêng của từng tầng.
        /// </summary>
        private static Image[] BuildCup(Transform dialog)
        {
            PitcherArtGenerator.Generate(overwrite: false);

            var cup = NewUI("Cup", dialog);
            SetRect(cup, TopLeft, TopLeft, TopLeft, CupPosition, CupSize);
            var back = cup.AddComponent<Image>();
            back.sprite = ProceduralSpriteUtility.LoadSprite(PitcherArtGenerator.BackPath);
            back.raycastTarget = false;

            var liquid = NewUI("Liquid", cup.transform);
            Stretch((RectTransform)liquid.transform);

            var layers = new Image[BrewingController.MaxIngredients];
            for (int i = 0; i < layers.Length; i++)
            {
                PitcherArtGenerator.LiquidBand(i, out float bandBottom, out float bandTop);

                var layerGo = NewUI($"Layer{i}", liquid.transform);
                var rect = (RectTransform)layerGo.transform;
                rect.anchorMin = new Vector2(0f, bandBottom);
                rect.anchorMax = new Vector2(1f, bandTop);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = layerGo.AddComponent<Image>();
                image.sprite = ProceduralSpriteUtility.LoadSprite(PitcherArtGenerator.LiquidPath(i));
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Vertical;
                image.fillOrigin = (int)Image.OriginVertical.Bottom;
                image.fillAmount = 0f;
                image.raycastTarget = false;
                layers[i] = image;
            }

            // Dựng sau Liquid nên vành ca phủ lên mặt nước.
            var glass = NewUI("Glass", cup.transform);
            Stretch((RectTransform)glass.transform);
            var front = glass.AddComponent<Image>();
            front.sprite = ProceduralSpriteUtility.LoadSprite(PitcherArtGenerator.FrontPath);
            front.raycastTarget = false;

            return layers;
        }

        /// <summary>Bảng đen gợi ý công thức chưa mở khoá + dòng kết quả/cảnh báo thao tác.</summary>
        private static (TMP_Text hint, TMP_Text result) BuildHintBoard(Transform dialog)
        {
            var board = NewUI("HintBoard", dialog);
            // Ảnh bảng đen là 594x401 (tỉ lệ 1.481) -> 590x399 giữ nguyên tỉ lệ, không méo viền gỗ.
            SetRect(board, TopLeft, TopLeft, TopLeft, new Vector2(1192f, -107f), new Vector2(590f, 399f));
            AddSprite(board, $"{ArtDir}/image 82.png", Color.white, fallback: new Color(0.129f, 0.137f, 0.129f));

            var title = AddText(NewUI("Title", board.transform), "Hint:", 42, Color.white, TextAlignmentOptions.Center);
            SetRect(title.gameObject, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(0f, 54f));
            title.fontStyle = FontStyles.Bold;

            var hint = AddText(NewUI("HintLabel", board.transform), "...", 29,
                new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.TopLeft);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(62f, 112f);
            hintRect.offsetMax = new Vector2(-62f, -123f);

            var result = AddText(NewUI("ResultLabel", board.transform), string.Empty, 27,
                Color.white, TextAlignmentOptions.Top);
            var resultRect = result.rectTransform;
            resultRect.anchorMin = new Vector2(0f, 0f);
            resultRect.anchorMax = new Vector2(1f, 0f);
            resultRect.pivot = new Vector2(0.5f, 0f);
            resultRect.anchoredPosition = new Vector2(0f, 40f);
            resultRect.sizeDelta = new Vector2(-108f, 83f);
            result.fontStyle = FontStyles.Bold;

            return (hint, result);
        }

        /// <summary>Dải 3 ô "công thức đang thử", ngăn cách bằng dấu +.</summary>
        private static BrewMixtureSlotView[] BuildMixtureStrip(Transform dialog)
        {
            var strip = NewUI("MixtureStrip", dialog);
            SetRect(strip, TopLeft, TopLeft, TopLeft, new Vector2(1219f, -566f), new Vector2(540f, 182f));

            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            // Bắt buộc bật: tắt thì layout bỏ qua LayoutElement và xếp con theo kích thước rect thô
            // của chúng (chữ "+" mặc định 200x50), tổng bề ngang vọt lên ~708px và dải chữ tràn hẳn
            // ra ngoài mép phải panel.
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 6f;

            var slots = new BrewMixtureSlotView[BrewingController.MaxIngredients];
            for (int i = 0; i < slots.Length; i++)
            {
                if (i > 0)
                {
                    var plus = AddText(NewUI("Plus", strip.transform), "+", 48, Brown, TextAlignmentOptions.Center);
                    AddLayoutElement(plus.gameObject, 40f, 100f);
                    plus.fontStyle = FontStyles.Bold;
                }

                slots[i] = BuildMixtureSlot(strip.transform, i);
            }

            return slots;
        }

        private static BrewMixtureSlotView BuildMixtureSlot(Transform parent, int index)
        {
            var root = NewUI($"MixtureSlot{index}", parent);
            AddLayoutElement(root, 142f, 165f);
            var bg = AddImage(root, new Color(1f, 1f, 1f, 0f));
            bg.raycastTarget = true;

            var iconGo = NewUI("Icon", root.transform);
            SetRect(iconGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(120f, 136f));
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var nameLabel = AddText(NewUI("NameLabel", root.transform), "???", 20, DarkBrown, TextAlignmentOptions.Top);
            SetRect(nameLabel.gameObject, BottomCenter, BottomCenter, BottomCenter, new Vector2(0f, 6f), new Vector2(145f, 54f));

            var button = root.AddComponent<Button>();
            button.targetGraphic = bg;

            var view = root.AddComponent<BrewMixtureSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        // =====================================================================
        // Popup kết quả
        // =====================================================================

        private static BrewResultPopupView BuildResultPopup(Transform window)
        {
            var host = NewUI("ResultPopup", window);
            Stretch((RectTransform)host.transform);

            var root = NewUI("Root", host.transform);
            Stretch((RectTransform)root.transform);

            // Lớp mờ kiêm nút tắt — bấm đâu cũng đóng được popup.
            var dim = NewUI("Dim", root.transform);
            Stretch((RectTransform)dim.transform);
            var dimImage = AddImage(dim, PopupDim, rounded: false);
            dimImage.raycastTarget = true;
            var dismissButton = dim.AddComponent<Button>();
            dismissButton.targetGraphic = dimImage;
            dismissButton.transition = Selectable.Transition.None;

            // Content được phóng to dần lúc hiện; căn theo tâm panel chứ không phải tâm màn hình.
            var content = NewUI("Content", root.transform);
            SetRect(content, Center, Center, Center, Vector2.zero, DialogSize);

            var raysGo = NewUI("Rays", content.transform);
            SetRect(raysGo, Center, Center, Center, new Vector2(0f, 20f), new Vector2(520f, 520f));
            var rays = raysGo.AddComponent<Image>();
            rays.sprite = LoadSprite($"{ArtDir}/image 83.png");
            rays.raycastTarget = false;
            rays.preserveAspect = true;

            var drinkGo = NewUI("Drink", content.transform);
            SetRect(drinkGo, Center, Center, Center, new Vector2(0f, 20f), new Vector2(250f, 250f));
            var drink = drinkGo.AddComponent<Image>();
            drink.preserveAspect = true;
            drink.raycastTarget = false;

            var bannerGo = NewUI("Banner", content.transform);
            SetRect(bannerGo, Center, Center, Center, new Vector2(0f, 218f), new Vector2(430f, 137f));
            var banner = bannerGo.AddComponent<Image>();
            banner.sprite = LoadSprite($"{ArtDir}/image 89.png");
            banner.raycastTarget = false;
            banner.preserveAspect = true;

            var bannerLabel = AddText(NewUI("BannerLabel", bannerGo.transform), "FAILED", 30, Color.white,
                TextAlignmentOptions.Center);
            SetRect(bannerLabel.gameObject, Center, Center, Center, new Vector2(0f, 4f), new Vector2(330f, 60f));
            bannerLabel.fontStyle = FontStyles.Bold;
            bannerLabel.enableAutoSizing = true;
            bannerLabel.fontSizeMin = 16f;
            bannerLabel.fontSizeMax = 30f;
            Outline(bannerLabel, new Color(0.20f, 0.13f, 0.08f), 0.25f);

            // Chip "New!" chỉ bật khi vừa mò ra công thức lần đầu.
            var newBadge = NewUI("NewBadge", bannerGo.transform);
            SetRect(newBadge, TopLeft, TopLeft, TopLeft, new Vector2(14f, 6f), new Vector2(84f, 44f));
            AddImage(newBadge, new Color(0.902f, 0.239f, 0.243f));
            var newLabel = AddText(NewUI("Label", newBadge.transform), "New!", 19, Color.white, TextAlignmentOptions.Center);
            Stretch(newLabel.rectTransform);
            newLabel.fontStyle = FontStyles.Bold;

            var headline = AddText(NewUI("Headline", content.transform), "Congratulation", 44, Color.white,
                TextAlignmentOptions.Center);
            SetRect(headline.gameObject, Center, Center, Center, new Vector2(0f, -180f), new Vector2(700f, 70f));
            headline.fontStyle = FontStyles.Bold;
            Outline(headline, new Color(0.20f, 0.13f, 0.08f), 0.3f);

            var catGo = NewUI("Cat", content.transform);
            SetRect(catGo, BottomRight, BottomRight, BottomRight, new Vector2(30f, -70f), new Vector2(250f, 264f));
            var cat = catGo.AddComponent<Image>();
            cat.sprite = LoadSprite($"{ArtDir}/image 84.png");
            cat.preserveAspect = true;
            cat.raycastTarget = false;

            // Dựng sau Content nên khói vẽ ĐÈ lên popup — khói tan tới đâu lộ popup tới đó, nối
            // liền mạch vào đúng lúc màn chờ vừa trượt đi.
            var smoke = BuildSmokeBurst(root.transform);

            var view = host.AddComponent<BrewResultPopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("content").objectReferenceValue = content.transform;
            so.FindProperty("dismissButton").objectReferenceValue = dismissButton;
            so.FindProperty("smokeBurst").objectReferenceValue = smoke;
            so.FindProperty("rays").objectReferenceValue = rays;
            so.FindProperty("drinkIcon").objectReferenceValue = drink;
            so.FindProperty("banner").objectReferenceValue = banner;
            so.FindProperty("bannerLabel").objectReferenceValue = bannerLabel;
            so.FindProperty("newBadge").objectReferenceValue = newBadge;
            so.FindProperty("headlineLabel").objectReferenceValue = headline;
            so.FindProperty("cat").objectReferenceValue = cat;
            so.FindProperty("successRays").objectReferenceValue = LoadSprite($"{ArtDir}/image 83.png");
            so.FindProperty("failRays").objectReferenceValue = LoadSprite($"{ArtDir}/image 88.png");
            // Ruy băng xanh trống duy nhất đang có là nút lime; new-recipe-banner.png đã in sẵn chữ
            // "BISCOFF LATTE" nên không dùng làm khung động được.
            so.FindProperty("successBanner").objectReferenceValue = LoadSprite($"{ArtDir}/btn-green-lime.png");
            so.FindProperty("failBanner").objectReferenceValue = LoadSprite($"{ArtDir}/image 89.png");
            so.FindProperty("happyCat").objectReferenceValue = LoadSprite($"{ArtDir}/image 84.png");
            so.FindProperty("sadCat").objectReferenceValue = LoadSprite($"{ArtDir}/image 85.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
        }

        // =====================================================================
        // Màn chờ "đang pha" (trượt vào ngay sau khi bấm BREW)
        // =====================================================================

        /// <summary>
        /// Tấm panel trượt từ dưới lên giữa màn hình: vòng cung xoay + vòng tiến độ + mèo ngồi đợi.
        /// Lớp mờ chặn luôn raycast nên không bấm BREW dồn dập trong lúc máy đang chạy được.
        /// </summary>
        private static BrewLoadingView BuildLoadingOverlay(Transform window)
        {
            var host = NewUI("BrewLoading", window);
            Stretch((RectTransform)host.transform);

            var root = NewUI("Root", host.transform);
            Stretch((RectTransform)root.transform);

            var dim = NewUI("Dim", root.transform);
            Stretch((RectTransform)dim.transform);
            AddImage(dim, LoadingDim, rounded: false).raycastTarget = true;
            var dimGroup = dim.AddComponent<CanvasGroup>();

            // Node được trượt — vị trí đặt sẵn ở đây chính là "chỗ đứng" mà BrewLoadingView ghi nhớ.
            var panel = NewUI("Panel", root.transform);
            SetRect(panel, Center, Center, Center, Vector2.zero, LoadingPanelSize);

            var frame = NewUI("Frame", panel.transform);
            SetRect(frame, Center, Center, Center, Vector2.zero, LoadingPanelSize + new Vector2(12f, 12f));
            AddImage(frame, new Color(0.804f, 0.729f, 0.545f));

            var cardGo = NewUI("Card", panel.transform);
            SetRect(cardGo, Center, Center, Center, Vector2.zero, LoadingPanelSize);
            AddImage(cardGo, Cream);
            var card = cardGo.transform;

            // Vòng tiến độ nằm dưới vòng xoay: nó đứng yên, chỉ dày dần lên theo thời gian chờ.
            var trackGo = NewUI("ProgressTrack", card);
            SetRect(trackGo, Center, Center, Center, new Vector2(0f, 30f), new Vector2(206f, 206f));
            var track = trackGo.AddComponent<Image>();
            track.sprite = EnsureRingSprite();
            track.color = new Color(0.29f, 0.21f, 0.13f, 0.12f);
            track.raycastTarget = false;

            var progressGo = NewUI("ProgressFill", card);
            SetRect(progressGo, Center, Center, Center, new Vector2(0f, 30f), new Vector2(206f, 206f));
            var progress = progressGo.AddComponent<Image>();
            progress.sprite = EnsureRingSprite();
            progress.color = Green;
            progress.raycastTarget = false;
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Radial360;
            progress.fillOrigin = (int)Image.Origin360.Top;
            progress.fillClockwise = true;
            progress.fillAmount = 0f;

            var spinnerGo = NewUI("Spinner", card);
            SetRect(spinnerGo, Center, Center, Center, new Vector2(0f, 30f), new Vector2(172f, 172f));
            var spinner = spinnerGo.AddComponent<Image>();
            spinner.sprite = EnsureSpinnerSprite();
            spinner.color = Brown;
            spinner.raycastTarget = false;

            // "Hình chờ" thật sự ở chính giữa — con mèo của popup ăn mừng, ngồi đợi cho có không khí.
            var iconGo = NewUI("WaitIcon", card);
            SetRect(iconGo, Center, Center, Center, new Vector2(0f, 30f), new Vector2(116f, 116f));
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = LoadSprite($"{ArtDir}/image 84.png");
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var status = AddText(NewUI("StatusLabel", card), "Brewing", 30f, DarkBrown, TextAlignmentOptions.Center);
            SetRect(status.gameObject, Center, Center, Center, new Vector2(0f, -118f), new Vector2(480f, 46f));
            status.fontStyle = FontStyles.Bold;

            var view = host.AddComponent<BrewLoadingView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("panel").objectReferenceValue = panel.transform;
            so.FindProperty("dim").objectReferenceValue = dimGroup;
            so.FindProperty("spinner").objectReferenceValue = spinnerGo.transform;
            so.FindProperty("pulseIcon").objectReferenceValue = iconGo.transform;
            so.FindProperty("progressFill").objectReferenceValue = progress;
            so.FindProperty("statusLabel").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
        }

        // =====================================================================
        // Khói nổ (phủ lên popup kết quả lúc nó vừa hiện, ngay sau màn chờ)
        // =====================================================================

        /// <summary>
        /// Cụm khói + chớp sáng + vòng xung kích. Các cụm khói tự sinh lúc chạy nên ở đây chỉ dựng
        /// phần cố định; node phải nằm CUỐI danh sách con để khói vẽ đè lên nội dung popup.
        /// </summary>
        private static UISmokeBurstView BuildSmokeBurst(Transform parent)
        {
            var go = NewUI("SmokeBurst", parent);
            SetRect(go, Center, Center, Center, Vector2.zero, new Vector2(100f, 100f));

            // Chớp sáng phủ rộng hơn màn hình để không lộ mép lúc chớp.
            var flashGo = NewUI("Flash", go.transform);
            SetRect(flashGo, Center, Center, Center, Vector2.zero, new Vector2(2600f, 1600f));
            var flash = AddImage(flashGo, new Color(1f, 1f, 1f, 0f), rounded: false);

            var waveGo = NewUI("Shockwave", go.transform);
            SetRect(waveGo, Center, Center, Center, Vector2.zero, new Vector2(560f, 560f));
            var wave = waveGo.AddComponent<Image>();
            wave.sprite = EnsureRingSprite();
            wave.color = new Color(1f, 1f, 1f, 0f);
            wave.raycastTarget = false;

            var view = go.AddComponent<UISmokeBurstView>();
            var so = new SerializedObject(view);
            so.FindProperty("puffSprite").objectReferenceValue = EnsureSmokePuffSprite();
            so.FindProperty("flash").objectReferenceValue = flash;
            so.FindProperty("shockwave").objectReferenceValue = waveGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        // =====================================================================
        // Sprite tự sinh
        // =====================================================================

        /// <summary>Vệt tròn mềm viền — chồng chục cái lên nhau thì ra cụm khói.</summary>
        private static Sprite EnsureSmokePuffSprite() =>
            EnsureGeneratedSprite("smoke-puff.png", 128, (nx, ny) =>
            {
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                if (r >= 1f) return 0f;
                // Lõi đặc tới tận 0.18 rồi mới loãng — mềm đều từ tâm ra sẽ nhìn như sương, không ra khói.
                float core = 1f - ProceduralSpriteUtility.SmoothStep01(0.18f, 1f, r);
                return Mathf.Pow(core, 1.35f);
            });

        /// <summary>Vòng cung kiểu đuôi sao chổi — xoay tròn là thành hình chờ.</summary>
        private static Sprite EnsureSpinnerSprite() =>
            EnsureGeneratedSprite("spinner-arc.png", 192, (nx, ny) =>
            {
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                const float inner = 0.62f;
                const float outer = 0.96f;
                float band = ProceduralSpriteUtility.SmoothStep01(inner - 0.07f, inner, r) * (1f - ProceduralSpriteUtility.SmoothStep01(outer - 0.06f, outer, r));
                if (band <= 0f) return 0f;

                float angle = Mathf.Atan2(ny, nx) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                // Đậm nhất ở đầu vòng cung, nhạt dần rồi hụt hẳn — 60 độ cuối để trống làm khe hở.
                if (angle > 300f) return 0f;
                return band * Mathf.Clamp01(1f - angle / 300f);
            });

        /// <summary>Vòng tròn rỗng đều — dùng cho vòng tiến độ và vòng xung kích.</summary>
        private static Sprite EnsureRingSprite() =>
            EnsureGeneratedSprite("ring.png", 192, (nx, ny) =>
            {
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                const float inner = 0.82f;
                const float outer = 0.98f;
                return Mathf.Clamp01(ProceduralSpriteUtility.SmoothStep01(inner - 0.04f, inner, r) *
                                     (1f - ProceduralSpriteUtility.SmoothStep01(outer - 0.04f, outer, r)));
            });

        /// <summary>
        /// Vẽ một texture trắng chỉ khác nhau ở kênh alpha rồi lưu thành PNG trong
        /// <see cref="GeneratedDir"/>. Có file sẵn thì dùng lại — chạy lại menu không làm bẩn git;
        /// muốn sinh lại thì xoá file đi rồi chạy lại.
        /// </summary>
        /// <param name="alphaAt">Alpha 0..1 tại một điểm, toạ độ chuẩn hoá -1..1 với tâm ở giữa ảnh.</param>
        private static Sprite EnsureGeneratedSprite(string fileName, int size, System.Func<float, float, float> alphaAt)
        {
            // Mấy hình cũ viết theo toạ độ tâm -1..1, còn bộ vẽ chung dùng 0..1 gốc góc dưới-trái.
            return EnsureGeneratedSprite(fileName, size, size,
                (u, v) => new Color(1f, 1f, 1f, alphaAt(u * 2f - 1f, v * 2f - 1f)));
        }

        /// <summary>
        /// Bản đầy đủ màu của <see cref="EnsureGeneratedSprite(string,int,System.Func{float,float,float})"/> —
        /// dùng cho những hình cần nhiều hơn một màu (cốc thuỷ tinh: kính, vành, vệt sáng, viền).
        /// </summary>
        /// <param name="colorAt">Màu tại một điểm, toạ độ chuẩn hoá 0..1 với (0,0) ở góc dưới-trái.</param>
        private static Sprite EnsureGeneratedSprite(string fileName, int width, int height,
            System.Func<float, float, Color> colorAt)
        {
            string path = $"{GeneratedDir}/{fileName}";

            if (!System.IO.File.Exists(path))
            {
                if (!AssetDatabase.IsValidFolder(GeneratedDir)) AssetDatabase.CreateFolder(ArtDir, "Generated");

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Lấy tâm điểm ảnh (+0.5) chứ không lấy góc, nếu không hình bị lệch nửa pixel.
                        var color = colorAt((x + 0.5f) / width, (y + 0.5f) / height);

                        // Điểm trong suốt hẳn vẫn phải mang màu sáng: lọc bilinear kéo màu của chúng
                        // vào mép hình, để đen thì viền bị quầng tối.
                        if (color.a <= 0f) color = new Color(1f, 1f, 1f, 0f);

                        pixels[y * width + x] = color;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            // Import mặc định của project là Sprite Multiple — ép về Single, không thì LoadSprite trượt.
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Sprite ||
                 importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return LoadSprite(path);
        }

        // =====================================================================
        // Bảng "NEW RECIPE DISCOVERED!" (hiện sau khi tắt popup ăn mừng)
        // =====================================================================

        /// <summary>
        /// Ô nguyên liệu trong dải BLUEPRINT: một hàng ngang [dấu +][thẻ]. Tắt dấu + thì thẻ tự co
        /// lại nhờ ContentSizeFitter, nên ô đầu tiên không chừa khoảng trống thừa bên trái.
        /// </summary>
        private static DiscoveryIngredientChipView BuildDiscoveryChipPrefab()
        {
            var root = NewUI("BrewDiscoveryChip", null);
            SetRect(root, Center, Center, Center, Vector2.zero, new Vector2(132f, 104f));

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var plus = NewUI("Plus", root.transform);
            AddLayoutElement(plus, 24f, 104f);
            var plusLabel = AddText(plus, "+", 24f, Brown, TextAlignmentOptions.Center);
            plusLabel.fontStyle = FontStyles.Bold;

            var card = NewUI("Card", root.transform);
            AddLayoutElement(card, 106f, 104f);
            AddImage(card, CardCream);

            var iconGo = NewUI("Icon", card.transform);
            SetRect(iconGo, Center, Center, Center, new Vector2(0f, 22f), new Vector2(48f, 48f));
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var nameLabel = AddText(NewUI("NameLabel", card.transform), "Coffee Beans", 11f, DarkBrown,
                TextAlignmentOptions.Center);
            SetRect(nameLabel.gameObject, Center, Center, Center, new Vector2(0f, -18f), new Vector2(98f, 28f));
            nameLabel.fontStyle = FontStyles.Bold;

            var countLabel = AddText(NewUI("CountLabel", card.transform), "x1", 13f, Brown, TextAlignmentOptions.Center);
            SetRect(countLabel.gameObject, Center, Center, Center, new Vector2(0f, -40f), new Vector2(98f, 20f));
            countLabel.fontStyle = FontStyles.Bold;

            var view = root.AddComponent<DiscoveryIngredientChipView>();
            var so = new SerializedObject(view);
            so.FindProperty("plusSign").objectReferenceValue = plus;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("countLabel").objectReferenceValue = countLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ChipPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<DiscoveryIngredientChipView>();
        }

        private static RecipeDiscoveryPopupView BuildDiscoveryPopup(Transform window,
            DiscoveryIngredientChipView chipPrefab)
        {
            var host = NewUI("DiscoveryPopup", window);
            Stretch((RectTransform)host.transform);

            var root = NewUI("Root", host.transform);
            Stretch((RectTransform)root.transform);

            // Lớp mờ ở đây KHÔNG phải nút tắt (khác popup ăn mừng) — bảng này để đọc, chỉ đóng bằng
            // X hoặc ADD TO MENU; nó chặn luôn click rơi xuống panel pha chế phía dưới.
            // Đậm hơn lớp mờ của popup ăn mừng: panel pha chế phía dưới cũng có một nút X, mờ nhạt
            // quá thì hai chữ X nằm cạnh nhau trông như lỗi.
            var dim = NewUI("Dim", root.transform);
            Stretch((RectTransform)dim.transform);
            AddImage(dim, DiscoveryDim, rounded: false).raycastTarget = true;

            var content = NewUI("Content", root.transform);
            SetRect(content, Center, Center, Center, Vector2.zero, CardSize);

            // Viền đôi: một tấm màu cát to hơn nằm dưới tấm kem.
            var frame = NewUI("Frame", content.transform);
            SetRect(frame, Center, Center, Center, Vector2.zero, CardSize + new Vector2(12f, 12f));
            AddImage(frame, new Color(0.804f, 0.729f, 0.545f));

            var cardGo = NewUI("Card", content.transform);
            SetRect(cardGo, Center, Center, Center, Vector2.zero, CardSize);
            AddImage(cardGo, Cream);
            var card = cardGo.transform;

            var header = NewUI("Header", card);
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 84f);
            AddImage(header, DarkBrown);

            var title = AddText(NewUI("Title", header.transform), "NEW RECIPE DISCOVERED!", 40f, Cream,
                TextAlignmentOptions.Center);
            Stretch(title.rectTransform);
            title.fontStyle = FontStyles.Bold;
            Outline(title, new Color(0.15f, 0.09f, 0.05f), 0.25f);

            // Nằm gọn trong góc dải tiêu đề chứ không thò ra ngoài mép thẻ — tránh đụng nút X của panel.
            var closeGo = NewUI("CloseButton", card);
            SetRect(closeGo, TopRight, TopRight, TopRight, new Vector2(-10f, -10f), new Vector2(60f, 60f));
            var closeImage = AddSprite(closeGo, $"{SpritesDir}/circle.png", Red);
            closeImage.raycastTarget = true;
            var closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = AddText(NewUI("Label", closeGo.transform), "X", 30f, Color.white, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform);
            closeLabel.fontStyle = FontStyles.Bold;

            // --- Cột trái: ly, ruy băng tên món, dải nguyên liệu ---
            var left = NewUI("RecipeCard", card);
            SetRect(left, Center, Center, Center, new Vector2(-238f, -15f), new Vector2(444f, 414f));
            AddImage(left, CardCream);

            var drinkGo = NewUI("Drink", left.transform);
            SetRect(drinkGo, Center, Center, Center, new Vector2(0f, 78f), new Vector2(200f, 200f));
            var drink = drinkGo.AddComponent<Image>();
            drink.preserveAspect = true;
            drink.raycastTarget = false;

            var bannerGo = NewUI("Banner", left.transform);
            // 372x98 = đúng tỉ lệ 371x98 của btn-green-lime.png; để 372x86 như trước là nút bị bóp
            // dẹt 14% vì AddSprite không bật preserveAspect.
            SetRect(bannerGo, Center, Center, Center, new Vector2(0f, -54f), new Vector2(372f, 98f));
            AddSprite(bannerGo, $"{ArtDir}/btn-green-lime.png", Color.white, fallback: Green);

            var nameLabel = AddText(NewUI("NameLabel", bannerGo.transform), "BISCOFF LATTE", 26f, Cream,
                TextAlignmentOptions.Center);
            SetRect(nameLabel.gameObject, Center, Center, Center, Vector2.zero, new Vector2(316f, 56f));
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 14f;
            nameLabel.fontSizeMax = 26f;
            Outline(nameLabel, new Color(0.20f, 0.13f, 0.08f), 0.25f);

            var newBadge = NewUI("NewBadge", bannerGo.transform);
            SetRect(newBadge, TopLeft, TopLeft, TopLeft, new Vector2(4f, 14f), new Vector2(78f, 40f));
            AddImage(newBadge, new Color(0.902f, 0.239f, 0.243f));
            var newLabel = AddText(NewUI("Label", newBadge.transform), "New!", 17f, Color.white, TextAlignmentOptions.Center);
            Stretch(newLabel.rectTransform);
            newLabel.fontStyle = FontStyles.Bold;

            var blueprint = AddText(NewUI("BlueprintLabel", left.transform), "BLUEPRINT (INGREDIENTS)", 16f,
                DarkBrown, TextAlignmentOptions.MidlineLeft);
            SetRect(blueprint.gameObject, Center, Center, Center, new Vector2(0f, -114f), new Vector2(400f, 26f));
            blueprint.fontStyle = FontStyles.Bold;

            var rowGo = NewUI("IngredientRow", left.transform);
            SetRect(rowGo, Center, Center, Center, new Vector2(0f, -164f), new Vector2(410f, 78f));
            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 2f;

            // --- Cột phải: chỉ số vận hành + phần thưởng ---
            var stats = NewUI("StatsCard", card);
            SetRect(stats, Center, Center, Center, new Vector2(238f, 86f), new Vector2(444f, 212f));
            AddImage(stats, CardCream);
            BuildSectionHeader(stats.transform, "OPERATION STATS");

            var revenue = BuildStatRow(stats.transform, 34f, Gold, "Money Per Sec", "0 d/s", DarkBrown, out _);
            var prepTime = BuildStatRow(stats.transform, -18f, Brown, "Prep Time", "0 sec", DarkBrown, out _);
            var level = BuildStatRow(stats.transform, -70f, Gold, "Initial Level", "Lv.1", DarkBrown, out _);

            var rewards = NewUI("RewardCard", card);
            SetRect(rewards, Center, Center, Center, new Vector2(238f, -122f), new Vector2(444f, 196f));
            AddImage(rewards, CardCream);
            BuildSectionHeader(rewards.transform, "ACHIEVEMENTS & REWARDS");

            var reputation = BuildStatRow(rewards.transform, 24f, Gold, "Shop Reputation", "+0 EXP", RewardGreen, out _);
            var customer = BuildStatRow(rewards.transform, -30f, Color.white, "New Customer", "-", RewardGreen, out var avatar);
            // Dòng khách hàng dùng ảnh đại diện thật chứ không phải chấm tròn — sprite gán lúc chạy.
            avatar.sprite = null;
            avatar.color = Color.white;

            var addButton = BuildButton(card, "AddToMenuButton", "ADD TO MENU",
                BottomCenter, new Vector2(0f, -28f), new Vector2(320f, 85f), 28f);

            var view = host.AddComponent<RecipeDiscoveryPopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("content").objectReferenceValue = content.transform;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("addToMenuButton").objectReferenceValue = addButton;
            so.FindProperty("drinkIcon").objectReferenceValue = drink;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("newBadge").objectReferenceValue = newBadge;
            so.FindProperty("ingredientRow").objectReferenceValue = rowGo.transform;
            so.FindProperty("chipPrefab").objectReferenceValue = chipPrefab;
            so.FindProperty("revenueLabel").objectReferenceValue = revenue;
            so.FindProperty("prepTimeLabel").objectReferenceValue = prepTime;
            so.FindProperty("levelLabel").objectReferenceValue = level;
            so.FindProperty("reputationLabel").objectReferenceValue = reputation;
            // Cả dòng khách hàng bị tắt khi lần này không mở ra ai — nút chứa nó chính là node cha.
            so.FindProperty("customerRow").objectReferenceValue = customer.transform.parent.gameObject;
            so.FindProperty("customerLabel").objectReferenceValue = customer;
            so.FindProperty("customerAvatar").objectReferenceValue = avatar;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
        }

        /// <summary>Dải tiêu đề màu cát chạy ngang đầu một thẻ con.</summary>
        private static void BuildSectionHeader(Transform card, string text)
        {
            var strip = NewUI("SectionHeader", card);
            var rect = (RectTransform)strip.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 46f);
            AddImage(strip, TanStrip);

            var label = AddText(NewUI("Label", strip.transform), text, 17f, Brown, TextAlignmentOptions.MidlineLeft);
            SetRect(label.gameObject, MiddleLeft, MiddleLeft, MiddleLeft, new Vector2(18f, 0f), new Vector2(400f, 28f));
            label.fontStyle = FontStyles.Bold;
        }

        /// <summary>Một dòng "chấm tròn — tên — giá trị" trong thẻ chỉ số; trả về nhãn giá trị.</summary>
        private static TextMeshProUGUI BuildStatRow(Transform card, float y, Color iconTint, string name,
            string value, Color valueColor, out Image icon)
        {
            var row = NewUI($"Row_{name.Replace(" ", string.Empty)}", card);
            SetRect(row, Center, Center, Center, new Vector2(0f, y), new Vector2(408f, 48f));

            var iconGo = NewUI("Icon", row.transform);
            SetRect(iconGo, MiddleLeft, MiddleLeft, MiddleLeft, new Vector2(2f, 0f), new Vector2(34f, 34f));
            icon = AddSprite(iconGo, $"{SpritesDir}/circle.png", iconTint);
            icon.preserveAspect = true;

            var label = AddText(NewUI("Label", row.transform), name, 16f, DarkBrown, TextAlignmentOptions.MidlineLeft);
            SetRect(label.gameObject, MiddleLeft, MiddleLeft, MiddleLeft, new Vector2(44f, 0f), new Vector2(216f, 30f));
            label.fontStyle = FontStyles.Bold;

            var valueLabel = AddText(NewUI("Value", row.transform), value, 17f, valueColor, TextAlignmentOptions.MidlineRight);
            SetRect(valueLabel.gameObject, MiddleRight, MiddleRight, MiddleRight, new Vector2(-4f, 0f), new Vector2(190f, 30f));
            valueLabel.fontStyle = FontStyles.Bold;

            var line = NewUI("Separator", row.transform);
            SetRect(line, Center, Center, Center, new Vector2(0f, -23f), new Vector2(396f, 2f));
            AddImage(line, new Color(0.29f, 0.21f, 0.13f, 0.10f), rounded: false);

            return valueLabel;
        }

        // =====================================================================
        // Helper
        // =====================================================================

        private static readonly Vector2 TopLeft = new(0f, 1f);
        private static readonly Vector2 TopRight = new(1f, 1f);
        private static readonly Vector2 MiddleLeft = new(0f, 0.5f);
        private static readonly Vector2 MiddleRight = new(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new(0f, 0f);
        private static readonly Vector2 BottomRight = new(1f, 0f);
        private static readonly Vector2 BottomCenter = new(0.5f, 0f);
        private static readonly Vector2 Center = new(0.5f, 0.5f);

        /// <summary>
        /// Art trong dự án import ở chế độ Sprite Multiple nên <c>LoadAssetAtPath&lt;Sprite&gt;</c>
        /// trả về null — phải lấy sprite con qua representations.
        /// </summary>
        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) Debug.LogWarning($"[BrewingUISetup] Không tìm thấy sprite tại '{path}' — dùng hình mặc định.");
            return sprite;
        }

        /// <summary>Gắn sprite từ project; thiếu file thì rơi về ô bo góc mặc định màu <paramref name="fallback"/>.</summary>
        private static Image AddSprite(GameObject go, string path, Color tint, Color? fallback = null)
        {
            var sprite = LoadSprite(path);
            if (sprite == null) return AddImage(go, fallback ?? tint);

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        private static Button BuildButton(Transform parent, string name, string text,
            Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
        {
            var go = NewUI(name, parent);
            SetRect(go, anchor, anchor, anchor, position, size);
            var image = AddSprite(go, $"{ArtDir}/btn-green-lime.png", Color.white, fallback: Green);
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var label = AddText(NewUI("Label", go.transform), text, fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.fontStyle = FontStyles.Bold;
            Outline(label, new Color(0.25f, 0.33f, 0.13f), 0.2f);

            return button;
        }

        private static void AddLayoutElement(GameObject go, float width, float height)
        {
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
        }

        /// <summary>Viền chữ — chữ trắng trên nền sáng không có viền thì chìm nghỉm.</summary>
        private static void Outline(TMP_Text label, Color color, float width)
        {
            label.outlineColor = color;
            label.outlineWidth = width;
        }

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
