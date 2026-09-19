using System.Collections.Generic;
using System.IO;
using System.Text;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Canh hình vẽ của từng prefab nội thất cho đúng ô sàn của nó: **ô lưới là mặt sàn món đồ đứng
    /// lên**, nên đáy của mảnh thân phải nằm trên ô đó chứ không lửng lơ bên trên.
    ///
    /// Đo bằng **pixel đục thật** của sprite chứ không dùng <c>sprite.bounds</c> — bounds tính cả
    /// viền trong suốt, có món lệch tới vài centimet, đủ để nhìn ra là đồ không chạm sàn.
    ///
    /// Đáy được đặt thụt xuống dưới tâm ô một quãng bằng nửa bề sâu của đế, ước theo bề ngang mảnh
    /// art: mảnh rộng bằng cả ô thì đế phủ trọn ô nên đáy chạm mép trước của ô, mảnh bé như chậu cây
    /// thì đế chỉ chiếm phần giữa. Kê tất cả sát mép trước như nhau thì đồ nhỏ trông như trôi ra
    /// khỏi ô.
    ///
    /// Kết quả chỉ ghi vào <see cref="GridOccupant"/>._artOffset — không đụng vào ô lưới, cũng không
    /// đụng vào vị trí từng sprite con. Chạy xong vẫn nhích tay tiếp được.
    ///
    /// Menu: Tools > DreamCafe > Canh art vào ô sàn
    /// </summary>
    public static class DecorArtAlignTool
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Decor";

        /// <summary>Một ô gạch isometric: rộng 1, cao 0.5.</summary>
        private static readonly Vector2 CellSize = new(1f, 0.5f);

        /// <summary>Số đo của một mảnh art, quy về gốc prefab.</summary>
        private readonly struct Piece
        {
            public readonly SpriteRenderer Renderer;
            public readonly float CenterX;
            public readonly float Bottom;
            public readonly float Width;

            public Piece(SpriteRenderer renderer, float centerX, float bottom, float width)
            {
                Renderer = renderer;
                CenterX = centerX;
                Bottom = bottom;
                Width = width;
            }
        }

        [MenuItem("Tools/DreamCafe/Canh art vào ô sàn", priority = 3)]
        private static void Align()
        {
            var textures = new Dictionary<string, Texture2D>();
            var report = new StringBuilder("[CanhArt] Kết quả:");

            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
                {
                    Align(AssetDatabase.GUIDToAssetPath(guid), textures, report);
                }
            }
            finally
            {
                foreach (var texture in textures.Values) Object.DestroyImmediate(texture);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        private static void Align(string path, Dictionary<string, Texture2D> textures, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var occupant = root.GetComponent<GridOccupant>();
                if (occupant == null) return;

                var pieces = Measure(root, textures);
                if (pieces.Count == 0) return;

                var body = PickBody(pieces, occupant);

                // Đế phủ được bao nhiêu phần bề ngang ô thì thụt xuống bấy nhiêu phần nửa ô.
                float depth = CellSize.y * 0.5f * Mathf.Clamp01(body.Width / CellSize.x);
                var delta = new Vector2(-body.CenterX, -depth - body.Bottom);

                var so = new SerializedObject(occupant);
                var offset = so.FindProperty("_artOffset");
                Vector2 before = offset.vector2Value;
                offset.vector2Value = before + delta;
                so.ApplyModifiedPropertiesWithoutUndo();
                occupant.ApplyArtOffset();

                PrefabUtility.SaveAsPrefabAsset(root, path);

                report.AppendLine();
                report.Append($"  {root.name,-26} thân '{body.Renderer.name}' rộng {body.Width:F2} → " +
                              $"đáy {body.Bottom:+0.000;-0.000} ⇒ {-depth:+0.000;-0.000}, " +
                              $"_artOffset {before:F3} → {offset.vector2Value:F3}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Mảnh "thân" — mảnh đứng trên chính ô sàn của món, dùng làm mốc canh.
        ///
        /// Chỉ xét những mảnh nằm ngay giữa món (ghế của bộ bàn lệch hẳn nửa ô sang bên nên tự
        /// loại), rồi lấy mảnh **thấp nhất**. Trong hình isometric mảnh thấp nhất là mảnh đứng gần
        /// người xem nhất, tức mảnh ở chính ô của món. Đừng lấy mảnh rộng nhất: bộ sofa có cái ghế
        /// bành còn to hơn cái bàn nước, canh nhầm vào nó là cả bộ tụt xuống nửa ô.
        /// </summary>
        private static Piece PickBody(List<Piece> pieces, GridOccupant occupant)
        {
            Piece best = pieces[0];
            float lowest = float.MaxValue;

            foreach (var piece in pieces)
            {
                if (Mathf.Abs(piece.CenterX) > CellSize.x * 0.25f) continue;
                if (piece.Bottom >= lowest) continue;

                lowest = piece.Bottom;
                best = piece;
            }

            if (lowest < float.MaxValue) return best;

            // Không mảnh nào ở giữa (món một mảnh kê lệch): đành lấy mảnh rộng nhất.
            foreach (var piece in pieces)
            {
                if (piece.Width > best.Width) best = piece;
            }
            return best;
        }

        /// <summary>
        /// Đo từng mảnh art: tâm ngang, đáy, bề ngang — tính theo **pixel đục**, quy về gốc prefab.
        /// </summary>
        private static List<Piece> Measure(GameObject root, Dictionary<string, Texture2D> textures)
        {
            var pieces = new List<Piece>();

            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var sprite = renderer.sprite;
                if (sprite == null) continue;

                var texture = LoadReadable(sprite.texture, textures);
                if (texture == null) continue;

                int x0 = (int)sprite.rect.x, y0 = (int)sprite.rect.y;
                int w = (int)sprite.rect.width, h = (int)sprite.rect.height;
                var pixels = texture.GetPixels(x0, y0, w, h);

                int minY = h, minX = w, maxX = -1;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 0.05f) continue;
                    if (y < minY) minY = y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
                if (maxX < 0) continue;

                float ppu = sprite.pixelsPerUnit;
                var scale = renderer.transform.lossyScale;
                var origin = renderer.transform.position;

                float bottom = origin.y + (minY - sprite.pivot.y) / ppu * scale.y;
                float centerX = origin.x + ((minX + maxX) * 0.5f - sprite.pivot.x) / ppu * scale.x;
                float width = (maxX - minX + 1) / ppu * scale.x;

                pieces.Add(new Piece(renderer, centerX, bottom, width));
            }

            return pieces;
        }

        /// <summary>
        /// Texture của sprite trong dự án không bật Read/Write, nên nạp lại thẳng từ file ảnh thay
        /// vì đổi thiết lập import — đổi thiết lập là bắt Unity reimport cả bộ art, chậm mà còn làm
        /// bẩn file .meta của người khác.
        /// </summary>
        private static Texture2D LoadReadable(Texture source, Dictionary<string, Texture2D> cache)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            if (cache.TryGetValue(path, out var cached)) return cached;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Object.DestroyImmediate(texture);
                return null;
            }

            cache[path] = texture;
            return texture;
        }
    }
}
