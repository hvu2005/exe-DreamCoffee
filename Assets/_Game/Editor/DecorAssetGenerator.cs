using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh các asset đồ họa phụ trợ chuẩn phối cảnh Isometric 2.5D:
    /// - Chỉ báo ô sàn trống (slot_empty_floor.png)
    /// - Chỉ báo ô tường trống (slot_empty_wall.png)
    /// - Tranh nghệ thuật cà phê treo tường (prop_wall_painting_latte.png)
    /// - Bảng thực đơn gỗ treo tường (prop_wall_menu.png)
    /// - Kệ ly tách gỗ treo tường (prop_wall_shelves.png)
    /// - Bồn hoa cẩm tú cầu chân đế isometric (prop_planter_hydrangea.png)
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
            GenerateWallMenu();
            GenerateWallShelves();
            GenerateWallDecorRightVariants();
            GenerateIsometricPlanter();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DecorAssetGenerator] Đã tạo thành công toàn bộ các asset đồ họa Decor chuẩn Isometric!");
        }

        public static void GenerateEmptyFloorIndicator()
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
                    float dist = dx + dy;

                    if (dist <= 1.0f)
                    {
                        if (dist >= 0.88f)
                        {
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

            DrawRect(cx - 15, cy - 4, 30, 8, shadowCol);
            DrawRect(cx - 4, cy - 15, 8, 30, shadowCol);
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

        public static void GenerateEmptyWallIndicator()
        {
            int w = 160, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 fillCol = new Color32(255, 248, 220, 50);
            Color32 borderCol = new Color32(248, 198, 62, 255);
            Color32 plusCol = new Color32(255, 255, 255, 255);
            Color32 shadowCol = new Color32(70, 50, 20, 180);

            int xc = w / 2;
            int yc = h / 2;
            int wp = 104;
            int hp = 110;
            float slope = 0.5f;

            int xmin = xc - wp / 2;
            int xmax = xc + wp / 2;
            float ybase = yc - hp * 0.5f;

            float invSqrt125 = 1f / Mathf.Sqrt(1f + slope * slope);
            float T = 4.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= xmin && x <= xmax)
                    {
                        float yb = ybase + slope * (x - xc);
                        float yt = yb + hp;

                        if (y >= yb && y <= yt)
                        {
                            float dLeft = x - xmin;
                            float dRight = xmax - x;
                            float dBottom = (y - yb) * invSqrt125;
                            float dTop = (yt - y) * invSqrt125;

                            float dEdge = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

                            if (dEdge < T)
                            {
                                bool isCorner = (dLeft < T * 1.8f && dBottom < T * 1.8f) ||
                                                (dRight < T * 1.8f && dBottom < T * 1.8f) ||
                                                (dLeft < T * 1.8f && dTop < T * 1.8f) ||
                                                (dRight < T * 1.8f && dTop < T * 1.8f);

                                bool dash = false;
                                if (isCorner)
                                {
                                    dash = true;
                                }
                                else if (dLeft < dBottom && dLeft < dTop)
                                {
                                    float s = y - (ybase + slope * (xmin - xc));
                                    dash = ((int)(s / 9f)) % 2 == 0;
                                }
                                else if (dRight < dBottom && dRight < dTop)
                                {
                                    float s = y - (ybase + slope * (xmax - xc));
                                    dash = ((int)(s / 9f)) % 2 == 0;
                                }
                                else
                                {
                                    float s = (x - xmin) * 1.118034f;
                                    dash = ((int)(s / 9f)) % 2 == 0;
                                }

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
                    else
                    {
                        pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - xc;
                    float dy = y - yc;
                    float dySlanted = (dy - slope * dx) * invSqrt125;

                    bool isVertCore = Mathf.Abs(dx) <= 2.5f && Mathf.Abs(dy) <= 13f;
                    bool isSlantCore = Mathf.Abs(dx) <= 13f && Mathf.Abs(dySlanted) <= 2.5f;

                    bool isVertOutline = Mathf.Abs(dx) <= 3.8f && Mathf.Abs(dy) <= 14.5f;
                    bool isSlantOutline = Mathf.Abs(dx) <= 14.5f && Mathf.Abs(dySlanted) <= 3.8f;

                    if (isVertCore || isSlantCore)
                    {
                        pixels[y * w + x] = plusCol;
                    }
                    else if (isVertOutline || isSlantOutline)
                    {
                        pixels[y * w + x] = shadowCol;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/slot_empty_wall.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        public static void GenerateWallPaintingLatte()
        {
            int w = 160, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 frameOuter = new Color32(78, 52, 46, 255);    // Mahogany wood
            Color32 frameInner = new Color32(109, 76, 65, 255);   // Wood bevel
            Color32 goldTrim   = new Color32(218, 165, 32, 255);   // Golden inner border
            Color32 canvasBg   = new Color32(253, 248, 238, 255);  // Cream textured canvas
            Color32 saucerCol  = new Color32(140, 160, 145, 255);  // Sage green saucer
            Color32 cupOuter   = new Color32(46, 125, 50, 255);    // Emerald cup
            Color32 cupShade   = new Color32(27, 94, 32, 255);     // Cup shadow
            Color32 coffeeCol  = new Color32(93, 58, 38, 255);     // Dark espresso
            Color32 milkCol    = new Color32(255, 253, 245, 255);  // Latte milk
            Color32 steamCol   = new Color32(200, 190, 180, 140);  // Delicate steam
            Color32 shadowCol  = new Color32(40, 25, 15, 120);     // Drop shadow

            int xc = w / 2;
            int yc = h / 2;
            int wp = 104;
            int hp = 110;
            float slope = 0.5f;

            float hw = wp * 0.5f;
            float hh = hp * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - xc;
                    float u = dx / hw;
                    float ySlanted = (y - yc) - slope * dx;
                    float v = ySlanted / hh;

                    if (Mathf.Abs(u) <= 1.0f && Mathf.Abs(v) <= 1.0f)
                    {
                        float au = Mathf.Abs(u);
                        float av = Mathf.Abs(v);
                        float edgeDist = Mathf.Max(au, av);

                        if (edgeDist > 0.84f)
                        {
                            if (u < 0 || v > 0) pixels[y * w + x] = new Color32(95, 65, 58, 255);
                            else pixels[y * w + x] = frameOuter;
                        }
                        else if (edgeDist > 0.74f)
                        {
                            pixels[y * w + x] = frameInner;
                        }
                        else if (edgeDist > 0.68f)
                        {
                            pixels[y * w + x] = goldTrim;
                        }
                        else
                        {
                            pixels[y * w + x] = canvasBg;

                            float sDx = u / 0.44f;
                            float sDy = (v - (-0.28f)) / 0.12f;
                            if (sDx * sDx + sDy * sDy <= 1.0f)
                            {
                                pixels[y * w + x] = saucerCol;
                            }

                            float cDx = u / 0.32f;
                            float cDy = (v - (-0.08f)) / 0.24f;
                            if (cDx * cDx + cDy * cDy <= 1.0f && v >= -0.26f)
                            {
                                pixels[y * w + x] = (u < 0) ? cupOuter : cupShade;

                                float kDx = u / 0.28f;
                                float kDy = (v - 0.05f) / 0.11f;
                                if (kDx * kDx + kDy * kDy <= 1.0f)
                                {
                                    pixels[y * w + x] = coffeeCol;

                                    float hx = u * 4.2f;
                                    float hy = (v - 0.05f) * 8.5f;
                                    float heartVal = (hx * hx + hy * hy - 1.0f);
                                    if (heartVal * heartVal * heartVal - hx * hx * hy * hy * hy <= 0f)
                                    {
                                        pixels[y * w + x] = milkCol;
                                    }
                                }
                            }

                            float hDx = (u - 0.32f) / 0.10f;
                            float hDy = (v - (-0.05f)) / 0.14f;
                            float hDist = hDx * hDx + hDy * hDy;
                            if (hDist <= 1.0f && hDist >= 0.35f && u > 0.24f)
                            {
                                pixels[y * w + x] = cupOuter;
                            }

                            if (v >= 0.20f && v <= 0.58f)
                            {
                                float st1 = Mathf.Sin(v * 18f + 1f) * 0.08f;
                                float st2 = Mathf.Sin(v * 16f + 3f) * 0.08f;
                                if (Mathf.Abs(u - (-0.08f + st1)) < 0.025f ||
                                    Mathf.Abs(u - (0.08f + st2)) < 0.025f)
                                {
                                    pixels[y * w + x] = steamCol;
                                }
                            }
                        }
                    }
                    else
                    {
                        float sU = (dx - 3f) / hw;
                        float sV = ((y - yc - 2f) - slope * (dx - 3f)) / hh;
                        if (Mathf.Abs(sU) <= 1.02f && Mathf.Abs(sV) <= 1.02f && (u > 0.95f || v < -0.95f))
                        {
                            pixels[y * w + x] = shadowCol;
                        }
                        else
                        {
                            pixels[y * w + x] = clear;
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

        public static void GenerateWallMenu()
        {
            int w = 160, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 woodFrame = new Color32(141, 110, 99, 255);
            Color32 slateBg   = new Color32(44, 48, 56, 255);
            Color32 chalkGold = new Color32(255, 215, 64, 255);
            Color32 chalkText = new Color32(245, 245, 245, 220);
            Color32 chalkSub  = new Color32(180, 210, 230, 200);

            int xc = w / 2;
            int yc = h / 2;
            int wp = 104;
            int hp = 120;
            float slope = 0.5f;

            float hw = wp * 0.5f;
            float hh = hp * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - xc;
                    float u = dx / hw;
                    float ySlanted = (y - yc) - slope * dx;
                    float v = ySlanted / hh;

                    if (Mathf.Abs(u) <= 1.0f && Mathf.Abs(v) <= 1.0f)
                    {
                        float au = Mathf.Abs(u);
                        float av = Mathf.Abs(v);
                        float edgeDist = Mathf.Max(au, av);

                        if (edgeDist > 0.85f)
                        {
                            pixels[y * w + x] = woodFrame;
                        }
                        else
                        {
                            pixels[y * w + x] = slateBg;

                            if (v >= 0.52f && v <= 0.68f && Mathf.Abs(u) <= 0.60f)
                            {
                                pixels[y * w + x] = chalkGold;
                            }

                            if (Mathf.Abs(v - 0.44f) <= 0.02f && Mathf.Abs(u) <= 0.65f)
                            {
                                pixels[y * w + x] = chalkGold;
                            }

                            float[] rowV = { 0.24f, 0.02f, -0.20f, -0.42f };
                            for (int r = 0; r < rowV.Length; r++)
                            {
                                float rv = rowV[r];
                                if (Mathf.Abs(v - rv) <= 0.035f && u >= -0.65f && u <= 0.15f)
                                {
                                    pixels[y * w + x] = chalkText;
                                }
                                if (Mathf.Abs(v - rv) <= 0.035f && u >= 0.35f && u <= 0.65f)
                                {
                                    pixels[y * w + x] = chalkSub;
                                }
                            }

                            float bDx = u / 0.18f;
                            float bDy = (v - (-0.68f)) / 0.08f;
                            if (bDx * bDx + bDy * bDy <= 1.0f && bDy >= -0.8f)
                            {
                                pixels[y * w + x] = chalkGold;
                            }
                        }
                    }
                    else
                    {
                        pixels[y * w + x] = clear;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/prop_wall_menu.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        public static void GenerateWallShelves()
        {
            int w = 170, h = 180;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 woodDark  = new Color32(109, 76, 65, 255);
            Color32 woodLight = new Color32(161, 136, 127, 255);
            Color32 mugCeramic= new Color32(240, 240, 245, 255);
            Color32 mugTeal   = new Color32(0, 150, 136, 255);
            Color32 potTerra  = new Color32(216, 112, 60, 255);
            Color32 leafGreen = new Color32(76, 175, 80, 255);

            int xc = w / 2;
            int yc = h / 2;
            int wp = 112;
            float slope = 0.5f;

            float hw = wp * 0.5f;
            float[] shelfY = { yc + 28f, yc - 28f };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = clear;
                    float dx = x - xc;
                    if (Mathf.Abs(dx) <= hw)
                    {
                        for (int s = 0; s < shelfY.Length; s++)
                        {
                            float sy = shelfY[s] + slope * dx;
                            float dy = y - sy;

                            if (dy >= -5f && dy <= 5f)
                            {
                                pixels[y * w + x] = (dy > 1f) ? woodLight : woodDark;
                            }

                            if (s == 0)
                            {
                                if (Mathf.Abs(dx - (-25f)) <= 10f && dy > 5f && dy <= 25f)
                                {
                                    pixels[y * w + x] = mugCeramic;
                                }
                                if (Mathf.Abs(dx) <= 12f && dy > 5f && dy <= 30f)
                                {
                                    pixels[y * w + x] = mugTeal;
                                }
                                if (Mathf.Abs(dx - 28f) <= 9f && dy > 5f && dy <= 17f)
                                {
                                    pixels[y * w + x] = potTerra;
                                }
                                if (Mathf.Abs(dx - 28f) <= 13f && dy > 17f && dy <= 29f)
                                {
                                    pixels[y * w + x] = leafGreen;
                                }
                            }
                            else
                            {
                                if (Mathf.Abs(dx - (-30f)) <= 8f && dy > 5f && dy <= 22f)
                                {
                                    pixels[y * w + x] = mugTeal;
                                }
                                if (Mathf.Abs(dx - (-10f)) <= 8f && dy > 5f && dy <= 22f)
                                {
                                    pixels[y * w + x] = mugCeramic;
                                }
                                if (Mathf.Abs(dx - 18f) <= 16f && dy > 5f && dy <= 20f)
                                {
                                    pixels[y * w + x] = woodDark;
                                }
                            }
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/prop_wall_shelves.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.5f));
            Object.DestroyImmediate(tex);
        }

        public static void GenerateWallDecorRightVariants()
        {
            string[] baseFiles = new string[]
            {
                $"{ArtDir}/prop_wall_painting_latte.png",
                $"{ArtDir}/prop_wall_menu.png",
                $"{ArtDir}/prop_wall_shelves.png"
            };

            foreach (var origPath in baseFiles)
            {
                if (!File.Exists(origPath)) continue;

                var origBytes = File.ReadAllBytes(origPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(origBytes);

                int w = tex.width;
                int h = tex.height;
                var origPixels = tex.GetPixels32();
                var flippedPixels = new Color32[w * h];

                // Xoay trục Y 180 độ (lật theo phương ngang: x -> w - 1 - x)
                // Đổi góc nghiêng từ Slope +0.5 (Tường trái) sang Slope -0.5 (Tường phải)
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        flippedPixels[y * w + x] = origPixels[y * w + (w - 1 - x)];
                    }
                }

                var flippedTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                flippedTex.SetPixels32(flippedPixels);
                flippedTex.Apply();

                string outPath = origPath.Replace(".png", "_right.png");
                File.WriteAllBytes(outPath, flippedTex.EncodeToPNG());
                AssetDatabase.ImportAsset(outPath);

                SetupSprite(outPath, 200f, new Vector2(0.5f, 0.5f));

                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(flippedTex);
            }
        }

        public static void GenerateIsometricPlanter()
        {
            int w = 240, h = 190;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            Color32 clear          = new Color32(0, 0, 0, 0);
            Color32 woodFaceRight  = new Color32(202, 152, 108, 255); // Right face (lit)
            Color32 woodFaceLeft   = new Color32(148, 105, 68, 255);  // Left face (shaded)
            Color32 woodPlankGap   = new Color32(105, 72, 45, 255);   // Dark plank lines
            Color32 woodCornerPost = new Color32(120, 82, 52, 255);   // Corner brackets
            Color32 soilColor      = new Color32(58, 42, 28, 255);    // Potting soil
            Color32 leafDark       = new Color32(42, 108, 44, 255);
            Color32 leafBright     = new Color32(76, 175, 80, 255);
            Color32 hydrWhite      = new Color32(253, 253, 255, 255);
            Color32 hydrBlue       = new Color32(178, 210, 245, 255);
            Color32 hydrShade      = new Color32(215, 225, 235, 255);
            Color32 hydrCore       = new Color32(255, 240, 180, 255);
            Color32 shadowFloor    = new Color32(50, 40, 30, 85);

            int xc = w / 2; // 120
            int ybase = 32;
            int lx = 46;   // Left face width along X
            int rx = 66;   // Right face width along X
            float hBox = 38f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = clear;
                    float dx = x - xc;

                    // 1. Bóng đổ trên mặt sàn (Floor shadow parallel to grid)
                    float sU = (y - (ybase - 4f)) - 0.5f * (dx - 4f);
                    float sV = (y - (ybase - 4f)) + 0.5f * (dx - 4f);
                    if (sU >= -10f && sU <= hBox * 0.7f && sV >= -10f && sV <= hBox * 0.7f)
                    {
                        if (dx >= -lx - 6 && dx <= rx + 6)
                        {
                            pixels[y * w + x] = shadowFloor;
                        }
                    }

                    // 2. Thân bồn gỗ (Isometric Planter Box)
                    // Mặt trước bên phải (Right face, slopes +0.5)
                    if (dx >= 0 && dx <= rx)
                    {
                        float ybot = ybase + 0.5f * dx;
                        float ytop = ybot + hBox;
                        if (y >= ybot && y <= ytop)
                        {
                            float faceRelY = y - ybot;
                            bool isPlankLine = ((int)faceRelY % 12 == 0);
                            bool isCornerPost = (dx >= rx - 4);
                            if (isCornerPost) pixels[y * w + x] = woodCornerPost;
                            else if (isPlankLine) pixels[y * w + x] = woodPlankGap;
                            else pixels[y * w + x] = woodFaceRight;
                        }
                    }
                    // Mặt trước bên trái (Left face, slopes -0.5)
                    else if (dx < 0 && dx >= -lx)
                    {
                        float ybot = ybase - 0.5f * dx;
                        float ytop = ybot + hBox;
                        if (y >= ybot && y <= ytop)
                        {
                            float faceRelY = y - ybot;
                            bool isPlankLine = ((int)faceRelY % 12 == 0);
                            bool isCornerPost = (dx <= -lx + 4);
                            if (isCornerPost) pixels[y * w + x] = woodCornerPost;
                            else if (isPlankLine) pixels[y * w + x] = woodPlankGap;
                            else pixels[y * w + x] = woodFaceLeft;
                        }
                    }

                    // Trụ góc chính diện (Front corner post)
                    if (Mathf.Abs(dx) <= 2.5f)
                    {
                        float ybot = ybase;
                        float ytop = ybase + hBox;
                        if (y >= ybot && y <= ytop)
                        {
                            pixels[y * w + x] = woodCornerPost;
                        }
                    }

                    // Mặt đất trồng bên trong miệng chậu (Top soil diamond)
                    float frontRim = (dx >= 0) ? (ybase + hBox + 0.5f * dx) : (ybase + hBox - 0.5f * dx);
                    float backRim = frontRim + 0.5f * (lx + rx) * 0.45f;
                    if (y >= frontRim - 4f && y <= backRim && dx >= -lx && dx <= rx)
                    {
                        pixels[y * w + x] = soilColor;
                    }
                }
            }

            // 3. Tán lá xanh & chùm hoa cẩm tú cầu nở rộ
            Vector2[] bloomCenters = {
                new Vector2(xc - 18, ybase + hBox + 22),
                new Vector2(xc + 22, ybase + hBox + 26),
                new Vector2(xc + 2,  ybase + hBox + 38),
                new Vector2(xc + 46, ybase + hBox + 18),
                new Vector2(xc - 32, ybase + hBox + 14)
            };
            float[] bloomRadius = { 24f, 26f, 23f, 20f, 18f };
            bool[] isBlue = { false, true, false, false, true };

            // Tán lá xanh phủ xung quanh
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    foreach (var bc in bloomCenters)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), bc);
                        if (d <= 34f && d > 16f && y >= ybase + hBox - 2f)
                        {
                            float noise = Mathf.Sin(x * 0.45f) * Mathf.Cos(y * 0.45f);
                            if (noise > 0.1f)
                            {
                                pixels[y * w + x] = (noise > 0.45f) ? leafBright : leafDark;
                            }
                        }
                    }
                }
            }

            // Các chùm hoa cẩm tú cầu hình cầu rực rỡ
            for (int i = 0; i < bloomCenters.Length; i++)
            {
                Vector2 bc = bloomCenters[i];
                float r = bloomRadius[i];
                Color32 baseCol = isBlue[i] ? hydrBlue : hydrWhite;

                for (int y = Mathf.Max(0, (int)(bc.y - r)); y <= Mathf.Min(h - 1, (int)(bc.y + r)); y++)
                {
                    for (int x = Mathf.Max(0, (int)(bc.x - r)); x <= Mathf.Min(w - 1, (int)(bc.x + r)); x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), bc);
                        if (dist <= r)
                        {
                            float petal = Mathf.Sin(x * 0.65f) * Mathf.Cos(y * 0.65f);
                            if (dist < r * 0.6f && petal > 0.35f)
                            {
                                pixels[y * w + x] = hydrCore;
                            }
                            else if (dist > r * 0.75f || petal < -0.3f)
                            {
                                pixels[y * w + x] = isBlue[i] ? new Color32(145, 180, 225, 255) : hydrShade;
                            }
                            else
                            {
                                pixels[y * w + x] = baseCol;
                            }
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{ArtDir}/prop_planter_hydrangea.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            SetupSprite(path, 200f, new Vector2(0.5f, 0.18f));
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
