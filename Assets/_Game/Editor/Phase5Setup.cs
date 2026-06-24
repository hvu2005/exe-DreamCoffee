using DreamCafe.Data;
using DreamCafe.Gameplay.Order;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 5 setup.
    /// Menu:  Tools > DreamCafé > Setup Phase 5 — Ticket Labels
    ///        Tools > DreamCafé > Create Sound Config Asset
    ///
    /// Setup Phase 5 — Ticket Labels:
    ///   Edits OrderTicketPrefab in-place: adds a TextMeshPro world-space child "ItemLabel"
    ///   and wires it to OrderTicketView.itemLabel. Idempotent.
    ///
    /// Create Sound Config Asset:
    ///   Creates Assets/_Game/Resources/SoundConfig.asset.
    ///   Fill in keys + AudioClips in Inspector.
    ///   Built-in keys: "serve_ding", "craft_complete", "customer_arrive", "customer_leave".
    /// </summary>
    public static class Phase5Setup
    {
        private const string ResPrefabs = "Assets/_Game/Resources/Prefabs";
        private const string ResPath    = "Assets/_Game/Resources";

        [MenuItem("Tools/DreamCafé/Setup Phase 5 — Ticket Labels")]
        public static void SetupTicketLabels()
        {
            var prefabPath = ResPrefabs + "/OrderTicketPrefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                EditorUtility.DisplayDialog("DreamCafé",
                    "OrderTicketPrefab not found.\nRun  Tools > DreamCafé > Create Phase 2 Prefabs  first.",
                    "OK");
                return;
            }

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                EditorUtility.DisplayDialog("DreamCafé",
                    "TMP default font not found.\nOpen  Window > TextMeshPro > Import TMP Essential Resources  first.",
                    "OK");
                return;
            }

            // Load prefab contents into a temporary scene for editing.
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabRoot.transform.Find("ItemLabel") != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                EditorUtility.DisplayDialog("DreamCafé",
                    "ItemLabel already exists in OrderTicketPrefab — nothing to do.", "OK");
                return;
            }

            // Create the world-space TMP label child.
            var labelGO = new GameObject("ItemLabel");
            labelGO.transform.SetParent(prefabRoot.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
            labelGO.transform.localScale    = new Vector3(0.28f, 0.28f, 1f);

            var tmp            = labelGO.AddComponent<TextMeshPro>();
            tmp.font           = font;
            tmp.fontSize       = 2f;
            tmp.alignment      = TextAlignmentOptions.Center;
            tmp.color          = new Color(0.1f, 0.1f, 0.1f);
            tmp.text           = "Item";
            tmp.sortingLayerID = SortingLayer.NameToID("Default");
            tmp.sortingOrder   = 6;
            tmp.enableWordWrapping = false;
            tmp.overflowMode   = TextOverflowModes.Ellipsis;

            // Wire to OrderTicketView.itemLabel
            var view = prefabRoot.GetComponent<OrderTicketView>();
            if (view != null)
            {
                var so = new SerializedObject(view);
                so.FindProperty("itemLabel").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("DreamCafé — Phase 5",
                "Done!\n\n" +
                "• OrderTicketPrefab: ItemLabel (TextMeshPro) child added.\n" +
                "• Wired to OrderTicketView.itemLabel.\n\n" +
                "Now run  Tools > DreamCafé > Create Sound Config Asset\n" +
                "to set up audio, then save + Play.",
                "OK");

            Debug.Log("[Phase5Setup] ✓ ItemLabel added to OrderTicketPrefab.");
        }

        [MenuItem("Tools/DreamCafé/Create Sound Config Asset")]
        public static void CreateSoundConfigAsset()
        {
            EnsureFolder(ResPath);
            const string path = ResPath + "/SoundConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<SoundConfig>(path) != null)
            {
                EditorUtility.DisplayDialog("DreamCafé", "SoundConfig.asset already exists at " + path, "OK");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SoundConfig>(path);
                return;
            }
            var asset = ScriptableObject.CreateInstance<SoundConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;

            EditorUtility.DisplayDialog("DreamCafé — Sound Config",
                "SoundConfig.asset created at Resources/SoundConfig.\n\n" +
                "Add entries in the Inspector:\n\n" +
                "  key: serve_ding      → drag AudioClip\n" +
                "  key: craft_complete  → drag AudioClip\n" +
                "  key: customer_arrive → drag AudioClip\n" +
                "  key: customer_leave  → drag AudioClip\n\n" +
                "Leave clips null to keep log-only fallback (no errors).",
                "OK");
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
