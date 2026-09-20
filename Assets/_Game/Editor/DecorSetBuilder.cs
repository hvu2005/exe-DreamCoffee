using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh bộ nội thất theo chủ đề ModernEmerald và VintageClassic:
    /// - Bộ ModernEmerald: Bàn tròn cẩm thạch trắng viền vàng + 2 ghế nhung emerald + Tranh cà phê hoàng gia.
    /// - Bộ VintageClassic: Bàn tròn gỗ gụ cổ điển chạm khắc + 2 ghế da cognac + Tranh thực vật Coffea Arabica.
    /// - Tranh tường chuẩn hướng nghiêng Tường Trái (slope -0.5) kèm biến thể Tường Phải (slope +0.5).
    /// - Mỗi bộ bàn chỉ có đúng 2 ghế.
    /// Menu: DreamCafe > Setup > Build Complete Decor Sets
    /// </summary>
    public static class DecorSetBuilder
    {
        private const string ArtDir = "Assets/_Game/Art/Decor";
        private const string PrefabDir = "Assets/_Game/Prefabs/Decor";
        private const string DataDir = "Assets/_Game/Data/Decor";
        private const string RepoPath = "Assets/_Game/Data/Decor/DecorRepository.asset";

        [MenuItem("DreamCafe/Setup/Build Complete Decor Sets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(ArtDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(DataDir);

            Debug.Log("[DecorSetBuilder] 1. Sinh assets đồ họa chất lượng cao...");
            GenerateArtAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DecorSetBuilder] 2. Cấu hình TextureImporters...");
            ConfigureImporters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DecorSetBuilder] 3. Tạo Prefabs (mỗi bàn đúng 2 ghế) và ScriptableObjects...");
            BuildPrefabsAndData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DecorSetBuilder] Hoàn thành cập nhật toàn bộ bộ nội thất mới!");
        }

        #region 1. Art Generation

        public static void GenerateArtAssets()
        {
            GenerateVintageRoundTable();
            GenerateEmeraldWallPainting(isRightWall: false);
            GenerateEmeraldWallPainting(isRightWall: true);
            GenerateVintageWallPainting(isRightWall: false);
            GenerateVintageWallPainting(isRightWall: true);
            GenerateVintageChair();
        }

        /// <summary>
        /// Vẽ bàn tròn gỗ gụ cổ điển (Vintage Mahogany Round Table) ở độ phân giải cao 1024x1024.
        /// Đồng bộ hoàn hảo phong cách hoạt hình 2.5D isometric với ghế gỗ cổ điển và bàn tròn gỗ.
        /// </summary>
        private static void GenerateVintageRoundTable()
        {
            int w = 1024, h = 1024;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(System.Drawing.Color.Transparent);

                    float cx = 512f;

                    // 1. Soft Contact Shadow on Floor
                    using (var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(50, 0, 0, 0)))
                    {
                        g.FillEllipse(shadowBrush, cx - 290, 835, 580, 135);
                    }
                    using (var coreShadow = new SolidBrush(System.Drawing.Color.FromArgb(80, 0, 0, 0)))
                    {
                        g.FillEllipse(coreShadow, cx - 180, 855, 360, 90);
                    }

                    // 2. Cabriole Legs - 2 Chân sau
                    DrawCabrioleLeg(g, cx, 715, -165, 775, -205, 805, 36, 24, isBack: true);
                    DrawCabrioleLeg(g, cx, 715, 165, 775, 205, 805, 36, 24, isBack: true);

                    // 3. Lower Pedestal Base (Bệ tiện gỗ đa tầng)
                    // Tầng chân mở rộng dưới cùng
                    using (var baseBrush = new LinearGradientBrush(new PointF(cx - 150, 700), new PointF(cx + 150, 760),
                        System.Drawing.Color.FromArgb(75, 32, 22), System.Drawing.Color.FromArgb(38, 14, 9)))
                    {
                        g.FillEllipse(baseBrush, cx - 145, 725, 290, 65);
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 6f))
                    {
                        g.DrawEllipse(pen, cx - 145, 725, 290, 65);
                    }

                    // Tầng gờ tiện số 2
                    using (var collarBrush = new LinearGradientBrush(new PointF(cx - 110, 690), new PointF(cx + 110, 735),
                        System.Drawing.Color.FromArgb(92, 40, 28), System.Drawing.Color.FromArgb(46, 18, 12)))
                    {
                        g.FillEllipse(collarBrush, cx - 110, 700, 220, 50);
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 5f))
                    {
                        g.DrawEllipse(pen, cx - 110, 700, 220, 50);
                    }

                    // Cabriole Legs - 2 Chân trước bọc đầu đồng thau móng vuốt
                    DrawCabrioleLeg(g, cx, 735, -195, 840, -250, 895, 48, 30, isBack: false);
                    DrawCabrioleLeg(g, cx, 735, 195, 840, 250, 895, 48, 30, isBack: false);

                    // 4. Central Turned Column (Trụ tiện phong cách Baluster Châu Âu)
                    // Thân loe dưới
                    using (var vaseBrush = new LinearGradientBrush(new PointF(cx - 90, 630), new PointF(cx + 90, 630),
                        System.Drawing.Color.FromArgb(52, 22, 15), System.Drawing.Color.FromArgb(96, 42, 30)))
                    {
                        vaseBrush.SetBlendTriangularShape(0.6f);
                        var pathVase = new GraphicsPath();
                        pathVase.AddBezier(cx - 65, 715, cx - 100, 670, cx - 110, 610, cx - 80, 560);
                        pathVase.AddLine(cx - 80, 560, cx + 80, 560);
                        pathVase.AddBezier(cx + 80, 560, cx + 110, 610, cx + 100, 670, cx + 65, 715);
                        pathVase.CloseFigure();
                        g.FillPath(vaseBrush, pathVase);
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 6f))
                        {
                            g.DrawPath(pen, pathVase);
                        }
                    }

                    // Vòng tiện ngăn cách
                    using (var ringBrush = new LinearGradientBrush(new PointF(cx - 90, 545), new PointF(cx + 90, 545),
                        System.Drawing.Color.FromArgb(108, 48, 34), System.Drawing.Color.FromArgb(50, 20, 14)))
                    {
                        g.FillEllipse(ringBrush, cx - 92, 544, 184, 38);
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 5f))
                            g.DrawEllipse(pen, cx - 92, 544, 184, 38);
                    }

                    // Bầu tiện quả lê (Center bulb)
                    using (var bulbBrush = new LinearGradientBrush(new PointF(cx - 85, 460), new PointF(cx + 85, 460),
                        System.Drawing.Color.FromArgb(52, 22, 15), System.Drawing.Color.FromArgb(105, 46, 32)))
                    {
                        bulbBrush.SetBlendTriangularShape(0.65f);
                        g.FillEllipse(bulbBrush, cx - 85, 460, 170, 100);
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 6f))
                            g.DrawEllipse(pen, cx - 85, 460, 170, 100);
                    }

                    // Cổ đỡ mặt bàn (Sub-table support neck)
                    using (var neckBrush = new LinearGradientBrush(new PointF(cx - 130, 420), new PointF(cx + 130, 420),
                        System.Drawing.Color.FromArgb(50, 20, 14), System.Drawing.Color.FromArgb(90, 38, 26)))
                    {
                        neckBrush.SetBlendTriangularShape(0.6f);
                        PointF[] neckPts = {
                            new PointF(cx - 55, 480),
                            new PointF(cx - 140, 390),
                            new PointF(cx + 140, 390),
                            new PointF(cx + 55, 480)
                        };
                        g.FillPolygon(neckBrush, neckPts);
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 10, 6), 6f))
                            g.DrawPolygon(pen, neckPts);
                    }

                    // 5. Tabletop Dimensions & Under-Apron
                    float tX = 84f, tY = 90f, tW = 856f, tH = 430f;
                    float lipDepth = 42f;

                    // Yếm gỗ sẫm màu bên dưới (Subframe / Apron)
                    using (var apronBrush = new LinearGradientBrush(new PointF(cx, tY + 40), new PointF(cx, tY + tH + 50),
                        System.Drawing.Color.FromArgb(62, 24, 15), System.Drawing.Color.FromArgb(28, 10, 6)))
                    {
                        g.FillEllipse(apronBrush, tX + 35, tY + 45, tW - 70, tH - 10);
                    }

                    // 6. Tabletop Rim Lip (Elip mép bàn bên dưới tạo độ dày 3D hữu cơ 100% mượt mà)
                    using (var rimBrush = new LinearGradientBrush(new PointF(tX, tY + lipDepth), new PointF(tX + tW, tY + lipDepth + tH),
                        System.Drawing.Color.FromArgb(88, 38, 25), System.Drawing.Color.FromArgb(42, 16, 10)))
                    {
                        g.FillEllipse(rimBrush, tX, tY + lipDepth, tW, tH);
                    }
                    // Viền nẹp đồng thau sáng chạy dọc mép bàn
                    using (var brassPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(228, 182, 75), 4.5f))
                    {
                        g.DrawArc(brassPen, tX + 16, tY + lipDepth * 0.7f, tW - 32, tH, 15, 150);
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(26, 8, 4), 7f))
                    {
                        g.DrawEllipse(pen, tX, tY + lipDepth, tW, tH);
                    }

                    // 7. Tabletop Surface (Mặt bàn tròn gỗ gụ phẳng bóng loáng phủ lên trên)
                    using (var lipBrush = new LinearGradientBrush(new PointF(tX, tY), new PointF(tX + tW, tY + tH),
                        System.Drawing.Color.FromArgb(108, 50, 34), System.Drawing.Color.FromArgb(55, 24, 15)))
                    {
                        g.FillEllipse(lipBrush, tX, tY, tW, tH);
                    }

                    // Rãnh chỉ vàng cổ viền chu vi mặt bàn
                    using (var beadPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(218, 172, 65), 4.5f))
                    {
                        g.DrawEllipse(beadPen, tX + 14, tY + 8, tW - 28, tH - 16);
                    }

                    // Lòng mặt bàn gỗ gụ trung tâm
                    float coreInset = 24f;
                    float cX = tX + coreInset, cY = tY + coreInset * 0.7f;
                    float cW = tW - coreInset * 2f, cH = tH - coreInset * 1.4f;

                    using (var topBrush = new LinearGradientBrush(new PointF(cX, cY), new PointF(cX, cY + cH),
                        System.Drawing.Color.FromArgb(105, 46, 32), System.Drawing.Color.FromArgb(58, 24, 16)))
                    {
                        g.FillEllipse(topBrush, cX, cY, cW, cH);
                    }

                    // Các dải vân gỗ đồng tâm tự nhiên
                    Action<float, float, int> drawGrain = (rx, ry, alpha) =>
                    {
                        using (var gp = new System.Drawing.Pen(System.Drawing.Color.FromArgb(alpha, 145, 68, 48), 3f))
                        {
                            g.DrawEllipse(gp, cx - rx, (cY + cH * 0.5f) - ry, rx * 2, ry * 2);
                        }
                    };
                    drawGrain(cW * 0.42f, cH * 0.42f, 40);
                    drawGrain(cW * 0.33f, cH * 0.33f, 48);
                    drawGrain(cW * 0.24f, cH * 0.24f, 55);
                    drawGrain(cW * 0.15f, cH * 0.15f, 60);

                    // Tia vân gỗ hướng tâm nhẹ
                    using (var rayPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(32, 145, 68, 48), 2.5f))
                    {
                        for (int ang = 0; ang < 360; ang += 30)
                        {
                            double rad = ang * Math.PI / 180.0;
                            float px1 = cx + (float)(Math.Cos(rad) * cW * 0.12f);
                            float py1 = (cY + cH * 0.5f) + (float)(Math.Sin(rad) * cH * 0.12f);
                            float px2 = cx + (float)(Math.Cos(rad) * cW * 0.46f);
                            float py2 = (cY + cH * 0.5f) + (float)(Math.Sin(rad) * cH * 0.46f);
                            g.DrawLine(rayPen, px1, py1, px2, py2);
                        }
                    }

                    // Vệt phản quang ánh sáng bóng loáng (High-gloss lacquer sheen)
                    var sheenPath = new GraphicsPath();
                    sheenPath.AddArc(cX + 50, cY + 25, cW - 100, cH - 50, 195, 150);
                    sheenPath.AddBezier(
                        cX + cW - 80, cY + cH * 0.38f,
                        cx + 80, cY + 90,
                        cx - 80, cY + 95,
                        cX + 70, cY + cH * 0.42f
                    );
                    sheenPath.CloseFigure();
                    using (var sheenBrush = new LinearGradientBrush(new PointF(cx, cY + 20), new PointF(cx, cY + 160),
                        System.Drawing.Color.FromArgb(65, 255, 240, 220), System.Drawing.Color.FromArgb(0, 255, 240, 220)))
                    {
                        g.FillPath(sheenBrush, sheenPath);
                    }

                    // Viền nét đậm bao quanh mặt bàn
                    using (var outlinePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(28, 10, 6), 7f))
                    {
                        g.DrawEllipse(outlinePen, tX, tY, tW, tH);
                    }
                }

                bmp.Save($"{ArtDir}/table_vintage_round.png", ImageFormat.Png);
            }
        }

        private static void DrawCabrioleLeg(System.Drawing.Graphics g, float cx, float startY, float midDx, float midY, float tipDx, float tipY, float capW, float capH, bool isBack)
        {
            float startX = cx + (tipDx > 0 ? 30f : -30f);
            float tipX = cx + tipDx;
            float tipYPos = tipY;
            float midX = cx + midDx;

            var path = new GraphicsPath();
            path.AddBezier(startX, startY, midX, startY + (midY - startY) * 0.3f, midX + (tipDx > 0 ? 35 : -35), midY, tipX, tipYPos);
            path.AddLine(tipX, tipYPos, tipX + (tipDx > 0 ? -22 : 22), tipYPos + 12);
            path.AddBezier(tipX + (tipDx > 0 ? -22 : 22), tipYPos + 12, midX + (tipDx > 0 ? 8 : -8), midY + 10, startX, startY + 20, startX, startY);
            path.CloseFigure();

            int alpha = isBack ? 220 : 255;
            System.Drawing.Color c1 = isBack ? System.Drawing.Color.FromArgb(alpha, 45, 18, 12) : System.Drawing.Color.FromArgb(alpha, 85, 36, 25);
            System.Drawing.Color c2 = System.Drawing.Color.FromArgb(alpha, 30, 10, 7);

            using (var legBrush = new LinearGradientBrush(new PointF(startX, startY), new PointF(tipX, tipYPos), c1, c2))
            {
                g.FillPath(legBrush, path);
            }
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(alpha, 25, 8, 5), isBack ? 4.5f : 6f))
            {
                g.DrawPath(pen, path);
            }

            // Bọc đồng thau móng vuốt (Brass Claw Ferrule)
            float capX = tipX + (tipDx > 0 ? -capW * 0.4f : -capW * 0.6f);
            float capY = tipYPos - capH * 0.3f;
            using (var capBrush = new LinearGradientBrush(new PointF(capX, capY), new PointF(capX + capW, capY + capH),
                System.Drawing.Color.FromArgb(235, 195, 75), System.Drawing.Color.FromArgb(135, 95, 20)))
            {
                g.FillEllipse(capBrush, capX, capY, capW, capH);
            }
            using (var capHighlight = new SolidBrush(System.Drawing.Color.FromArgb(255, 245, 160)))
            {
                g.FillEllipse(capHighlight, capX + capW * 0.25f, capY + capH * 0.2f, capW * 0.4f, capH * 0.35f);
            }
            using (var capPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 40, 8), 3f))
            {
                g.DrawEllipse(capPen, capX, capY, capW, capH);
            }
        }

        /// <summary>
        /// Vẽ tranh cà phê hoàng gia ngọc lục bảo (Emerald Wall Painting).
        /// Hướng nghiêng mặc định: Tường Trái (slope -0.5, mép trên dốc lên về bên phải, bề dày ở cạnh phải).
        /// </summary>
        private static void GenerateEmeraldWallPainting(bool isRightWall)
        {
            int w = 260, h = 320;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(System.Drawing.Color.Transparent);

                    float slope = isRightWall ? 0.5f : -0.5f;
                    float fw = 160f, fh = 175f;
                    float x0 = isRightWall ? 40f : 38f;
                    float y0 = isRightWall ? 50f : 130f;

                    PointF pTL = new PointF(x0, y0);
                    PointF pTR = new PointF(x0 + fw, y0 + fw * slope);
                    PointF pBR = new PointF(x0 + fw, y0 + fw * slope + fh);
                    PointF pBL = new PointF(x0, y0 + fh);

                    // 1. Khung mạ vàng hoàng gia đa tầng (Ornate Baroque Gold Frame)
                    using (var goldBrush = new LinearGradientBrush(pTL, pBR,
                        System.Drawing.Color.FromArgb(245, 210, 110),
                        System.Drawing.Color.FromArgb(165, 118, 30)))
                    {
                        g.FillPolygon(goldBrush, new[] { pTL, pTR, pBR, pBL });
                    }

                    // Vát cạnh đổ bóng 3D dọc mép ngoài (Bevel strip)
                    float bevelW = 8f;
                    PointF bTL = isRightWall ? InsetPoint(pTL, bevelW, 0, slope) : pTL;
                    PointF bTR = isRightWall ? pTR : InsetPoint(pTR, -bevelW, 0, slope);
                    PointF bBR = isRightWall ? pBR : InsetPoint(pBR, -bevelW, 0, slope);
                    PointF bBL = isRightWall ? InsetPoint(pBL, bevelW, 0, slope) : pBL;

                    PointF[] bevelStrip = isRightWall
                        ? new[] { pTL, bTL, bBL, pBL }
                        : new[] { bTR, pTR, pBR, bBR };

                    using (var bevelBrush = new LinearGradientBrush(pTL, pBR,
                        System.Drawing.Color.FromArgb(115, 75, 20),
                        System.Drawing.Color.FromArgb(48, 28, 6)))
                    {
                        g.FillPolygon(bevelBrush, bevelStrip);
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 35, 10), 3f))
                    {
                        g.DrawPolygon(pen, new[] { pTL, pTR, pBR, pBL });
                    }

                    // Nẹp gờ chỉ vàng lõm tạo hiệu ứng nổi khối
                    float mGold = 8f;
                    PointF gTL = InsetPoint(pTL, mGold, mGold, slope);
                    PointF gTR = InsetPoint(pTR, -mGold, mGold, slope);
                    PointF gBR = InsetPoint(pBR, -mGold, -mGold, slope);
                    PointF gBL = InsetPoint(pBL, mGold, -mGold, slope);

                    using (var innerGold = new LinearGradientBrush(gTL, gBR,
                        System.Drawing.Color.FromArgb(180, 130, 35),
                        System.Drawing.Color.FromArgb(110, 75, 18)))
                    {
                        g.FillPolygon(innerGold, new[] { gTL, gTR, gBR, gBL });
                    }

                    // 3. Nền Canvas lụa satin ngọc trai
                    float mCanvas = 15f;
                    PointF cTL = InsetPoint(pTL, mCanvas, mCanvas, slope);
                    PointF cTR = InsetPoint(pTR, -mCanvas, mCanvas, slope);
                    PointF cBR = InsetPoint(pBR, -mCanvas, -mCanvas, slope);
                    PointF cBL = InsetPoint(pBL, mCanvas, -mCanvas, slope);

                    using (var canvasBrush = new LinearGradientBrush(cTL, cBR,
                        System.Drawing.Color.FromArgb(253, 251, 245),
                        System.Drawing.Color.FromArgb(242, 235, 222)))
                    {
                        g.FillPolygon(canvasBrush, new[] { cTL, cTR, cBR, cBL });
                    }
                    using (var filletPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(215, 168, 62), 1.5f))
                    {
                        g.DrawPolygon(filletPen, new[] { cTL, cTR, cBR, cBL });
                    }

                    // 4. Tác phẩm mỹ thuật: Tách cà phê ngọc lục bảo sứ hoàng gia & Swan Latte Art
                    float cx = (cTL.X + cBR.X) * 0.5f;
                    float cy = (cTL.Y + cBR.Y) * 0.5f + 12f;

                    // Bóng đổ dưới đĩa lót
                    using (var saucerShadow = new SolidBrush(System.Drawing.Color.FromArgb(40, 0, 0, 0)))
                    {
                        g.FillEllipse(saucerShadow, cx - 48, cy + 24, 96, 22);
                    }

                    // Đĩa lót sứ viền vàng
                    using (var saucerBrush = new LinearGradientBrush(new PointF(cx - 44, cy + 16), new PointF(cx + 44, cy + 40),
                        System.Drawing.Color.FromArgb(248, 245, 238), System.Drawing.Color.FromArgb(18, 90, 60)))
                    {
                        g.FillEllipse(saucerBrush, cx - 44, cy + 16, 88, 26);
                    }
                    using (var goldPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(228, 185, 70), 2.5f))
                    {
                        g.DrawEllipse(goldPen, cx - 44, cy + 16, 88, 26);
                    }

                    // Tách sứ màu xanh ngọc lục bảo hoàng gia
                    float cupW = 56f, cupH = 18f;
                    var cupBody = new GraphicsPath();
                    cupBody.AddBezier(cx - cupW * 0.5f, cy + 2, cx - cupW * 0.45f, cy + 22, cx - cupW * 0.28f, cy + 25, cx, cy + 25);
                    cupBody.AddBezier(cx, cy + 25, cx + cupW * 0.28f, cy + 25, cx + cupW * 0.45f, cy + 22, cx + cupW * 0.5f, cy + 2);
                    cupBody.CloseFigure();

                    using (var cupBrush = new LinearGradientBrush(new PointF(cx - cupW * 0.5f, cy), new PointF(cx + cupW * 0.5f, cy + 25),
                        System.Drawing.Color.FromArgb(14, 82, 54), System.Drawing.Color.FromArgb(6, 42, 26)))
                    {
                        g.FillPath(cupBrush, cupBody);
                    }
                    // Highlight vệt bóng trên thân tách sứ
                    using (var shinePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(110, 60, 195, 140), 3f))
                    {
                        g.DrawBezier(shinePen, cx - 18, cy + 6, cx - 16, cy + 18, cx - 12, cy + 21, cx - 6, cy + 23);
                    }

                    // Quai tách dát vàng
                    using (var handlePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(228, 185, 70), 3.5f))
                    {
                        g.DrawArc(handlePen, cx + cupW * 0.35f, cy + 2, 18, 20, 270, 180);
                    }

                    // Mặt cà phê & Crema
                    using (var cremaBrush = new LinearGradientBrush(new PointF(cx - cupW * 0.5f, cy - cupH * 0.5f), new PointF(cx + cupW * 0.5f, cy + cupH * 0.5f),
                        System.Drawing.Color.FromArgb(120, 65, 30), System.Drawing.Color.FromArgb(60, 28, 12)))
                    {
                        g.FillEllipse(cremaBrush, cx - cupW * 0.5f, cy - cupH * 0.5f, cupW, cupH);
                    }
                    using (var goldPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(235, 195, 75), 2f))
                    {
                        g.DrawEllipse(goldPen, cx - cupW * 0.5f, cy - cupH * 0.5f, cupW, cupH);
                    }

                    // Bọt sữa Latte Art hình thiên nga/hoa sen hoàng gia
                    using (var foamBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 252, 244)))
                    {
                        // Thân hoa/cánh thiên nga
                        g.FillEllipse(foamBrush, cx - 12, cy - 3, 24, 7);
                        g.FillEllipse(foamBrush, cx - 8, cy - 5, 16, 5);
                        g.FillEllipse(foamBrush, cx - 4, cy - 7, 8, 4);
                    }

                    // Làn hơi nước thơm ấm bay lượn
                    using (var steamPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(120, 255, 255, 255), 1.8f))
                    {
                        steamPen.StartCap = LineCap.Round;
                        steamPen.EndCap = LineCap.Round;
                        g.DrawBezier(steamPen, cx - 8, cy - 10, cx - 14, cy - 25, cx - 4, cy - 38, cx - 10, cy - 52);
                        g.DrawBezier(steamPen, cx + 6, cy - 10, cx + 14, cy - 24, cx + 4, cy - 36, cx + 8, cy - 50);
                    }
                }

                string fileName = isRightWall ? "prop_wall_painting_emerald_right.png" : "prop_wall_painting_emerald.png";
                bmp.Save($"{ArtDir}/{fileName}", ImageFormat.Png);
            }
        }

        /// <summary>
        /// Vẽ tranh thực vật cổ điển Coffea Arabica (Vintage Botanical Wall Painting).
        /// Hướng nghiêng mặc định: Tường Trái (slope -0.5, mép trên dốc lên về bên phải, bề dày ở cạnh phải).
        /// </summary>
        private static void GenerateVintageWallPainting(bool isRightWall)
        {
            int w = 260, h = 320;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(System.Drawing.Color.Transparent);

                    float slope = isRightWall ? 0.5f : -0.5f;
                    float fw = 160f, fh = 175f;
                    float x0 = isRightWall ? 40f : 38f;
                    float y0 = isRightWall ? 50f : 130f;

                    PointF pTL = new PointF(x0, y0);
                    PointF pTR = new PointF(x0 + fw, y0 + fw * slope);
                    PointF pBR = new PointF(x0 + fw, y0 + fw * slope + fh);
                    PointF pBL = new PointF(x0, y0 + fh);

                    // 1. Khung gỗ óc chó cổ điển chạm khắc (Antique Walnut Carved Frame)
                    using (var woodBrush = new LinearGradientBrush(pTL, pBR,
                        System.Drawing.Color.FromArgb(88, 42, 22),
                        System.Drawing.Color.FromArgb(46, 20, 10)))
                    {
                        g.FillPolygon(woodBrush, new[] { pTL, pTR, pBR, pBL });
                    }

                    // Vát cạnh đổ bóng 3D dọc mép ngoài (Bevel strip)
                    float bevelW = 8f;
                    PointF bTL = isRightWall ? InsetPoint(pTL, bevelW, 0, slope) : pTL;
                    PointF bTR = isRightWall ? pTR : InsetPoint(pTR, -bevelW, 0, slope);
                    PointF bBR = isRightWall ? pBR : InsetPoint(pBR, -bevelW, 0, slope);
                    PointF bBL = isRightWall ? InsetPoint(pBL, bevelW, 0, slope) : pBL;

                    PointF[] bevelStrip = isRightWall
                        ? new[] { pTL, bTL, bBL, pBL }
                        : new[] { bTR, pTR, pBR, bBR };

                    using (var bevelBrush = new LinearGradientBrush(pTL, pBR,
                        System.Drawing.Color.FromArgb(42, 18, 8),
                        System.Drawing.Color.FromArgb(20, 8, 3)))
                    {
                        g.FillPolygon(bevelBrush, bevelStrip);
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(24, 10, 4), 3f))
                    {
                        g.DrawPolygon(pen, new[] { pTL, pTR, pBR, pBL });
                    }

                    // Nẹp dát vàng lá cổ (Antique Gold Fillet)
                    float mGold = 7f;
                    PointF gTL = InsetPoint(pTL, mGold, mGold, slope);
                    PointF gTR = InsetPoint(pTR, -mGold, mGold, slope);
                    PointF gBR = InsetPoint(pBR, -mGold, -mGold, slope);
                    PointF gBL = InsetPoint(pBL, mGold, -mGold, slope);

                    using (var goldFillet = new LinearGradientBrush(gTL, gBR,
                        System.Drawing.Color.FromArgb(215, 172, 70),
                        System.Drawing.Color.FromArgb(150, 108, 32)))
                    {
                        g.FillPolygon(goldFillet, new[] { gTL, gTR, gBR, gBL });
                    }

                    // 3. Nền giấy da cổ (Antique Botanical Parchment)
                    float mCanvas = 14f;
                    PointF cTL = InsetPoint(pTL, mCanvas, mCanvas, slope);
                    PointF cTR = InsetPoint(pTR, -mCanvas, mCanvas, slope);
                    PointF cBR = InsetPoint(pBR, -mCanvas, -mCanvas, slope);
                    PointF cBL = InsetPoint(pBL, mCanvas, -mCanvas, slope);

                    using (var parchmentBrush = new LinearGradientBrush(cTL, cBR,
                        System.Drawing.Color.FromArgb(248, 240, 224),
                        System.Drawing.Color.FromArgb(236, 224, 200)))
                    {
                        g.FillPolygon(parchmentBrush, new[] { cTL, cTR, cBR, cBL });
                    }
                    using (var sepiaPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(135, 95, 55), 1.2f))
                    {
                        g.DrawPolygon(sepiaPen, new[] { cTL, cTR, cBR, cBL });
                    }

                    // 4. Tranh vẽ minh họa thực vật: Nhánh Coffea Arabica với lá xanh & chùm quả chín mọng
                    float cx = (cTL.X + cBR.X) * 0.5f;
                    float cy = (cTL.Y + cBR.Y) * 0.5f - 4f;

                    // Nhánh cây gỗ uốn cong tự nhiên
                    using (var branchPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(82, 45, 20), 2.8f))
                    {
                        branchPen.StartCap = LineCap.Round;
                        branchPen.EndCap = LineCap.Round;
                        if (!isRightWall)
                        {
                            g.DrawBezier(branchPen, cx - 35, cy + 50, cx - 18, cy + 18, cx + 10, cy - 16, cx + 26, cy - 48);
                            g.DrawBezier(branchPen, cx - 12, cy + 22, cx + 6, cy + 14, cx + 18, cy + 18, cx + 28, cy + 16);
                            g.DrawBezier(branchPen, cx - 18, cy + 34, cx - 32, cy + 24, cx - 40, cy + 26, cx - 46, cy + 22);
                        }
                        else
                        {
                            g.DrawBezier(branchPen, cx + 35, cy + 50, cx + 18, cy + 18, cx - 10, cy - 16, cx - 26, cy - 48);
                            g.DrawBezier(branchPen, cx + 12, cy + 22, cx - 6, cy + 14, cx - 18, cy + 18, cx - 28, cy + 16);
                            g.DrawBezier(branchPen, cx + 18, cy + 34, cx + 32, cy + 24, cx + 40, cy + 26, cx + 46, cy + 22);
                        }
                    }

                    // Vẽ lá cà phê (Pointed glossy botanical leaves)
                    Action<float, float, float, float, float> drawLeaf = (lx, ly, wL, hL, ang) =>
                    {
                        var state = g.Save();
                        g.TranslateTransform(lx, ly);
                        g.RotateTransform(ang);
                        using (var leafBrush = new LinearGradientBrush(new PointF(-wL * 0.5f, -hL * 0.5f), new PointF(wL * 0.5f, hL * 0.5f),
                            System.Drawing.Color.FromArgb(48, 115, 60), System.Drawing.Color.FromArgb(25, 75, 38)))
                        {
                            g.FillEllipse(leafBrush, -wL * 0.5f, -hL * 0.5f, wL, hL);
                        }
                        // Gân lá xương cá sắc nét
                        using (var veinPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(120, 195, 135), 1.2f))
                        {
                            g.DrawLine(veinPen, -wL * 0.45f, 0, wL * 0.45f, 0);
                            g.DrawLine(veinPen, -wL * 0.15f, 0, -wL * 0.05f, -hL * 0.3f);
                            g.DrawLine(veinPen, -wL * 0.15f, 0, -wL * 0.05f, hL * 0.3f);
                            g.DrawLine(veinPen, wL * 0.15f, 0, wL * 0.25f, -hL * 0.3f);
                            g.DrawLine(veinPen, wL * 0.15f, 0, wL * 0.25f, hL * 0.3f);
                        }
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(18, 55, 26), 1.2f))
                        {
                            g.DrawEllipse(pen, -wL * 0.5f, -hL * 0.5f, wL, hL);
                        }
                        g.Restore(state);
                    };

                    float flip = isRightWall ? -1f : 1f;
                    drawLeaf(cx + 25 * flip, cy - 38, 28, 14, -25 * flip);
                    drawLeaf(cx - 8 * flip, cy - 14, 30, 15, 20 * flip);
                    drawLeaf(cx + 28 * flip, cy + 12, 26, 13, -15 * flip);
                    drawLeaf(cx - 36 * flip, cy + 22, 26, 13, 30 * flip);

                    // Vẽ quả cà phê chín mọng căng tròn (Ruby-red ripe coffee cherries)
                    Action<float, float> drawCherry = (bx, by) =>
                    {
                        using (var cherryBrush = new LinearGradientBrush(new PointF(bx - 6, by - 6), new PointF(bx + 6, by + 6),
                            System.Drawing.Color.FromArgb(215, 38, 38), System.Drawing.Color.FromArgb(115, 12, 12)))
                        {
                            g.FillEllipse(cherryBrush, bx - 6, by - 6, 12, 12);
                        }
                        // Đốm sáng phản quang bóng bẩy
                        using (var spec = new SolidBrush(System.Drawing.Color.FromArgb(255, 205, 205)))
                        {
                            g.FillEllipse(spec, bx - 3, by - 4, 3.5f, 3.5f);
                        }
                        // Cuống/rốn quả
                        using (var calyx = new SolidBrush(System.Drawing.Color.FromArgb(50, 8, 8)))
                        {
                            g.FillEllipse(calyx, bx + 2, by + 2, 2.5f, 2.5f);
                        }
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 10, 10), 1f))
                        {
                            g.DrawEllipse(pen, bx - 6, by - 6, 12, 12);
                        }
                    };

                    drawCherry(cx - 12 * flip, cy + 22);
                    drawCherry(cx - 4 * flip, cy + 26);
                    drawCherry(cx - 14 * flip, cy + 32);
                    drawCherry(cx + 10 * flip, cy + 8);
                    drawCherry(cx + 18 * flip, cy + 14);
                    drawCherry(cx + 6 * flip, cy - 16);
                    drawCherry(cx + 15 * flip, cy - 10);

                    // Hoa cà phê trắng e ấp
                    Action<float, float> drawBlossom = (fx, fy) =>
                    {
                        using (var petal = new SolidBrush(System.Drawing.Color.FromArgb(255, 252, 245)))
                        {
                            for (int a = 0; a < 360; a += 72)
                            {
                                double rad = a * Math.PI / 180.0;
                                g.FillEllipse(petal, fx + (float)(Math.Cos(rad) * 4f) - 2f, fy + (float)(Math.Sin(rad) * 4f) - 2f, 4.5f, 4.5f);
                            }
                        }
                        using (var center = new SolidBrush(System.Drawing.Color.FromArgb(235, 185, 60)))
                        {
                            g.FillEllipse(center, fx - 1.5f, fy - 1.5f, 3f, 3f);
                        }
                    };
                    drawBlossom(cx - 2 * flip, cy + 14);

                    // Chữ thư pháp cổ điển "Coffea Arabica"
                    using (var font = new System.Drawing.Font("Georgia", 8f, System.Drawing.FontStyle.Italic))
                    using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(95, 60, 35)))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString("Coffea Arabica", font, textBrush, cx, cy + 54, format);
                    }
                }

                string fileName = isRightWall ? "prop_wall_painting_vintage_right.png" : "prop_wall_painting_vintage.png";
                bmp.Save($"{ArtDir}/{fileName}", ImageFormat.Png);
            }
        }

        private static PointF InsetPoint(PointF pt, float dx, float dy, float slope)
        {
            return new PointF(pt.X + dx, pt.Y + dx * slope + dy);
        }

        /// <summary>
        /// Vẽ ghế da cổ điển Châu Âu (Vintage Classic Chair).
        /// </summary>
        private static void GenerateVintageChair()
        {
            int w = 320, h = 420;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(System.Drawing.Color.Transparent);

                    // 1. Chân sau (trong phối cảnh sau mặt ghế)
                    using (var legBrush = new LinearGradientBrush(
                        new PointF(70, 220), new PointF(100, 340),
                        System.Drawing.Color.FromArgb(58, 28, 14),
                        System.Drawing.Color.FromArgb(28, 12, 6)))
                    {
                        g.FillPolygon(legBrush, new[] {
                            new PointF(92, 235), new PointF(102, 235),
                            new PointF(94, 335), new PointF(86, 335)
                        });
                        g.FillPolygon(legBrush, new[] {
                            new PointF(208, 225), new PointF(218, 225),
                            new PointF(225, 320), new PointF(217, 320)
                        });
                    }

                    // 2. Trụ đỡ lưng ghế (Uprights)
                    using (var postBrush = new LinearGradientBrush(
                        new PointF(110, 110), new PointF(220, 240),
                        System.Drawing.Color.FromArgb(75, 38, 18),
                        System.Drawing.Color.FromArgb(38, 18, 8)))
                    {
                        g.FillPolygon(postBrush, new[] {
                            new PointF(124, 110), new PointF(134, 110),
                            new PointF(124, 235), new PointF(114, 235)
                        });
                        g.FillPolygon(postBrush, new[] {
                            new PointF(190, 110), new PointF(200, 110),
                            new PointF(202, 230), new PointF(192, 230)
                        });
                    }
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(22, 10, 4), 2f))
                    {
                        g.DrawPolygon(pen, new[] { new PointF(124, 110), new PointF(134, 110), new PointF(124, 235), new PointF(114, 235) });
                        g.DrawPolygon(pen, new[] { new PointF(190, 110), new PointF(200, 110), new PointF(202, 230), new PointF(192, 230) });
                    }

                    // 3. Tựa lưng oval bọc da chần nút
                    float brX = 98, brY = 50, brW = 126, brH = 102;
                    using (var frameBrush = new LinearGradientBrush(
                        new PointF(brX, brY), new PointF(brX + brW, brY + brH),
                        System.Drawing.Color.FromArgb(88, 44, 22),
                        System.Drawing.Color.FromArgb(42, 20, 9)))
                    {
                        g.FillEllipse(frameBrush, brX, brY, brW, brH);
                    }
                    using (var framePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(22, 10, 4), 3.5f))
                    {
                        g.DrawEllipse(framePen, brX, brY, brW, brH);
                    }

                    // Đệm da màu cognac chần nút
                    float padM = 12;
                    using (var leatherBrush = new LinearGradientBrush(
                        new PointF(brX + padM, brY + padM),
                        new PointF(brX + brW - padM, brY + brH - padM),
                        System.Drawing.Color.FromArgb(172, 88, 36),
                        System.Drawing.Color.FromArgb(108, 50, 20)))
                    {
                        g.FillEllipse(leatherBrush, brX + padM, brY + padM, brW - padM * 2, brH - padM * 2);
                    }
                    using (var creasePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(62, 26, 10), 1.8f))
                    {
                        g.DrawLine(creasePen, brX + 42, brY + 36, brX + 42, brY + 68);
                        g.DrawLine(creasePen, brX + 63, brY + 30, brX + 63, brY + 74);
                        g.DrawLine(creasePen, brX + 84, brY + 36, brX + 84, brY + 68);
                    }
                    using (var brassBrush = new SolidBrush(System.Drawing.Color.FromArgb(228, 185, 80)))
                    {
                        g.FillEllipse(brassBrush, brX + 40, brY + 50, 5, 5);
                        g.FillEllipse(brassBrush, brX + 61, brY + 50, 5, 5);
                        g.FillEllipse(brassBrush, brX + 82, brY + 50, 5, 5);
                    }

                    // 4. Chân trước có đầu bọc đồng thau
                    using (var legBrush = new LinearGradientBrush(
                        new PointF(70, 260), new PointF(180, 400),
                        System.Drawing.Color.FromArgb(82, 42, 20),
                        System.Drawing.Color.FromArgb(42, 20, 9)))
                    {
                        g.FillPolygon(legBrush, new[] {
                            new PointF(86, 265), new PointF(98, 265),
                            new PointF(90, 395), new PointF(82, 395)
                        });
                        g.FillPolygon(legBrush, new[] {
                            new PointF(176, 275), new PointF(188, 275),
                            new PointF(182, 405), new PointF(174, 405)
                        });
                    }

                    // Bọc đồng thau 4 chân
                    using (var brassBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 175, 72)))
                    {
                        g.FillRectangle(brassBrush, 82, 385, 8, 10);
                        g.FillRectangle(brassBrush, 174, 395, 8, 10);
                        g.FillRectangle(brassBrush, 86, 328, 8, 8);
                        g.FillRectangle(brassBrush, 217, 314, 8, 8);
                    }

                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(22, 10, 4), 2.5f))
                    {
                        g.DrawPolygon(pen, new[] { new PointF(86, 265), new PointF(98, 265), new PointF(90, 395), new PointF(82, 395) });
                        g.DrawPolygon(pen, new[] { new PointF(176, 275), new PointF(188, 275), new PointF(182, 405), new PointF(174, 405) });
                    }

                    // 5. Mặt ghế đệm da tròn
                    float sX = 70, sY = 210, sW = 155, sH = 84;
                    float sDepth = 22;

                    using (var skirtBrush = new LinearGradientBrush(
                        new PointF(sX, sY), new PointF(sX + sW, sY + sH),
                        System.Drawing.Color.FromArgb(62, 30, 15),
                        System.Drawing.Color.FromArgb(32, 15, 7)))
                    {
                        var path = new GraphicsPath();
                        path.AddArc(sX, sY, sW, sH, 0, 180);
                        path.AddArc(sX, sY + sDepth, sW, sH, 180, -180);
                        path.CloseFigure();
                        g.FillPath(skirtBrush, path);
                    }

                    using (var cushionBrush = new LinearGradientBrush(
                        new PointF(sX, sY), new PointF(sX + sW, sY + sH),
                        System.Drawing.Color.FromArgb(186, 96, 40),
                        System.Drawing.Color.FromArgb(112, 52, 20)))
                    {
                        g.FillEllipse(cushionBrush, sX, sY, sW, sH);
                    }

                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(sX + 15, sY + 8, sW - 30, sH - 16);
                        using (var pgb = new PathGradientBrush(path))
                        {
                            pgb.CenterColor = System.Drawing.Color.FromArgb(90, 240, 150, 80);
                            pgb.SurroundColors = new[] { System.Drawing.Color.FromArgb(0, 140, 60, 25) };
                            g.FillPath(pgb, path);
                        }
                    }

                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(22, 10, 4), 3f))
                    {
                        g.DrawArc(pen, sX, sY + sDepth, sW, sH, 0, 180);
                        g.DrawEllipse(pen, sX, sY, sW, sH);
                    }
                }

                bmp.Save($"{ArtDir}/chair_vintage_front.png", ImageFormat.Png);
            }
        }

        #endregion

        #region 2. Texture Importer Configuration

        public static void ConfigureImporters()
        {
            string[] tableAndChairs = {
                $"{ArtDir}/table_vintage_round.png",
                $"{ArtDir}/chair_vintage_front.png",
                $"{ArtDir}/table_emerald_round.png",
                $"{ArtDir}/chair_emerald_front.png"
            };

            foreach (var path in tableAndChairs)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 400;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }

            string[] paintings = {
                $"{ArtDir}/prop_wall_painting_emerald.png",
                $"{ArtDir}/prop_wall_painting_emerald_right.png",
                $"{ArtDir}/prop_wall_painting_vintage.png",
                $"{ArtDir}/prop_wall_painting_vintage_right.png"
            };

            foreach (var path in paintings)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 250;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        #endregion

        #region 3. Prefabs & Data

        public static void BuildPrefabsAndData()
        {
            // Sprites
            var sTableEmerald = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/table_emerald_round.png");
            var sChairEmerald = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chair_emerald_front.png");
            var sPaintingEmerald = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_wall_painting_emerald.png");
            var sPaintingEmeraldRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_wall_painting_emerald_right.png");

            var sTableVintage = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/table_vintage_round.png");
            var sChairVintage = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/chair_vintage_front.png");
            var sPaintingVintage = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_wall_painting_vintage.png");
            var sPaintingVintageRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/prop_wall_painting_vintage_right.png");

            // Build Prefabs (Mỗi bàn chỉ có đúng 2 ghế)
            var pTableEmerald = CreateTable2SeatsPrefab("Prefab_Table_EmeraldRound", sTableEmerald, sChairEmerald);
            var pPaintingEmerald = CreateWallPaintingPrefab("Prefab_Wall_Painting_Emerald", sPaintingEmerald);

            var pTableVintage = CreateTable2SeatsPrefab("Prefab_Table_VintageRound", sTableVintage, sChairVintage);
            var pPaintingVintage = CreateWallPaintingPrefab("Prefab_Wall_Painting_Vintage", sPaintingVintage);

            // ScriptableObjects
            var itemTableEmerald = CreateOrGetDecor("item_table_emerald_round", "Bàn Tròn Ngọc Lục Bảo 2 Ghế",
                "Bàn mặt đá cẩm thạch trắng viền vàng kim loại cao cấp kèm 2 ghế nhung xanh ngọc sang trọng.",
                DecorCategory.SeatingSet, DecorTheme.ModernEmerald, 2, 45000, 50, 180, false,
                0.08f, 0.05f, 2, 0f, sTableEmerald, pTableEmerald);

            var itemPaintingEmerald = CreateOrGetDecor("item_wall_painting_emerald", "Tranh Cà Phê Emerald Treo Tường",
                "Tranh khung vàng kim loại kết hợp tách cà phê ngọc bích phong cách hoàng gia tân cổ điển.",
                DecorCategory.WallDecor, DecorTheme.ModernEmerald, 2, 28000, 30, 120, false,
                0.05f, 0.04f, 0, 4f, sPaintingEmerald, pPaintingEmerald, sPaintingEmeraldRight);

            var itemTableVintage = CreateOrGetDecor("item_table_vintage_round", "Bàn Tròn Gỗ Gụ Cổ Điển 2 Ghế",
                "Bàn gỗ gụ chạm khắc tinh tế kèm 2 ghế da bò khâu nút thủ công phong cách Châu Âu thế kỷ 19.",
                DecorCategory.SeatingSet, DecorTheme.VintageClassic, 2, 38000, 40, 150, false,
                0.07f, 0.04f, 2, 0f, sTableVintage, pTableVintage);

            var itemPaintingVintage = CreateOrGetDecor("item_wall_painting_vintage", "Tranh Thực Vật Cà Phê Cổ Điển",
                "Bức tranh khắc họa nhánh cà phê Coffea Arabica trên giấy da cổ khung gỗ chạm trổ.",
                DecorCategory.WallDecor, DecorTheme.VintageClassic, 2, 24000, 25, 100, false,
                0.05f, 0.03f, 0, 3.5f, sPaintingVintage, pPaintingVintage, sPaintingVintageRight);

            // Đăng ký vào DecorRepository
            RegisterItemsInRepository(new[] {
                itemTableEmerald, itemPaintingEmerald,
                itemTableVintage, itemPaintingVintage
            });
        }

        private static GameObject CreateTable2SeatsPrefab(string name, Sprite tableSprite, Sprite chairSprite)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var root = new GameObject(name);

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = new Vector2(0f, 0.29f);

            var occupant = root.AddComponent<GridOccupant>();

            var art = new GameObject("Art");
            art.transform.SetParent(root.transform, false);
            art.transform.localPosition = new Vector3(-0.01f, 0.03f, 0f);

            // Bàn chính ở giữa (Order 15)
            var tableGo = new GameObject("Table");
            tableGo.transform.SetParent(art.transform, false);
            tableGo.transform.localPosition = new Vector3(0.008f, 0.04f, 0f);
            tableGo.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var srTable = tableGo.AddComponent<SpriteRenderer>();
            srTable.sprite = tableSprite;
            srTable.sortingOrder = 15;

            // Ghế 1 (bên trái, order 14, quay vào bàn)
            var chair1Go = new GameObject("Chair_1");
            chair1Go.transform.SetParent(art.transform, false);
            chair1Go.transform.localPosition = new Vector3(-0.387f, 0.312f, 0f);
            chair1Go.transform.localScale = new Vector3(0.252f, 0.252f, 0.833f);
            chair1Go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var srChair1 = chair1Go.AddComponent<SpriteRenderer>();
            srChair1.sprite = chairSprite;
            srChair1.sortingOrder = 14;

            // Ghế 2 (bên phải, order 14, quay vào bàn)
            var chair2Go = new GameObject("Chair_2");
            chair2Go.transform.SetParent(art.transform, false);
            chair2Go.transform.localPosition = new Vector3(0.418f, 0.306f, 0f);
            chair2Go.transform.localScale = new Vector3(0.252f, 0.252f, 1f);
            var srChair2 = chair2Go.AddComponent<SpriteRenderer>();
            srChair2.sprite = chairSprite;
            srChair2.sortingOrder = 14;

            // Cấu hình GridOccupant đúng 2 ghế
            var soOccupant = new SerializedObject(occupant);
            soOccupant.FindProperty("_artRoot").objectReferenceValue = art.transform;
            soOccupant.FindProperty("_artOffset").vector2Value = new Vector2(-0.0107f, 0.0325f);

            var propBlocked = soOccupant.FindProperty("_blockedCells");
            propBlocked.arraySize = 1;
            propBlocked.GetArrayElementAtIndex(0).vector2IntValue = Vector2Int.zero;

            var propSeats = soOccupant.FindProperty("_seats");
            propSeats.arraySize = 2;

            var seat0 = propSeats.GetArrayElementAtIndex(0);
            seat0.FindPropertyRelative("cell").vector2IntValue = new Vector2Int(0, 1);
            seat0.FindPropertyRelative("sitOffset").vector2Value = new Vector2(-0.47f, 0.37f);
            seat0.FindPropertyRelative("chairArt").objectReferenceValue = srChair1;
            seat0.FindPropertyRelative("label").stringValue = "Chair_1";

            var seat1 = propSeats.GetArrayElementAtIndex(1);
            seat1.FindPropertyRelative("cell").vector2IntValue = new Vector2Int(1, 0);
            seat1.FindPropertyRelative("sitOffset").vector2Value = new Vector2(0.48f, 0.37f);
            seat1.FindPropertyRelative("chairArt").objectReferenceValue = srChair2;
            seat1.FindPropertyRelative("label").stringValue = "Chair_2";

            soOccupant.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateWallPaintingPrefab(string name, Sprite sprite)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var root = new GameObject(name);

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.0f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static DecorItem CreateOrGetDecor(string id, string name, string desc,
            DecorCategory cat, DecorTheme theme, int tier, int price, int reqRep, int repBonus, bool unlocked,
            float patience, float tip, int seats, float mps, Sprite icon, GameObject prefab, Sprite rightWallSprite = null)
        {
            string path = $"{DataDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<DecorItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<DecorItem>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_category").enumValueIndex = (int)cat;
            so.FindProperty("_theme").enumValueIndex = (int)theme;
            so.FindProperty("_tier").intValue = tier;
            so.FindProperty("_price").intValue = price;
            so.FindProperty("_requiredReputation").intValue = reqRep;
            so.FindProperty("_reputationBonus").intValue = repBonus;
            so.FindProperty("_isDefaultUnlocked").boolValue = unlocked;
            so.FindProperty("_patienceBonusPercent").floatValue = patience;
            so.FindProperty("_tipChanceBonus").floatValue = tip;
            so.FindProperty("_seatingCapacity").intValue = seats;
            so.FindProperty("_moneyPerSecondBonus").floatValue = mps;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.FindProperty("_rightWallSprite").objectReferenceValue = rightWallSprite;
            so.ApplyModifiedProperties();
            return item;
        }

        private static void RegisterItemsInRepository(DecorItem[] newItems)
        {
            var repo = AssetDatabase.LoadAssetAtPath<ScriptableDecorRepository>(RepoPath);
            if (repo == null) return;

            var soRepo = new SerializedObject(repo);
            var propItems = soRepo.FindProperty("_items");

            var currentGuids = new HashSet<string>();
            for (int i = 0; i < propItems.arraySize; i++)
            {
                var el = propItems.GetArrayElementAtIndex(i).objectReferenceValue;
                if (el != null)
                {
                    currentGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(el)));
                }
            }

            foreach (var item in newItems)
            {
                if (item == null) continue;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                if (!currentGuids.Contains(guid))
                {
                    int nextIdx = propItems.arraySize;
                    propItems.InsertArrayElementAtIndex(nextIdx);
                    propItems.GetArrayElementAtIndex(nextIdx).objectReferenceValue = item;
                    currentGuids.Add(guid);
                }
            }

            soRepo.ApplyModifiedProperties();
        }

        #endregion
    }
}
