using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ tách nền (remove background) tự động cho các bộ texture nội thất của DreamCafe.
    /// Chuyển đổi các file JPG có nền be/kem đặc thành file PNG có kênh Alpha trong suốt.
    /// Menu: DreamCafe > Setup > Process Transparent Decor Textures
    /// </summary>
    public static class DecorBackgroundRemover
    {
        [MenuItem("DreamCafe/Setup/Process Transparent Decor Textures")]
        public static void ProcessAllTextures()
        {
            ProcessTexture("Assets/_Game/Art/Decor/cafe_furniture_pack.jpg",
                           "Assets/_Game/Art/Decor/cafe_furniture_pack_transparent.png",
                           new Color32(247, 240, 239, 255), 30, true);

            ProcessTexture("Assets/_Game/Art/Decor/barista_counter_pack.jpg",
                           "Assets/_Game/Art/Decor/barista_counter_pack_transparent.png",
                           new Color32(245, 233, 214, 255), 35, false);

            SliceTransparentAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DecorBackgroundRemover] Đã hoàn tất tách nền trong suốt cho toàn bộ nội thất!");
        }

        private static void ProcessTexture(string inputPath, string outputPath, Color32 sampleBg, int tolerance, bool eraseLabels)
        {
            var importer = AssetImporter.GetAtPath(inputPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(inputPath);
            if (srcTex == null)
            {
                Debug.LogError($"[DecorBackgroundRemover] Không tìm thấy ảnh tại '{inputPath}'");
                return;
            }

            int w = srcTex.width;
            int h = srcTex.height;
            var pixels = srcTex.GetPixels32();
            var outPixels = new Color32[w * h];
            var visited = new bool[w * h];
            var q = new Queue<int>();

            // Kiểm tra pixel có thuộc dải màu nền không bằng khoảng cách màu Euclidean
            bool MatchesBg(Color32 c)
            {
                float dr = c.r - sampleBg.r;
                float dg = c.g - sampleBg.g;
                float db = c.b - sampleBg.b;
                float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);
                return dist <= tolerance;
            }

            // Đưa toàn bộ các pixel biên ngoài vào hàng đợi BFS
            for (int x = 0; x < w; x++)
            {
                int top = (h - 1) * w + x;
                int bot = x;
                if (!visited[top] && MatchesBg(pixels[top])) { visited[top] = true; q.Enqueue(top); }
                if (!visited[bot] && MatchesBg(pixels[bot])) { visited[bot] = true; q.Enqueue(bot); }
            }
            for (int y = 0; y < h; y++)
            {
                int left = y * w;
                int right = y * w + (w - 1);
                if (!visited[left] && MatchesBg(pixels[left])) { visited[left] = true; q.Enqueue(left); }
                if (!visited[right] && MatchesBg(pixels[right])) { visited[right] = true; q.Enqueue(right); }
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            // Loang màu (Flood Fill) từ ngoài vào
            while (q.Count > 0)
            {
                int curr = q.Dequeue();
                int cx = curr % w;
                int cy = curr / w;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                    {
                        int nidx = ny * w + nx;
                        if (!visited[nidx] && MatchesBg(pixels[nidx]))
                        {
                            visited[nidx] = true;
                            q.Enqueue(nidx);
                        }
                    }
                }
            }

            // Gán alpha = 0 cho pixel nền, giữ nguyên pixel vật thể
            for (int i = 0; i < pixels.Length; i++)
            {
                if (visited[i])
                {
                    outPixels[i] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    outPixels[i] = pixels[i];
                }
            }

            // Xóa sạch các nhãn ký hiệu (A1, A2, B1, ...) trên texture nếu có
            if (eraseLabels)
            {
                System.Action<int, int, int, int> erase = delegate(int xMin, int xMax, int yMin, int yMax)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        for (int x = xMin; x <= xMax; x++)
                        {
                            outPixels[y * w + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                };

                erase(70, 150, 660, 715); // A1
                erase(380, 460, 660, 715); // A2
                erase(670, 750, 660, 715); // A3
                erase(70, 150, 360, 420); // A4
                erase(380, 460, 360, 420); // A5
                erase(670, 750, 335, 390); // B1
                erase(70, 150, 45, 100); // B2
                erase(380, 460, 45, 100); // C1
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(outPixels);
            outTex.Apply();

            byte[] bytes = outTex.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);
            AssetDatabase.ImportAsset(outputPath);
            Object.DestroyImmediate(outTex);

            Debug.Log($"[DecorBackgroundRemover] Đã tạo file PNG trong suốt tại '{outputPath}'");
        }

        public static void SliceTransparentAtlases()
        {
            // Slicing cho cafe_furniture_pack_transparent.png
            string fPath = "Assets/_Game/Art/Decor/cafe_furniture_pack_transparent.png";
            var fImp = AssetImporter.GetAtPath(fPath) as TextureImporter;
            if (fImp != null)
            {
                fImp.textureType = TextureImporterType.Sprite;
                fImp.spriteImportMode = SpriteImportMode.Multiple;
                fImp.isReadable = true;
                fImp.alphaIsTransparency = true;

                var metas = new List<SpriteMetaData>
                {
                    new SpriteMetaData { name = "sprite_table_wood_round", rect = new Rect(86, 688, 218, 232), alignment = 9, pivot = new Vector2(0.5f, 0.25f) },
                    new SpriteMetaData { name = "sprite_sofa_armchair_r", rect = new Rect(390, 688, 242, 274), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_sofa_armchair_l", rect = new Rect(702, 688, 242, 274), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_sofa_armchair", rect = new Rect(390, 688, 242, 274), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_chair_r", rect = new Rect(86, 386, 196, 255), alignment = 9, pivot = new Vector2(0.5f, 0.18f) },
                    new SpriteMetaData { name = "sprite_chair_l", rect = new Rect(408, 386, 191, 255), alignment = 9, pivot = new Vector2(0.5f, 0.18f) },
                    new SpriteMetaData { name = "sprite_chair", rect = new Rect(86, 386, 196, 255), alignment = 9, pivot = new Vector2(0.5f, 0.18f) },
                    new SpriteMetaData { name = "sprite_bakery_display", rect = new Rect(668, 346, 283, 295), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_plant_monstera", rect = new Rect(424, 52, 213, 289), alignment = 9, pivot = new Vector2(0.5f, 0.15f) },
                    new SpriteMetaData { name = "sprite_planter_hydrangea", rect = new Rect(701, 54, 250, 265), alignment = 9, pivot = new Vector2(0.5f, 0.20f) }
                };

#pragma warning disable CS0618
                fImp.spritesheet = metas.ToArray();
#pragma warning restore CS0618
                EditorUtility.SetDirty(fImp);
                fImp.SaveAndReimport();
            }

            // Slicing cho barista_counter_pack_transparent.png
            string cPath = "Assets/_Game/Art/Decor/barista_counter_pack_transparent.png";
            var cImp = AssetImporter.GetAtPath(cPath) as TextureImporter;
            if (cImp != null)
            {
                cImp.textureType = TextureImporterType.Sprite;
                cImp.spriteImportMode = SpriteImportMode.Multiple;
                cImp.isReadable = true;
                cImp.alphaIsTransparency = true;

                var metas = new List<SpriteMetaData>
                {
                    new SpriteMetaData { name = "sprite_counter_emerald", rect = new Rect(280, 60, 491, 521), alignment = 9, pivot = new Vector2(0.5f, 0.25f) },
                    new SpriteMetaData { name = "sprite_coffee_machine", rect = new Rect(620, 400, 321, 281), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_menu_board", rect = new Rect(57, 633, 213, 338), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_wall_shelves", rect = new Rect(669, 630, 292, 341), alignment = 9, pivot = new Vector2(0.5f, 0.20f) },
                    new SpriteMetaData { name = "sprite_barista_smiling", rect = new Rect(770, 52, 191, 269), alignment = 9, pivot = new Vector2(0.5f, 0.15f) },
                    new SpriteMetaData { name = "sprite_barista_working", rect = new Rect(305, 620, 296, 317), alignment = 9, pivot = new Vector2(0.5f, 0.15f) },
                    new SpriteMetaData { name = "sprite_syrup_siphon", rect = new Rect(57, 55, 214, 546), alignment = 9, pivot = new Vector2(0.5f, 0.20f) }
                };

#pragma warning disable CS0618
                cImp.spritesheet = metas.ToArray();
#pragma warning restore CS0618
                EditorUtility.SetDirty(cImp);
                cImp.SaveAndReimport();
            }
        }
    }
}
