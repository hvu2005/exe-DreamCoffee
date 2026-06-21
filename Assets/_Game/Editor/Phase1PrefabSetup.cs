using System;
using System.IO;
using DreamCafe.Gameplay.Customer;
using DreamCafe.Gameplay.Table;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 1 prefab creation.
    /// Menu: Tools > DreamCafé > Create Phase 1 Prefabs
    ///       Tools > DreamCafé > Setup Phase 1 Scene
    ///
    /// Creates:
    ///   Resources/Prefabs/CustomerPrefab  — circle body + patience bar, all View refs wired
    ///   Prefabs/TablePrefab               — brown square top + faint circle seat, TableController wired
    ///   Art/Sprites/circle.png            — white circle, center pivot
    ///   Art/Sprites/sq_center.png         — white square, center pivot
    ///   Art/Sprites/sq_left.png           — white square, LEFT pivot (for patience bar fill)
    /// </summary>
    public static class Phase1PrefabSetup
    {
        private const string ArtPath     = "Assets/_Game/Art/Sprites";
        private const string ResPrefabs  = "Assets/_Game/Resources/Prefabs";
        private const string ScenePrefabs = "Assets/_Game/Prefabs";

        // ─── Menu: Create Prefabs ─────────────────────────────────────────────

        [MenuItem("Tools/DreamCafé/Create Phase 1 Prefabs")]
        public static void CreatePhase1Prefabs()
        {
            EnsureFolder("Assets/_Game/Art");
            EnsureFolder(ArtPath);
            EnsureFolder("Assets/_Game/Resources");
            EnsureFolder(ResPrefabs);
            EnsureFolder(ScenePrefabs);

            var circle   = GetOrCreateSprite(ArtPath + "/circle.png",    MakeCircleTex, new Vector2(0.5f, 0.5f));
            var sqCenter = GetOrCreateSprite(ArtPath + "/sq_center.png", MakeSquareTex, new Vector2(0.5f, 0.5f));
            var sqLeft   = GetOrCreateSprite(ArtPath + "/sq_left.png",   MakeSquareTex, new Vector2(0f,   0.5f));

            if (circle == null || sqCenter == null || sqLeft == null)
            {
                EditorUtility.DisplayDialog("DreamCafé", "Sprite generation failed. Check Console for errors.", "OK");
                return;
            }

            CreateCustomerPrefab(circle, sqCenter, sqLeft);
            CreateTablePrefab(sqCenter, circle);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DreamCafé — Phase 1 Prefabs",
                "Prefabs created!\n\n" +
                "• Resources/Prefabs/CustomerPrefab.prefab\n" +
                "  - Body: white circle (tinted at runtime)\n" +
                "  - Patience bar: wired\n\n" +
                "• Prefabs/TablePrefab.prefab\n" +
                "  - Table top: brown square\n" +
                "  - Seat: faint blue circle\n\n" +
                "Next: run  Tools > DreamCafé > Setup Phase 1 Scene",
                "OK");
        }

        // ─── Menu: Setup Scene ────────────────────────────────────────────────

        [MenuItem("Tools/DreamCafé/Setup Phase 1 Scene")]
        public static void SetupPhase1Scene()
        {
            // SpawnPoint
            if (GameObject.Find("SpawnPoint") == null)
            {
                var sp = new GameObject("SpawnPoint");
                sp.transform.position = new Vector3(0f, 3f, 0f);
                Debug.Log("[Phase1Setup] SpawnPoint created at (0, 3, 0).");
            }

            // 4 Tables  — only add if not already in scene
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenePrefabs + "/TablePrefab.prefab");

            for (int i = 0; i < 4; i++)
            {
                string name = $"Table_{i}";
                if (GameObject.Find(name) != null) continue;

                GameObject go;
                if (tablePrefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                    go.name = name;
                }
                else
                {
                    go = new GameObject(name);
                    go.AddComponent<TableController>();
                }

                float x = -3f + i * 2f;
                go.transform.position = new Vector3(x, -1f, 0f);

                var ctrl = go.GetComponent<TableController>();
                if (ctrl != null)
                {
                    var so = new SerializedObject(ctrl);
                    so.FindProperty("tableIndex").intValue = i;
                    so.ApplyModifiedProperties();
                }
            }

            // Wire SpawnPoint into GameBootstrap
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<DreamCafe.App.GameBootstrap>();
            if (bootstrap != null)
            {
                var spawnGO = GameObject.Find("SpawnPoint");
                if (spawnGO != null)
                {
                    var so = new SerializedObject(bootstrap);
                    so.FindProperty("customerSpawnPoint").objectReferenceValue = spawnGO.transform;
                    so.ApplyModifiedProperties();
                    Debug.Log("[Phase1Setup] SpawnPoint wired into GameBootstrap.");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Phase1Setup] ✓ Scene setup complete — 4 tables + SpawnPoint added.");
            EditorUtility.DisplayDialog(
                "DreamCafé — Phase 1 Scene",
                "Scene updated!\n\n" +
                "• SpawnPoint at (0, 3, 0)\n" +
                "• Table_0..3 at y=-1, spaced 2 units apart\n" +
                "• GameBootstrap.customerSpawnPoint wired\n\n" +
                "Save the scene (Ctrl+S) then press Play.",
                "OK");
        }

        // ─── Prefab builders ─────────────────────────────────────────────────

        private static void CreateCustomerPrefab(Sprite bodySprite, Sprite bgSprite, Sprite fillSprite)
        {
            const string path = ResPrefabs + "/CustomerPrefab.prefab";

            var root = new GameObject("CustomerPrefab");
            root.AddComponent<CustomerController>();
            var view = root.AddComponent<CustomerView>();

            // Body — circle, white, tinted at runtime by CustomerData.tintColor
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(root.transform, false);
            bodyGO.transform.localScale = new Vector3(0.6f, 1f, 1f);
            var bodyRend = bodyGO.AddComponent<SpriteRenderer>();
            bodyRend.sprite = bodySprite;
            bodyRend.color = Color.white;
            bodyRend.sortingOrder = 1;

            // PatienceBar parent — controls bar size; children keep scale (1,1,1)
            var barParent = new GameObject("PatienceBar");
            barParent.transform.SetParent(root.transform, false);
            barParent.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            barParent.transform.localScale = new Vector3(0.7f, 0.1f, 1f);

            // BG — dark gray square, center pivot
            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(barParent.transform, false);
            var bgRend = bgGO.AddComponent<SpriteRenderer>();
            bgRend.sprite = bgSprite;
            bgRend.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            bgRend.sortingOrder = 2;

            // Fill — green square, LEFT pivot, positioned at left edge (-0.5 in parent space)
            // CustomerView sets fill.localScale.x = patience01 (0..1)
            // Left pivot + position at -0.5 = bar drains from the right ✓
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barParent.transform, false);
            fillGO.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            var fillRend = fillGO.AddComponent<SpriteRenderer>();
            fillRend.sprite = fillSprite;
            fillRend.color = new Color(0.2f, 0.85f, 0.2f);
            fillRend.sortingOrder = 3;

            // Wire CustomerView serialized fields
            var so = new SerializedObject(view);
            so.FindProperty("body").objectReferenceValue             = bodyRend;
            so.FindProperty("patienceBarRoot").objectReferenceValue  = barParent;
            so.FindProperty("patienceBarFill").objectReferenceValue  = fillGO.transform;
            so.FindProperty("patienceBarFillRenderer").objectReferenceValue = fillRend;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[Phase1Setup] ✓ CustomerPrefab saved → {path}");
        }

        private static void CreateTablePrefab(Sprite tableSprite, Sprite seatSprite)
        {
            const string path = ScenePrefabs + "/TablePrefab.prefab";

            var root = new GameObject("TablePrefab");
            var ctrl = root.AddComponent<TableController>();

            // Table top — brown square
            var topGO = new GameObject("Top");
            topGO.transform.SetParent(root.transform, false);
            topGO.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            var topRend = topGO.AddComponent<SpriteRenderer>();
            topRend.sprite = tableSprite;
            topRend.color = new Color(0.55f, 0.35f, 0.15f);
            topRend.sortingOrder = 0;

            // Seat indicator — faint blue circle, positioned in front of table
            var seatGO = new GameObject("Seat");
            seatGO.transform.SetParent(root.transform, false);
            seatGO.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            seatGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var seatRend = seatGO.AddComponent<SpriteRenderer>();
            seatRend.sprite = seatSprite;
            seatRend.color = new Color(0.6f, 0.75f, 1f, 0.45f);
            seatRend.sortingOrder = -1;

            // Wire seatTransform on TableController
            var so = new SerializedObject(ctrl);
            so.FindProperty("seatTransform").objectReferenceValue = seatGO.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[Phase1Setup] ✓ TablePrefab saved → {path}");
        }

        // ─── Sprite helpers ───────────────────────────────────────────────────

        private static Sprite GetOrCreateSprite(string assetPath, Func<Texture2D> factory, Vector2 pivot)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null) return existing;

            var tex   = factory();
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
            {
                imp.textureType        = TextureImporterType.Sprite;
                imp.spriteImportMode   = SpriteImportMode.Single;
                imp.spritePivot        = pivot;
                imp.filterMode         = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Texture2D MakeSquareTex()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
            var px  = new Color[S * S];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeCircleTex()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
            var px  = new Color[S * S];
            float cx = S * 0.5f, cy = S * 0.5f, r = S * 0.5f - 1f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                px[y * S + x] = (dx * dx + dy * dy) <= r * r ? Color.white : Color.clear;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
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
