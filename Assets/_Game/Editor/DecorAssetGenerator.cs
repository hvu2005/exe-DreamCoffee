using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh các asset đồ họa phụ trợ:
    /// - Chỉ báo ô sàn trống (slot_empty_floor.png)
    /// - Chỉ báo ô tường trống (slot_empty_wall.png)
    /// - Tranh nghệ thuật cà phê treo tường (prop_wall_painting_latte.png)
    /// Menu: DreamCafe > Setup > Generate Decor Extra Assets
    /// </summary>
    public static class DecorAssetGenerator
    {
        private const string ArtDir = "Assets/_Game/Art/Decor";

        [MenuItem("DreamCafe/Setup/Generate Decor Extra Assets")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(ArtDir);

            GenerateEmptyFloorIndicator();
            GenerateEmptyWallIndicator();
            GenerateWallPaintingLatte();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DecorAssetGenerator] Đã tạo thành công các asset đồ họa phụ trợ!");
        }

        private static void GenerateEmptyFloorIndicator()
        {
            int w = 240, h = 120;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 fillCol = new Color32(255, 240, 180, 50);
            Color32 borderCol = new Color32(255, 200, 80, 220);
            Color32 plusCol = new Color32(255, 255, 255, 240);
            Color32 shadowCol = new Color32(80, 50, 20, 100);

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float rx = w * 0.45f;
            float ry = h * 0.45f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - center.x) / rx;
                    float dy = Mathf.Abs(y - center.y) / ry;
                    float dist = dx + dy; // Diamond metric

                    if (dist <= 1.0f)
                    {
                        if (dist >= 0.88f)
                        {
                            // Dashed pattern
                            float angle = Mathf.Atan2(y - center.y, x - center.x);
                            bool dash = Mathf.Sin(angle * 12f) > -0.2f;
                            pixels[y * w + x] = dash ? borderCol : new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            pixels[y * w + x] = fillCol;
                        }
                    }
                    else
                    {
                        pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            // Draw center '+' symbol
            void DrawRect(int x0, int y0, int rw, int rh, Color32 col)
            {
                for (int py = y0; py < y0 + rh; py++)
                {
                    for (int px = x0; px < x0 + rw; px++)
                    {
                        if (px >= 0 && px < w && py >= 0 && py < h)
                            pixels[py * w + px] = col;
                    }
                }
            }

            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);

            // Shadow
            DrawRect(cx - 15, cy - 4, 30, 8, shadowCol);
            DrawRect(cx - 4, cy - 15, 8, 30, shadowCol);
            // Plus
            DrawRect(cx - 14, cy - 3, 28, 6, plusCol);
            DrawRect(cx - 3, cy - 14, 6, 28, plusCol);

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/slot_empty_floor.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        private static void GenerateEmptyWallIndicator()
        {
            int w = 160, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 fillCol = new Color32(230, 245, 210, 50);
            Color32 borderCol = new Color32(240, 210, 100, 220);
            Color32 plusCol = new Color32(255, 255, 255, 240);
            Color32 shadowCol = new Color32(40, 60, 40, 120);

            // Skewed frame along isometric angle (slope +0.5)
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;

                    // Border box with padding
                    if (u >= 0.1f && u <= 0.9f && v >= 0.1f && v <= 0.9f)
                    {
                        bool isEdge = (u <= 0.16f || u >= 0.84f || v <= 0.15f || v >= 0.85f);
                        if (isEdge)
                        {
                            bool dash = ((x / 10) + (y / 10)) % 2 == 0;
                            pixels[y * w + x] = dash ? borderCol : new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            pixels[y * w + x] = fillCol;
                        }
                    }
                    else
                    {
                        pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            // Draw center '+' symbol
            void DrawRect(int x0, int y0, int rw, int rh, Color32 col)
            {
                for (int py = y0; py < y0 + rh; py++)
                {
                    for (int px = x0; px < x0 + rw; px++)
                    {
                        if (px >= 0 && px < w && py >= 0 && py < h)
                            pixels[py * w + px] = col;
                    }
                }
            }

            int cx = w / 2;
            int cy = h / 2;

            DrawRect(cx - 16, cy - 4, 32, 8, shadowCol);
            DrawRect(cx - 4, cy - 16, 8, 32, shadowCol);
            DrawRect(cx - 14, cy - 3, 28, 6, plusCol);
            DrawRect(cx - 3, cy - 14, 6, 28, plusCol);

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/slot_empty_wall.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        private static void GenerateWallPaintingLatte()
        {
            int w = 180, h = 220;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 frameOuter = new Color32(93, 64, 55, 255);   // Dark wood frame
            Color32 frameInner = new Color32(141, 110, 99, 255); // Inner bevel
            Color32 canvasBg   = new Color32(255, 248, 235, 255); // Cream canvas
            Color32 cupCol     = new Color32(67, 160, 71, 255);   // Emerald green cup
            Color32 coffeeCol  = new Color32(109, 76, 65, 255);   // Rich espresso brown
            Color32 milkCol    = new Color32(255, 255, 255, 255); // Latte art milk
            Color32 heartCol   = new Color32(245, 235, 220, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Outer Frame
                    if (x < 14 || x >= w - 14 || y < 14 || y >= h - 14)
                    {
                        pixels[y * w + x] = frameOuter;
                    }
                    // Inner Bevel
                    else if (x < 22 || x >= w - 22 || y < 22 || y >= h - 22)
                    {
                        pixels[y * w + x] = frameInner;
                    }
                    // Canvas Content
                    else
                    {
                        pixels[y * w + x] = canvasBg;

                        // Draw Coffee Cup (Ellipse)
                        float cdx = (x - 90) / 45f;
                        float cdy = (y - 95) / 32f;
                        if (cdx * cdx + cdy * cdy <= 1f)
                        {
                            pixels[y * w + x] = cupCol;

                            // Coffee liquid
                            float kdx = (x - 90) / 37f;
                            float kdy = (y - 95) / 25f;
                            if (kdx * kdx + kdy * kdy <= 1f)
                            {
                                pixels[y * w + x] = coffeeCol;

                                // Latte Art Heart
                                float hx = (x - 90) * 0.08f;
                                float hy = (y - 97) * 0.08f;
                                float heart = (hx * hx + hy * hy - 1f);
                                if (heart * heart * heart - hx * hx * hy * hy * hy <= 0f)
                                {
                                    pixels[y * w + x] = milkCol;
                                }
                            }
                        }

                        // Saucer under cup
                        float sdx = (x - 90) / 58f;
                        float sdy = (y - 65) / 12f;
                        if (sdx * sdx + sdy * sdy <= 1f && y < 75)
                        {
                            pixels[y * w + x] = frameInner;
                        }

                        // Steam swirls
                        if ((x >= 80 && x <= 84 && y >= 140 && y <= 165) ||
                            (x >= 96 && x <= 100 && y >= 145 && y <= 175))
                        {
                            pixels[y * w + x] = new Color32(200, 190, 180, 160);
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/prop_wall_painting_latte.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        private static void SetupSprite(string path, float ppu, Vector2 pivot)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
    }
}
