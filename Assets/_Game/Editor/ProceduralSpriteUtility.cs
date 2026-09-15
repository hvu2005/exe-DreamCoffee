using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Bộ đồ nghề vẽ sprite bằng code: ghi texture ra PNG trong project rồi import thành Sprite,
    /// kèm mấy hàm dựng hình cơ bản (hộp bo góc, bầu dục, chồng màu).
    ///
    /// Dùng khi cần hình hình học sạch sẽ mà không muốn phụ thuộc art vẽ tay hay dịch vụ AI:
    /// cốc, vòng chờ, cụm khói... Kết quả nằm nguyên trong project nên vào game là chạy, và commit
    /// được để cả nhóm thấy đúng một thứ.
    /// </summary>
    public static class ProceduralSpriteUtility
    {
        /// <summary>
        /// smoothstep kiểu GLSL: trả ra hệ số 0..1 theo vị trí của <paramref name="x"/> giữa hai mép.
        ///
        /// CỐ TÌNH không dùng <c>Mathf.SmoothStep</c>: hàm của Unity nội suy GIỮA from và to (kết quả
        /// nằm trong khoảng [from, to]) chứ không trả ra hệ số 0..1, nên đem làm mặt nạ thì mọi điểm
        /// ảnh ra gần như cùng một giá trị và hình vẽ ra phẳng lì.
        /// </summary>
        public static float SmoothStep01(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0) return x < edge0 ? 0f : 1f;
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Hình chữ nhật bo góc, mép mềm — trả về độ phủ 0..1.</summary>
        public static float RoundedBoxMask(float u, float v, float left, float right, float bottom, float top,
            float radius, float softness)
        {
            float halfWidth = (right - left) * 0.5f;
            float halfHeight = (top - bottom) * 0.5f;
            if (halfWidth <= 0f || halfHeight <= 0f) return 0f;

            radius = Mathf.Min(radius, Mathf.Min(halfWidth, halfHeight));

            // Khoảng cách có dấu tới hộp bo góc (âm = nằm trong).
            float du = Mathf.Abs(u - (left + halfWidth)) - (halfWidth - radius);
            float dv = Mathf.Abs(v - (bottom + halfHeight)) - (halfHeight - radius);
            float outside = Mathf.Sqrt(Mathf.Max(du, 0f) * Mathf.Max(du, 0f) + Mathf.Max(dv, 0f) * Mathf.Max(dv, 0f));
            float distance = outside + Mathf.Min(Mathf.Max(du, dv), 0f) - radius;

            return 1f - SmoothStep01(-softness, softness, distance);
        }

        /// <summary>Bầu dục đặc, mép mềm — trả về độ phủ 0..1.</summary>
        public static float EllipseMask(float u, float v, float centerU, float centerV,
            float radiusU, float radiusV, float softness)
        {
            if (radiusU <= 0f || radiusV <= 0f) return 0f;

            float du = (u - centerU) / radiusU;
            float dv = (v - centerV) / radiusV;
            float distance = Mathf.Sqrt(du * du + dv * dv);

            // Quy độ mềm theo bán kính nhỏ hơn, không thì bầu dục dẹt sẽ mờ nhoè hết cả.
            float scaled = softness / Mathf.Min(radiusU, radiusV);
            return 1f - SmoothStep01(1f - scaled, 1f + scaled, distance);
        }

        /// <summary>Chồng <paramref name="over"/> lên <paramref name="under"/> theo kiểu source-over.</summary>
        public static Color Over(Color under, Color over, float coverage)
        {
            float alpha = over.a * Mathf.Clamp01(coverage);
            if (alpha <= 0f) return under;

            float outAlpha = alpha + under.a * (1f - alpha);
            if (outAlpha <= 0f) return Color.clear;

            float weightUnder = under.a * (1f - alpha);
            return new Color(
                (over.r * alpha + under.r * weightUnder) / outAlpha,
                (over.g * alpha + under.g * weightUnder) / outAlpha,
                (over.b * alpha + under.b * weightUnder) / outAlpha,
                outAlpha);
        }

        /// <summary>
        /// Vẽ một texture rồi lưu thành PNG tại <paramref name="path"/> và import về Sprite.
        /// </summary>
        /// <param name="colorAt">Màu tại một điểm, toạ độ chuẩn hoá 0..1 với (0,0) ở góc DƯỚI-TRÁI.</param>
        /// <param name="overwrite">
        /// false thì có file sẵn là dùng lại luôn — chạy lại menu không làm bẩn git, và không đè mất
        /// ảnh mà art đã vẽ tay chồng lên. true là ép vẽ lại.
        /// </param>
        public static Sprite EnsureSprite(string path, int width, int height, Func<float, float, Color> colorAt,
            bool overwrite = false)
        {
            if (overwrite || !File.Exists(path))
            {
                EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Lấy tâm điểm ảnh (+0.5) chứ không lấy góc, nếu không hình bị lệch nửa pixel.
                        var color = colorAt((x + 0.5f) / width, (y + 0.5f) / height);

                        // Điểm trong suốt hẳn vẫn phải mang màu sáng: lọc bilinear kéo màu của chúng
                        // vào mép hình, để đen thì viền bị quầng tối.
                        if (color.a <= 0f) color = new Color(1f, 1f, 1f, 0f);

                        pixels[y * width + x] = color;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            ApplySpriteImportSettings(path);
            return LoadSprite(path);
        }

        /// <summary>Tạo cả chuỗi thư mục con dưới Assets/ nếu chưa có.</summary>
        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// Import mặc định của project có thể là Sprite Multiple hoặc Default — ép về Sprite Single,
        /// không thì <see cref="LoadSprite"/> trả về null.
        /// </summary>
        public static void ApplySpriteImportSettings(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Art trong dự án import ở chế độ Sprite Multiple nên <c>LoadAssetAtPath&lt;Sprite&gt;</c>
        /// trả về null — phải lấy sprite con qua representations.
        /// </summary>
        public static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (asset is Sprite child) return child;
            }

            Debug.LogWarning($"[ProceduralSprite] Không tìm thấy sprite tại '{path}'.");
            return null;
        }
    }
}
