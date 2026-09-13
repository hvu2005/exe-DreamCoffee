using DreamCafe.SystemControl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ dựng Top Bar Currency UI trên đỉnh đầu Canvas + lưu thành Prefab.
    /// Menu: DreamCafe > Setup > Top Bar Currency UI
    /// </summary>
    public static class TopBarUISetup
    {
        private const string BarName = "TopBarCurrencyPanel";
        private const string PrefabPath = "Assets/_Game/Prefabs/TopBarCurrencyPanel.prefab";

        private static readonly Color BgColor = new(0.96f, 0.93f, 0.86f, 0.92f); // Màu kem nền thanh top
        private static readonly Color BadgeBg = new(1f, 1f, 1f, 0.85f);          // Nền từng ô badge
        private static readonly Color DarkBrown = new(0.290f, 0.212f, 0.125f);
        private static readonly Color GreenIncome = new(0.22f, 0.55f, 0.24f);
        private static readonly Color GoldRep = new(0.85f, 0.55f, 0.12f);

        private const float BarHeight = 72f;
        private const float BadgeHeight = 48f;
        private const float BadgeWidth = 220f;

        [MenuItem("DreamCafe/Setup/Top Bar Currency UI")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Top Bar UI", "Không tìm thấy Canvas trong scene đang mở.", "OK");
                return;
            }

            var existing = canvas.transform.Find(BarName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var bar = NewUI(BarName, canvas.transform);
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, BarHeight);

            var barImg = bar.AddComponent<Image>();
            barImg.color = BgColor;

            var hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 24f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. Badge Tiền mặt
            var moneyText = BuildBadge(bar.transform, "MoneyBadge", "VNĐ", "500,000 đ", DarkBrown);

            // 2. Badge Tiền / giây (Money Per Second)
            var mpsText = BuildBadge(bar.transform, "MpsBadge", "+/s", "+0 đ/s", GreenIncome);

            // 3. Badge Danh tiếng (Reputation)
            var repText = BuildBadge(bar.transform, "ReputationBadge", "REP", "0 Rep", GoldRep);

            // Gắn component TopBarCurrencyView
            var view = bar.AddComponent<TopBarCurrencyView>();
            var so = new SerializedObject(view);
            so.FindProperty("_moneyText").objectReferenceValue = moneyText;
            so.FindProperty("_mpsText").objectReferenceValue = mpsText;
            so.FindProperty("_reputationText").objectReferenceValue = repText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Lưu thành Prefab
            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            PrefabUtility.SaveAsPrefabAssetAndConnect(bar, PrefabPath, InteractionMode.AutomatedAction);

            Undo.RegisterCreatedObjectUndo(bar, "Create Top Bar Currency UI");
            Selection.activeGameObject = bar;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log($"[TopBarUISetup] Đã dựng '{BarName}' thành công tại đỉnh Canvas và lưu prefab tại '{PrefabPath}'.");
        }

        private static TextMeshProUGUI BuildBadge(Transform parent, string name, string iconStr, string defaultVal, Color valueColor)
        {
            var badge = NewUI(name, parent);
            var rect = (RectTransform)badge.transform;
            rect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);

            var img = badge.AddComponent<Image>();
            img.color = BadgeBg;

            var hlg = badge.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(12, 12, 4, 4);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Icon Label
            var iconGo = NewUI("Icon", badge.transform);
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            iconTmp.text = iconStr;
            iconTmp.fontSize = 22f;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = valueColor;
            ((RectTransform)iconGo.transform).sizeDelta = new Vector2(28f, BadgeHeight);

            // Value Text
            var valGo = NewUI("Value", badge.transform);
            var valTmp = valGo.AddComponent<TextMeshProUGUI>();
            valTmp.text = defaultVal;
            valTmp.fontSize = 18f;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.alignment = TextAlignmentOptions.Left;
            valTmp.color = valueColor;
            ((RectTransform)valGo.transform).sizeDelta = new Vector2(BadgeWidth - 52f, BadgeHeight);

            return valTmp;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }
    }
}
