using DreamCafe.Gameplay.CraftingStation;
using DreamCafe.Gameplay.Input;
using DreamCafe.Gameplay.Order;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 2 prefab and scene setup.
    /// Menu: Tools > DreamCafé > Create Phase 2 Prefabs
    ///       Tools > DreamCafé > Setup Phase 2 Scene
    ///
    /// Creates:
    ///   Resources/Prefabs/OrderTicketPrefab  — yellow/green square, wired View refs
    ///   Prefabs/CraftingStationPrefab        — beige square + green status dot
    /// Adds to scene:
    ///   CraftingStation_0                    — near top-center
    ///   OrderTicketSpawner                   — near crafting station
    ///   PlayerInputRouter                    — on GameBootstrap or new object
    /// </summary>
    public static class Phase2PrefabSetup
    {
        private const string ResPrefabs   = "Assets/_Game/Resources/Prefabs";
        private const string ScenePrefabs = "Assets/_Game/Prefabs";
        private const string ArtPath      = "Assets/_Game/Art/Sprites";

        [MenuItem("Tools/DreamCafé/Create Phase 2 Prefabs")]
        public static void CreatePhase2Prefabs()
        {
            EnsureFolder("Assets/_Game/Resources");
            EnsureFolder(ResPrefabs);
            EnsureFolder(ScenePrefabs);

            // Reuse sprites already created by Phase 1 setup
            var sqCenter = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/sq_center.png");
            var circle   = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/circle.png");

            if (sqCenter == null || circle == null)
            {
                EditorUtility.DisplayDialog("DreamCafé",
                    "Phase 1 sprites not found.\nRun  Tools > DreamCafé > Create Phase 1 Prefabs  first.",
                    "OK");
                return;
            }

            CreateOrderTicketPrefab(sqCenter);
            CreateCraftingStationPrefab(sqCenter, circle);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DreamCafé — Phase 2 Prefabs",
                "Prefabs created!\n\n" +
                "• Resources/Prefabs/OrderTicketPrefab.prefab\n" +
                "  - Yellow = Pending, Green = Ready\n\n" +
                "• Prefabs/CraftingStationPrefab.prefab\n" +
                "  - Beige body + green status dot\n" +
                "  - BoxCollider2D wired for tap detection\n\n" +
                "Next: run  Tools > DreamCafé > Setup Phase 2 Scene",
                "OK");
        }

        [MenuItem("Tools/DreamCafé/Setup Phase 2 Scene")]
        public static void SetupPhase2Scene()
        {
            var stationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenePrefabs + "/CraftingStationPrefab.prefab");

            // ── Crafting station ─────────────────────────────────────────────
            if (GameObject.Find("CraftingStation_0") == null)
            {
                GameObject go;
                if (stationPrefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(stationPrefab);
                    go.name = "CraftingStation_0";
                }
                else
                {
                    go = new GameObject("CraftingStation_0");
                    go.AddComponent<CraftingStationController>();
                    go.AddComponent<BoxCollider2D>();
                }
                go.transform.position = new Vector3(0f, 2f, 0f);

                var ctrl = go.GetComponent<CraftingStationController>();
                if (ctrl != null)
                {
                    var so = new SerializedObject(ctrl);
                    so.FindProperty("stationId").stringValue = "station_0";
                    so.ApplyModifiedProperties();
                }
                Debug.Log("[Phase2Setup] CraftingStation_0 added at (0, 2, 0).");
            }

            // ── Order ticket spawner ─────────────────────────────────────────
            if (GameObject.Find("OrderTicketSpawner") == null)
            {
                var go = new GameObject("OrderTicketSpawner");
                go.AddComponent<OrderTicketSpawner>();
                go.transform.position = new Vector3(0f, 3.5f, 0f);

                // ticket anchor slightly above station
                var anchorGO = new GameObject("TicketAnchor");
                anchorGO.transform.SetParent(go.transform, false);
                anchorGO.transform.localPosition = Vector3.zero;

                var so = new SerializedObject(go.GetComponent<OrderTicketSpawner>());
                so.FindProperty("ticketAnchor").objectReferenceValue = anchorGO.transform;
                so.ApplyModifiedProperties();
                Debug.Log("[Phase2Setup] OrderTicketSpawner added.");
            }

            // ── Player input router ──────────────────────────────────────────
            if (GameObject.Find("PlayerInputRouter") == null)
            {
                var go = new GameObject("PlayerInputRouter");
                go.AddComponent<PlayerInputRouter>();
                Debug.Log("[Phase2Setup] PlayerInputRouter added.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "DreamCafé — Phase 2 Scene",
                "Scene updated!\n\n" +
                "• CraftingStation_0 at (0, 2, 0)\n" +
                "• OrderTicketSpawner at (0, 3.5, 0)\n" +
                "• PlayerInputRouter added\n\n" +
                "Save the scene (Ctrl+S) then press Play.\n\n" +
                "Test: wait ~11s → customer auto-orders → click station → ticket turns green.",
                "OK");
        }

        // ── Prefab builders ───────────────────────────────────────────────────

        private static void CreateOrderTicketPrefab(Sprite sprite)
        {
            const string path = ResPrefabs + "/OrderTicketPrefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var root = new GameObject("OrderTicketPrefab");
            root.AddComponent<OrderTicketController>();
            var view = root.AddComponent<OrderTicketView>();

            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(root.transform, false);
            bgGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var bgRend = bgGO.AddComponent<SpriteRenderer>();
            bgRend.sprite       = sprite;
            bgRend.color        = new Color(1f, 0.85f, 0.1f);
            bgRend.sortingOrder = 5;

            var so = new SerializedObject(view);
            so.FindProperty("background").objectReferenceValue = bgRend;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Phase2Setup] ✓ OrderTicketPrefab → {path}");
        }

        private static void CreateCraftingStationPrefab(Sprite bodySprite, Sprite dotSprite)
        {
            const string path = ScenePrefabs + "/CraftingStationPrefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var root   = new GameObject("CraftingStationPrefab");
            var ctrl   = root.AddComponent<CraftingStationController>();
            var view   = root.AddComponent<CraftingStationView>();
            var col    = root.AddComponent<BoxCollider2D>();
            col.size   = new Vector2(1.5f, 1f);

            // Set Tappable layer now if it already exists; Phase3Setup patches it otherwise.
            int tappableLayer = LayerMask.NameToLayer("Tappable");
            if (tappableLayer >= 0) root.layer = tappableLayer;

            // Body — beige square (the counter surface)
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(root.transform, false);
            bodyGO.transform.localScale = new Vector3(1.5f, 1f, 1f);
            var bodyRend = bodyGO.AddComponent<SpriteRenderer>();
            bodyRend.sprite       = bodySprite;
            bodyRend.color        = new Color(0.85f, 0.75f, 0.55f);
            bodyRend.sortingOrder = 0;

            // Status dot — small circle, top-right corner
            var dotGO = new GameObject("StatusDot");
            dotGO.transform.SetParent(root.transform, false);
            dotGO.transform.localPosition = new Vector3(0.55f, 0.45f, 0f);
            dotGO.transform.localScale    = new Vector3(0.3f, 0.3f, 1f);
            var dotRend = dotGO.AddComponent<SpriteRenderer>();
            dotRend.sprite       = dotSprite;
            dotRend.color        = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            dotRend.sortingOrder = 1;

            // Wire CraftingStationView refs
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("body").objectReferenceValue            = bodyRend;
            viewSO.FindProperty("statusIndicator").objectReferenceValue = dotRend;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire stationId on controller
            var ctrlSO = new SerializedObject(ctrl);
            ctrlSO.FindProperty("stationId").stringValue = "station_0";
            ctrlSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Phase2Setup] ✓ CraftingStationPrefab → {path}");
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
