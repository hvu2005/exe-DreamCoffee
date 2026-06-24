using DreamCafe.Data;
using DreamCafe.UI.Shop;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 6 setup.
    /// Menu:  Tools > DreamCafé > Create Phase 6 Assets
    ///        Tools > DreamCafé > Setup Phase 6 Scene
    ///
    /// Create Phase 6 Assets:
    ///   Builds 4 UpgradeData.assets + UpgradeConfig.asset in Resources/.
    ///
    /// Setup Phase 6 Scene:
    ///   Creates the UpgradeShop Canvas (overlays HUD) with summary label,
    ///   4 upgrade cards, and "Start Day ▶" button; wires all refs to UpgradeShopView.
    /// </summary>
    public static class Phase6Setup
    {
        private const string ResPath  = "Assets/_Game/Resources";
        private const string DataPath = "Assets/_Game/Data/Upgrades";

        // ── Upgrade definitions ───────────────────────────────────────────────

        private struct UpgradeDef
        {
            public string id, name, desc;
            public UpgradeEffect effect;
            public float baseCost, costMult;
            public int maxLevel;
            public float[] values;
        }

        private static readonly UpgradeDef[] Upgrades =
        {
            new() {
                id = "rush_hour",    name = "Rush Hour",
                desc = "Shorten customer spawn interval — more customers, more revenue.",
                effect = UpgradeEffect.SpawnInterval,
                baseCost = 800f, costMult = 1.5f, maxLevel = 2,
                values = new[]{ 5f, 3f }
            },
            new() {
                id = "long_day",    name = "Extended Hours",
                desc = "Operate longer each day, giving more time to serve customers.",
                effect = UpgradeEffect.DayLength,
                baseCost = 1000f, costMult = 1.5f, maxLevel = 2,
                values = new[]{ 240f, 300f }
            },
            new() {
                id = "quality_roast", name = "Quality Roast",
                desc = "Premium ingredients boost tip amount per served order.",
                effect = UpgradeEffect.TipMultiplier,
                baseCost = 500f, costMult = 1.6f, maxLevel = 3,
                values = new[]{ 1.25f, 1.5f, 2.0f }
            },
            new() {
                id = "cozy_vibes",  name = "Cozy Vibes",
                desc = "Relaxing atmosphere slows patience drain for all customers.",
                effect = UpgradeEffect.PatienceMultiplier,
                baseCost = 600f, costMult = 1.5f, maxLevel = 2,
                values = new[]{ 1.25f, 1.5f }
            },
        };

        // ── Entry points ──────────────────────────────────────────────────────

        [MenuItem("Tools/DreamCafé/Create Phase 6 Assets")]
        public static void CreateAssets()
        {
            EnsureFolder(ResPath);
            EnsureFolder(DataPath);

            var configPath = ResPath + "/UpgradeConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(configPath)
                         ?? CreateConfig(configPath);

            var upgradeList = new UpgradeData[Upgrades.Length];
            for (int i = 0; i < Upgrades.Length; i++)
                upgradeList[i] = CreateOrLoadUpgrade(Upgrades[i]);

            var cfgSO = new SerializedObject(config);
            var arrProp = cfgSO.FindProperty("upgrades");
            arrProp.arraySize = upgradeList.Length;
            for (int i = 0; i < upgradeList.Length; i++)
                arrProp.GetArrayElementAtIndex(i).objectReferenceValue = upgradeList[i];
            cfgSO.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;

            EditorUtility.DisplayDialog("DreamCafé — Phase 6 Assets",
                "Created:\n" +
                "  Resources/UpgradeConfig.asset — lists 4 upgrades\n" +
                "  Data/Upgrades/rush_hour.asset\n" +
                "  Data/Upgrades/long_day.asset\n" +
                "  Data/Upgrades/quality_roast.asset\n" +
                "  Data/Upgrades/cozy_vibes.asset\n\n" +
                "Next: run  Tools > DreamCafé > Setup Phase 6 Scene",
                "OK");
        }

        [MenuItem("Tools/DreamCafé/Setup Phase 6 Scene")]
        public static void SetupScene()
        {
            if (Object.FindFirstObjectByType<UpgradeShopController>() != null)
            {
                EditorUtility.DisplayDialog("DreamCafé", "UpgradeShopController already exists.", "OK");
                return;
            }

            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                EditorUtility.DisplayDialog("DreamCafé",
                    "TMP default font missing — import TMP Essential Resources first.", "OK");
                return;
            }

            BuildShopCanvas(font);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("DreamCafé — Phase 6 Scene",
                "UpgradeShop canvas created!\n\n" +
                "Structure:\n" +
                "  UpgradeShop (Canvas, sort=20)\n" +
                "    ├─ Overlay (dark full-screen bg)\n" +
                "    ├─ Panel (centered content)\n" +
                "    │   ├─ SummaryLabel\n" +
                "    │   ├─ Card_rush_hour\n" +
                "    │   ├─ Card_long_day\n" +
                "    │   ├─ Card_quality_roast\n" +
                "    │   ├─ Card_cozy_vibes\n" +
                "    │   └─ BtnStartDay\n\n" +
                "Save (Ctrl+S) → Play → complete a day → upgrade shop appears.",
                "OK");
        }

        // ── Asset builders ────────────────────────────────────────────────────

        private static UpgradeConfig CreateConfig(string path)
        {
            var cfg = ScriptableObject.CreateInstance<UpgradeConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        private static UpgradeData CreateOrLoadUpgrade(UpgradeDef def)
        {
            var path = $"{DataPath}/{def.id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (existing != null) return existing;

            var asset             = ScriptableObject.CreateInstance<UpgradeData>();
            asset.upgradeId       = def.id;
            asset.displayName     = def.name;
            asset.description     = def.desc;
            asset.effectType      = def.effect;
            asset.baseCost        = def.baseCost;
            asset.costMultiplier  = def.costMult;
            asset.maxLevel        = def.maxLevel;
            asset.effectValues    = def.values;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ── Canvas builder ────────────────────────────────────────────────────

        private static void BuildShopCanvas(TMP_FontAsset font)
        {
            var root    = new GameObject("UpgradeShop");
            var canvas  = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // above HUD (10) and HUD summary
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<UpgradeShopController>();
            var view = root.AddComponent<UpgradeShopView>();

            // Dark full-screen overlay
            var overlay = MakeStretchPanel(root.transform, "Overlay", new Color(0f, 0f, 0f, 0.75f));

            // Centered content panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.2f, 0.1f);
            panelRT.anchorMax = new Vector2(0.8f, 0.9f);
            panelRT.sizeDelta = Vector2.zero;
            var panelBG = panel.AddComponent<Image>();
            panelBG.color = new Color(0.07f, 0.07f, 0.1f, 0.97f);

            // Summary label at top of panel
            var summaryGO  = MakeLabel(panel.transform, "SummaryLabel", "Day 1 Complete!", font, 28f,
                                       TextAlignmentOptions.Center);
            var summaryRT  = summaryGO.GetComponent<RectTransform>();
            summaryRT.anchorMin = new Vector2(0f, 0.78f);
            summaryRT.anchorMax = new Vector2(1f, 1f);
            summaryRT.offsetMin = new Vector2(20f, 0f);
            summaryRT.offsetMax = new Vector2(-20f, 0f);

            // 4 upgrade cards
            var cardViews = new UpgradeShopView.UpgradeCardUI[4];
            for (int i = 0; i < 4; i++)
            {
                float yMin = 0.78f - (i + 1) * 0.175f;
                float yMax = 0.78f - i * 0.175f - 0.01f;
                cardViews[i] = BuildCard(panel.transform, Upgrades[i], font, yMin, yMax);
            }

            // Start Day button
            var btnGO = MakeButton(panel.transform, "BtnStartDay", "Start Day ▶", font);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.25f, 0.02f);
            btnRT.anchorMax = new Vector2(0.75f, 0.10f);
            btnRT.sizeDelta = Vector2.zero;

            // Wire view
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("summaryLabel").objectReferenceValue   = summaryGO.GetComponent<TMP_Text>();
            viewSO.FindProperty("startDayButton").objectReferenceValue = btnGO.GetComponent<Button>();

            var cardsProp = viewSO.FindProperty("cards");
            cardsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var elem = cardsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("upgradeId").stringValue          = cardViews[i].upgradeId;
                elem.FindPropertyRelative("nameLabel").objectReferenceValue        = cardViews[i].nameLabel;
                elem.FindPropertyRelative("descriptionLabel").objectReferenceValue = cardViews[i].descriptionLabel;
                elem.FindPropertyRelative("levelLabel").objectReferenceValue       = cardViews[i].levelLabel;
                elem.FindPropertyRelative("costLabel").objectReferenceValue        = cardViews[i].costLabel;
                elem.FindPropertyRelative("buyButton").objectReferenceValue        = cardViews[i].buyButton;
            }
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false); // shown by UpgradeShopController.OnDayEnded
        }

        private static UpgradeShopView.UpgradeCardUI BuildCard(
            Transform parent, UpgradeDef def, TMP_FontAsset font, float yMin, float yMax)
        {
            var card = new GameObject($"Card_{def.id}");
            card.transform.SetParent(parent, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.02f, yMin);
            cardRT.anchorMax = new Vector2(0.98f, yMax);
            cardRT.sizeDelta = Vector2.zero;
            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // Name (left)
            var nameGO = MakeLabel(card.transform, "Name", def.name, font, 22f, TextAlignmentOptions.Left);
            SetRect(nameGO, new Vector2(0.01f, 0.5f), new Vector2(0.45f, 1f));

            // Description (left-bottom)
            var descGO = MakeLabel(card.transform, "Desc", def.desc, font, 14f, TextAlignmentOptions.Left);
            descGO.GetComponent<TMP_Text>().color = new Color(0.7f, 0.7f, 0.7f);
            SetRect(descGO, new Vector2(0.01f, 0f), new Vector2(0.55f, 0.5f));

            // Level label (center-right)
            var lvlGO = MakeLabel(card.transform, "Level", "Lv 0", font, 20f, TextAlignmentOptions.Center);
            SetRect(lvlGO, new Vector2(0.56f, 0.5f), new Vector2(0.70f, 1f));

            // Cost label (center-right-bottom)
            var costGO = MakeLabel(card.transform, "Cost", "500đ", font, 18f, TextAlignmentOptions.Center);
            costGO.GetComponent<TMP_Text>().color = new Color(1f, 0.85f, 0.3f);
            SetRect(costGO, new Vector2(0.56f, 0f), new Vector2(0.70f, 0.5f));

            // Buy button (right)
            var btnGO = MakeButton(card.transform, "BuyBtn", "Buy", font);
            SetRect(btnGO, new Vector2(0.72f, 0.1f), new Vector2(0.99f, 0.9f));

            return new UpgradeShopView.UpgradeCardUI
            {
                upgradeId       = def.id,
                nameLabel       = nameGO.GetComponent<TMP_Text>(),
                descriptionLabel = descGO.GetComponent<TMP_Text>(),
                levelLabel      = lvlGO.GetComponent<TMP_Text>(),
                costLabel       = costGO.GetComponent<TMP_Text>(),
                buyButton       = btnGO.GetComponent<Button>(),
            };
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static GameObject MakeStretchPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject MakeLabel(Transform parent, string name, string text,
            TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var lbl = go.AddComponent<TextMeshProUGUI>();
            lbl.font = font; lbl.fontSize = size; lbl.text = text;
            lbl.alignment = align; lbl.color = Color.white;
            return go;
        }

        private static GameObject MakeButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.95f);
            go.AddComponent<Button>();

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var tRT = txtGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one; tRT.sizeDelta = Vector2.zero;
            var t = txtGO.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = 22f; t.text = label;
            t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 2f); rt.offsetMax = new Vector2(-4f, -2f);
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
