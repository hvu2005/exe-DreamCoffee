using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh texture và sprite tường bao 4 dải liền mạch (seamless perimeter walls)
    /// chuẩn 100% tỷ lệ Isometric 2.5D, khớp khít chân tường với Grid sàn của SampleScene,
    /// sử dụng bảng màu và phong cách chuẩn từ wall-1.png và ảnh mẫu reference.
    /// Menu: DreamCafe > Setup > Generate Seamless Walls
    /// </summary>
    public static class DecorWallGenerator
    {
        public const string WallTexturePath = "Assets/_Game/Art/Decor/wall_seamless_perimeter.png";

        [MenuItem("DreamCafe/Setup/Generate Seamless Walls")]
        public static void Generate()
        {
            float ppu = 200f;
            float minX = -2.35f, maxX = 5.25f;
            float minY = -1.45f, maxY = 2.45f;

            int w = Mathf.RoundToInt((maxX - minX) * ppu);
            int h = Mathf.RoundToInt((maxY - minY) * ppu);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            Vector2 W2P(Vector2 world)
            {
                float px = (world.x - minX) * ppu;
                float py = (world.y - minY) * ppu;
                return new Vector2(px, py);
            }

            bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
                float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
                if ((s < 0) != (t < 0) && s != 0 && t != 0) return false;
                float d = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
                return d < 0 ? (s <= 0 && s + t >= d) : (s >= 0 && s + t <= d);
            }

            void FillTri(Vector2 a, Vector2 b, Vector2 c, Color32 col)
            {
                int pxMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, w - 1);
                int pxMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, w - 1);
                int pyMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, h - 1);
                int pyMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, h - 1);

                for (int y = pyMin; y <= pyMax; y++)
                {
                    for (int x = pxMin; x <= pxMax; x++)
                    {
                        if (PointInTri(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                        {
                            pixels[y * w + x] = col;
                        }
                    }
                }
            }

            void FillQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color32 col)
            {
                FillTri(p0, p1, p2, col);
                FillTri(p0, p2, p3, col);
            }

            // --- Tọa độ chân tường (Base) khớp chính xác các đỉnh tilemap của Ground ---
            // Wall 1: Nook Back Wall (x: -3..0, y: 0)
            Vector2 b0 = new Vector2(-2.00f, -0.75f);
            Vector2 b1 = new Vector2(0.00f, 0.25f);
            // Wall 2: Step Wall (x: 0, y: 0..-2)
            Vector2 b2 = new Vector2(1.00f, -0.25f);
            // Wall 3: Counter Back Wall (x: 0..2, y: -2)
            Vector2 b3 = new Vector2(2.00f, 0.25f);
            // Wall 4: Right Wall (x: 2, y: -2..-7)
            Vector2 b4 = new Vector2(5.00f, -1.25f);

            // Chiều cao tường chuẩn y hệt ảnh mẫu reference
            float H = 1.62f;
            Vector2 vH = new Vector2(0f, H);

            Vector2 t0 = b0 + vH;
            Vector2 t1 = b1 + vH;
            Vector2 t2 = b2 + vH;
            Vector2 t3 = b3 + vH;
            Vector2 t4 = b4 + vH;

            // Bảng màu trích xuất 100% chuẩn từ wall-1.png & ảnh mẫu
            Color32 cFace = new Color32(163, 190, 149, 255); // #A3BE95
            Color32 cRim  = new Color32(104, 130, 93, 255);  // #68825D
            Color32 cCap  = new Color32(75, 99, 75, 255);    // #4B634B
            Color32 cLine = new Color32(55, 75, 52, 255);    // #374B34

            // Độ dày gờ đỉnh tường (Top Rim Bevel)
            float thick = 0.085f;
            Vector2 bevelNW = new Vector2(-thick, thick * 0.5f);
            Vector2 bevelNE = new Vector2(thick, thick * 0.5f);

            Vector2 tr0 = t0 + bevelNW;
            Vector2 tr1 = t1 + new Vector2(0f, thick * 0.75f);
            Vector2 tr2 = t2 + new Vector2(0f, thick * 0.75f);
            Vector2 tr3 = t3 + new Vector2(0f, thick * 0.75f);
            Vector2 tr4 = t4 + bevelNE;

            // 1. Vẽ mặt chính diện của 4 bức tường (Face)
            FillQuad(W2P(b0), W2P(b1), W2P(t1), W2P(t0), cFace);
            FillQuad(W2P(b1), W2P(b2), W2P(t2), W2P(t1), cFace);
            FillQuad(W2P(b2), W2P(b3), W2P(t3), W2P(t2), cFace);
            FillQuad(W2P(b3), W2P(b4), W2P(t4), W2P(t3), cFace);

            // 2. Vẽ gờ đỉnh tường (Top Rim)
            FillQuad(W2P(t0), W2P(t1), W2P(tr1), W2P(tr0), cRim);
            FillQuad(W2P(t1), W2P(t2), W2P(tr2), W2P(tr1), cRim);
            FillQuad(W2P(t2), W2P(t3), W2P(tr3), W2P(tr2), cRim);
            FillQuad(W2P(t3), W2P(t4), W2P(tr4), W2P(tr3), cRim);

            // 3. Vẽ cạnh cắt bên trái (Left End Cap)
            Vector2 capL_bot = b0 + bevelNW;
            FillQuad(W2P(b0), W2P(t0), W2P(tr0), W2P(capL_bot), cCap);

            // 4. Vẽ cạnh cắt bên phải (Right End Cap)
            Vector2 capR_bot = b4 + bevelNE;
            FillQuad(W2P(b4), W2P(t4), W2P(tr4), W2P(capR_bot), cCap);

            // 5. Viền nét mảnh sẫm màu chạy dọc gờ đỉnh để tăng độ tương phản 3D
            void DrawLine(Vector2 pA, Vector2 pB, Color32 col)
            {
                int steps = Mathf.CeilToInt(Vector2.Distance(pA, pB) * 1.5f);
                for (int s = 0; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    Vector2 p = Vector2.Lerp(pA, pB, t);
                    int px = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, w - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(p.y), 0, h - 1);
                    pixels[py * w + px] = col;
                }
            }

            DrawLine(W2P(tr0), W2P(tr1), cLine);
            DrawLine(W2P(tr1), W2P(tr2), cLine);
            DrawLine(W2P(tr2), W2P(tr3), cLine);
            DrawLine(W2P(tr3), W2P(tr4), cLine);
            DrawLine(W2P(t0), W2P(t1), cLine);
            DrawLine(W2P(t1), W2P(t2), cLine);
            DrawLine(W2P(t2), W2P(t3), cLine);
            DrawLine(W2P(t3), W2P(t4), cLine);
            DrawLine(W2P(b0), W2P(capL_bot), cLine);
            DrawLine(W2P(capL_bot), W2P(tr0), cLine);
            DrawLine(W2P(b4), W2P(capR_bot), cLine);
            DrawLine(W2P(capR_bot), W2P(tr4), cLine);

            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(WallTexturePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(WallTexturePath);

            var imp = AssetImporter.GetAtPath(WallTexturePath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = ppu;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Bilinear;

                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2((0f - minX) / (maxX - minX), (0f - minY) / (maxY - minY));
                imp.SetTextureSettings(settings);
                imp.SaveAndReimport();
            }

            Object.DestroyImmediate(tex);
            Debug.Log($"[DecorWallGenerator] Đã tạo thành công tường bao liền mạch tại '{WallTexturePath}'!");
        }
    }
}
