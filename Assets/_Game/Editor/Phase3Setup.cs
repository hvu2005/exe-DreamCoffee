using DreamCafe.Gameplay.Table;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click Phase 3 scene setup.
    /// Menu: Tools > DreamCafé > Setup Phase 3 Scene
    ///
    /// What it does:
    ///   • Finds all TableController objects in the scene.
    ///   • Adds a BoxCollider2D (size 1×1) on the "Tappable" layer to each table that lacks one.
    ///   • Tables with a collider already present are skipped (idempotent).
    ///
    /// Phase 3 serving mechanic:
    ///   Player taps a table → Physics2D.OverlapPoint → TableController.OnTap()
    ///   → IOrderService.TryGetReadyOrderForCustomer → ServeOrder → OrderServed event
    ///   → Customer transitions Eating → PaymentReceived → EconomyService adds revenue.
    ///
    /// Prerequisites: Phase 1 + Phase 2 scene setup already run; tables exist in the scene.
    /// </summary>
    public static class Phase3Setup
    {
        [MenuItem("Tools/DreamCafé/Setup Phase 3 Scene")]
        public static void SetupPhase3Scene()
        {
            var tables = Object.FindObjectsByType<TableController>(FindObjectsSortMode.None);

            if (tables.Length == 0)
            {
                EditorUtility.DisplayDialog("DreamCafé — Phase 3",
                    "No TableController objects found in the scene.\n\nRun  Tools > DreamCafé > Setup Phase 1 Scene  first to add tables.",
                    "OK");
                return;
            }

            int tappableLayer = GetOrCreateTappableLayer();
            int added         = 0;

            foreach (var table in tables)
            {
                Undo.RecordObject(table.gameObject, "Phase3 — Add Table BoxCollider2D");

                if (table.GetComponent<BoxCollider2D>() == null)
                {
                    var col  = Undo.AddComponent<BoxCollider2D>(table.gameObject);
                    col.size = new Vector2(1f, 1f);
                    added++;
                    Debug.Log($"[Phase3Setup] BoxCollider2D added to {table.name}.");
                }

                if (tappableLayer >= 0)
                    table.gameObject.layer = tappableLayer;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "DreamCafé — Phase 3 Scene",
                $"Done!\n\n" +
                $"• {tables.Length} table(s) found.\n" +
                $"• BoxCollider2D added to {added} table(s).\n" +
                $"• Layer set to \"Tappable\" on all tables.\n\n" +
                "Save the scene (Ctrl+S) then press Play.\n\n" +
                "Full loop test:\n" +
                "  1. Wait ~8s → customer spawns → walks to seat.\n" +
                "  2. Wait ~3s → customer orders (ticket appears).\n" +
                "  3. Click the crafting station → ticket turns green.\n" +
                "  4. Click the table → OrderServed → customer eats.\n" +
                "  5. Customer leaves → Economy balance increases.\n" +
                "  6. Console: [OrderService] PaymentReceived logged.",
                "OK");
        }

        private static int GetOrCreateTappableLayer()
        {
            for (int i = 0; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == "Tappable")
                    return i;
            }

            // Layer doesn't exist yet — inform the dev (can't add layers programmatically at runtime)
            Debug.LogWarning("[Phase3Setup] Layer \"Tappable\" not found. " +
                             "Open Edit > Project Settings > Tags & Layers and add \"Tappable\" as a User Layer, " +
                             "then re-run Setup Phase 3 Scene.");
            return -1;
        }
    }
}
