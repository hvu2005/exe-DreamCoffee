using DreamCafe.Data;
using DreamCafe.Gameplay.Camera;
using DreamCafe.UI.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 4 scene setup.
    /// Menu:  Tools > DreamCafé > Create Phase 4 HUD
    ///        Tools > DreamCafé > Setup Phase 4 Scene
    ///        Tools > DreamCafé > Create CafeConfig Asset
    ///
    /// Creates:
    ///   HUD Canvas with HudController + HudView (balance, day, customer count, progress bar)
    ///   End-of-day summary overlay with "Next Day" button
    ///   CafeCamera component on the main Camera (orthographic 2.5D tilt)
    ///   Resources/CafeConfig.asset for balance tuning
    /// </summary>
    public static class Phase4Setup
    {
        private const string ResPath    = "Assets/_Game/Resources";
        private const string DataPath   = "Assets/_Game/Data";

        // ── Entry points ─────────────────────────────────────────────────────

        [MenuItem("Tools/DreamCafé/Create Phase 4 HUD")]
        public static void CreateHUD()
        {
            if (Object.FindFirstObjectByType<HudController>() != null)
            {
                EditorUtility.DisplayDialog("DreamCafé", "HudController already exists in the scene.", "OK");
                return;
            }

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                EditorUtility.DisplayDialog("DreamCafé",
                    "TextMeshPro default font not found.\n\nOpen  Window > TextMeshPro > Import TMP Essential Resources  and try again.",
                    "OK");
                return;
            }

            var canvasGO = BuildCanvas(font);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("DreamCafé — Phase 4 HUD",
                $"HUD created: {canvasGO.name}\n\n" +
                "Structure:\n" +
                "  HUD (Canvas) — HudController, HudView\n" +
                "  ├─ TopBar\n" +
                "  │   ├─ TxtBalance\n" +
                "  │   ├─ TxtDay\n" +
                "  │   └─ TxtCustomers\n" +
                "  ├─ DayProgressBG → DayProgressFill\n" +
                "  └─ SummaryOverlay (disabled)\n" +
                "      ├─ TxtTitle / TxtRevenue / TxtServed / TxtLost\n" +
                "      └─ BtnNextDay\n\n" +
                "Next: run  Tools > DreamCafé > Setup Phase 4 Scene",
                "OK");
        }

        [MenuItem("Tools/DreamCafé/Setup Phase 4 Scene")]
        public static void SetupPhase4Scene()
        {
            // Add CafeCamera to main camera
            var mainCam = UnityEngine.Camera.main;
            if (mainCam != null && mainCam.GetComponent<CafeCamera>() == null)
            {
                Undo.AddComponent<CafeCamera>(mainCam.gameObject);
                Debug.Log("[Phase4Setup] CafeCamera added to Main Camera.");
            }

            // Wire cafeConfig to GameBootstrap
            var bootstrap = Object.FindFirstObjectByType<DreamCafe.App.GameBootstrap>();
            if (bootstrap != null)
            {
                var config = AssetDatabase.LoadAssetAtPath<CafeConfig>(DataPath + "/CafeConfig.asset");
                if (config == null)
                    config = AssetDatabase.LoadAssetAtPath<CafeConfig>(ResPath + "/CafeConfig.asset");
                if (config != null)
                {
                    var so = new SerializedObject(bootstrap);
                    so.FindProperty("cafeConfig").objectReferenceValue = config;
                    so.ApplyModifiedProperties();
                    Debug.Log("[Phase4Setup] CafeConfig wired to GameBootstrap.");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("DreamCafé — Phase 4 Scene",
                "Scene updated!\n\n" +
                "• CafeCamera added to Main Camera (orthographic, 15° tilt).\n" +
                "• CafeConfig wired to GameBootstrap.\n\n" +
                "Save (Ctrl+S) then Press Play.\n\n" +
                "You should see:\n" +
                "  Balance, Day, Customer count in top bar\n" +
                "  Blue day-progress bar filling over 3 minutes\n" +
                "  Summary overlay after day ends (hit Next Day)\n\n" +
                "To tweak balance, select  Assets/_Game/Data/CafeConfig  in the Project panel.",
                "OK");
        }

        [MenuItem("Tools/DreamCafé/Create CafeConfig Asset")]
        public static void CreateCafeConfigAsset()
        {
            EnsureFolder(DataPath);
            const string path = DataPath + "/CafeConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<CafeConfig>(path) != null)
            {
                EditorUtility.DisplayDialog("DreamCafé", "CafeConfig.asset already exists.", "OK");
                return;
            }
            var asset = ScriptableObject.CreateInstance<CafeConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            Debug.Log($"[Phase4Setup] CafeConfig.asset created at {path}");
        }

        // ── Canvas builder ────────────────────────────────────────────────────

        private static GameObject BuildCanvas(TMP_FontAsset font)
        {
            var root = new GameObject("HUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var ctrl = root.AddComponent<HudController>();
            var view = root.AddComponent<HudView>();

            // ── Top bar ──────────────────────────────────────────────────────
            var topBar = MakePanel(root.transform, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, -80f));
            AddPanelBG(topBar, new Color(0f, 0f, 0f, 0.55f));

            var txtBalance   = MakeLabel(topBar.transform, "TxtBalance",   "0đ",    font, 32, TextAlignmentOptions.Left);
            var txtDay       = MakeLabel(topBar.transform, "TxtDay",       "Day 1", font, 32, TextAlignmentOptions.Center);
            var txtCustomers = MakeLabel(topBar.transform, "TxtCustomers", "0/10",  font, 32, TextAlignmentOptions.Right);
            LayoutThreeColumns(txtBalance.GetComponent<RectTransform>(),
                               txtDay.GetComponent<RectTransform>(),
                               txtCustomers.GetComponent<RectTransform>());

            // ── Day progress bar ─────────────────────────────────────────────
            var barBG = MakePanel(root.transform, "DayProgressBG",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(0f, -92f));
            AddPanelBG(barBG, new Color(0.15f, 0.15f, 0.15f, 0.9f));

            var fillGO = new GameObject("DayProgressFill");
            fillGO.transform.SetParent(barBG.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin        = Vector2.zero;
            fillRT.anchorMax        = Vector2.one;
            fillRT.sizeDelta        = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color      = new Color(0.25f, 0.65f, 1f);
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;

            // ── Summary overlay ──────────────────────────────────────────────
            var summaryGO = MakePanel(root.transform, "SummaryOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            summaryGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            AddPanelBG(summaryGO, new Color(0f, 0f, 0f, 0.78f));

            var contentRT = new GameObject("Content");
            contentRT.transform.SetParent(summaryGO.transform, false);
            var cRT = contentRT.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.3f, 0.25f);
            cRT.anchorMax = new Vector2(0.7f, 0.75f);
            cRT.sizeDelta = Vector2.zero;
            AddPanelBG(contentRT, new Color(0.08f, 0.08f, 0.08f, 0.95f));

            var lblTitle   = MakeLabel(contentRT.transform, "TxtTitle",   "Day 1 Done!", font, 40, TextAlignmentOptions.Center);
            var lblRevenue = MakeLabel(contentRT.transform, "TxtRevenue", "Revenue  0đ", font, 28, TextAlignmentOptions.Left);
            var lblServed  = MakeLabel(contentRT.transform, "TxtServed",  "Served   0",  font, 28, TextAlignmentOptions.Left);
            var lblLost    = MakeLabel(contentRT.transform, "TxtLost",    "Lost     0",  font, 28, TextAlignmentOptions.Left);
            LayoutSummaryRows(lblTitle, lblRevenue, lblServed, lblLost);

            var btnGO = MakeButton(contentRT.transform, "BtnNextDay", "Next Day ▶", font);

            summaryGO.SetActive(false);

            // ── Wire HudView ─────────────────────────────────────────────────
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("balanceLabel").objectReferenceValue    = txtBalance.GetComponent<TMP_Text>();
            viewSO.FindProperty("dayLabel").objectReferenceValue        = txtDay.GetComponent<TMP_Text>();
            viewSO.FindProperty("customersLabel").objectReferenceValue  = txtCustomers.GetComponent<TMP_Text>();
            viewSO.FindProperty("dayProgressFill").objectReferenceValue = fillImg;
            viewSO.FindProperty("summaryPanel").objectReferenceValue    = summaryGO;
            viewSO.FindProperty("summaryTitleLabel").objectReferenceValue   = lblTitle.GetComponent<TMP_Text>();
            viewSO.FindProperty("summaryRevenueLabel").objectReferenceValue = lblRevenue.GetComponent<TMP_Text>();
            viewSO.FindProperty("summaryServedLabel").objectReferenceValue  = lblServed.GetComponent<TMP_Text>();
            viewSO.FindProperty("summaryLostLabel").objectReferenceValue    = lblLost.GetComponent<TMP_Text>();
            viewSO.FindProperty("nextDayButton").objectReferenceValue       = btnGO.GetComponent<Button>();
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static GameObject MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.pivot      = pivot;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
            return go;
        }

        private static void AddPanelBG(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
        }

        private static GameObject MakeLabel(Transform parent, string name, string text,
            TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var lbl = go.AddComponent<TextMeshProUGUI>();
            lbl.font      = font;
            lbl.fontSize  = size;
            lbl.text      = text;
            lbl.alignment = align;
            lbl.color     = Color.white;
            return go;
        }

        private static GameObject MakeButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.2f, 0.05f);
            rt.anchorMax        = new Vector2(0.8f, 0.22f);
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.95f);
            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.highlightedColor = new Color(0.3f, 0.65f, 1f);
            btn.colors = cb;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var tRT = txtGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;
            var t = txtGO.AddComponent<TextMeshProUGUI>();
            t.font      = font;
            t.fontSize  = 26f;
            t.text      = label;
            t.alignment = TextAlignmentOptions.Center;
            t.color     = Color.white;
            return go;
        }

        private static void LayoutThreeColumns(RectTransform left, RectTransform center, RectTransform right)
        {
            SetStretchRect(left,   new Vector2(0f,    0f), new Vector2(0.33f, 1f), new Vector2(10f, 5f),  new Vector2(0f,  -5f));
            SetStretchRect(center, new Vector2(0.33f, 0f), new Vector2(0.67f, 1f), new Vector2(0f,  5f),  new Vector2(0f,  -5f));
            SetStretchRect(right,  new Vector2(0.67f, 0f), new Vector2(1f,    1f), new Vector2(0f,  5f),  new Vector2(-10f,-5f));
        }

        private static void LayoutSummaryRows(
            GameObject title, GameObject revenue, GameObject served, GameObject lost)
        {
            SetStretchRect(title.GetComponent<RectTransform>(),
                new Vector2(0f, 0.72f), new Vector2(1f, 1f),   new Vector2(10f, 0f), new Vector2(-10f, 0f));
            SetStretchRect(revenue.GetComponent<RectTransform>(),
                new Vector2(0f, 0.52f), new Vector2(1f, 0.70f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            SetStretchRect(served.GetComponent<RectTransform>(),
                new Vector2(0f, 0.34f), new Vector2(1f, 0.52f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            SetStretchRect(lost.GetComponent<RectTransform>(),
                new Vector2(0f, 0.22f), new Vector2(1f, 0.34f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
        }

        private static void SetStretchRect(RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts  = path.Split('/');
            var parent = string.Join("/", parts, 0, parts.Length - 1);
            AssetDatabase.CreateFolder(parent, parts[parts.Length - 1]);
        }
    }
}
