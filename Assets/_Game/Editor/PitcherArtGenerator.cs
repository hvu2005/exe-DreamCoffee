using DreamCafe.SystemControl.Brew;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Vẽ bộ art cho CA PHA CHẾ của panel pha chế: ca đong thuỷ tinh có mỏ rót và quai, cộng với
    /// từng "tầng nước" đã cắt sẵn theo đúng lòng ca.
    ///
    /// Vì sao tầng nước phải là ảnh riêng chứ không phải ô chữ nhật: ca LOE ra phía trên, nên một
    /// hình chữ nhật đổ vào sẽ thò ra ngoài thành ca ở miệng và hụt vào ở đáy. Mỗi tầng ở đây được
    /// vẽ đúng bằng mặt cắt của lòng ca tại khoảng cao của nó, mép trên cong theo mặt nước.
    ///
    /// Ảnh tầng vẽ ra màu TRẮNG để lúc chạy chỉ việc nhuộm bằng <c>Image.color</c> theo mã hex của
    /// nguyên liệu — một bộ ảnh dùng cho mọi món.
    ///
    /// CHẠY RIÊNG, không đụng gì tới scene: menu này chỉ sinh file ảnh.
    /// </summary>
    public static class PitcherArtGenerator
    {
        public const string OutputDir = "Assets/_Game/Art/Brew/Generated";

        public const string BackPath = OutputDir + "/pitcher-back.png";
        public const string FrontPath = OutputDir + "/pitcher-front.png";
        public static string LiquidPath(int index) => $"{OutputDir}/pitcher-liquid-{index}.png";

        // Ca cao hơn rộng; mỏ rót bên trái và quai bên phải ăn thêm bề ngang của khung ảnh.
        private const int TextureWidth = 600;
        private const int TextureHeight = 520;

        // ---------------------------------------------------------------
        // Hình học ca — toạ độ chuẩn hoá 0..1 trong khung ảnh, v = 0 ở ĐÁY.
        // BrewingUISetup neo ô chứa nước theo đúng mấy số này nên hai bên không lệch nhau.
        // ---------------------------------------------------------------

        /// <summary>Độ cao tâm vành miệng ca.</summary>
        public const float RimY = 0.880f;
        public const float RimHalfHeight = 0.040f;

        /// <summary>Đáy thân ca.</summary>
        public const float BodyBottom = 0.060f;

        // Thành ngoài: loe nhẹ từ đáy lên miệng (ca đong thẳng hơn tách nhiều).
        private const float OuterBottomLeft = 0.255f;
        private const float OuterBottomRight = 0.595f;
        private const float OuterTopLeft = 0.185f;
        private const float OuterTopRight = 0.665f;

        // Lòng ca (trừ đi bề dày thuỷ tinh).
        public const float CavityBottom = 0.098f;
        public const float CavityBottomLeft = 0.288f;
        public const float CavityBottomRight = 0.562f;
        public const float CavityTopLeft = 0.216f;
        public const float CavityTopRight = 0.634f;

        /// <summary>Mực nước cao nhất — nằm ngay dưới mép trong của vành.</summary>
        public const float LiquidTop = 0.840f;

        /// <summary>Độ vồng của mặt nước — nhìn hơi chúc xuống lòng ca nên mặt nước là một cung.</summary>
        private const float SurfaceRise = 0.020f;

        // Mỏ rót bên trái: nêm loe dần từ mũi vào thân.
        private const float SpoutTipU = 0.102f;
        private const float SpoutBaseU = 0.245f;
        private const float SpoutTipBottom = 0.846f;
        private const float SpoutTipTop = 0.878f;
        private const float SpoutBaseBottom = 0.760f;
        private const float SpoutBaseTop = 0.914f;

        // Quai bên phải, dựng bằng một vòng bầu dục rồi CẮT BỎ nửa nằm trong thân ca.
        // Tâm vòng phải đặt xấp xỉ ngay TRÊN mép thân (~0.633 ở lưng chừng ca): đặt ra ngoài thì
        // phần bị cắt quá ít, chữ C gần như khép kín và nhìn vẫn ra vòng O rời. Chỗ vòng cắt qua
        // mép thân chính là hai điểm quai cắm vào ca.
        private const float HandleCenterU = 0.628f;
        private const float HandleCenterV = 0.500f;
        private const float HandleOuterU = 0.150f;
        private const float HandleOuterV = 0.260f;
        private const float HandleInnerU = 0.082f;
        private const float HandleInnerV = 0.168f;

        // Đế ca: gờ bè ra một chút cho ca đứng vững, thay cho đĩa lót của tách.
        private const float FootLeft = 0.232f;
        private const float FootRight = 0.618f;
        private const float FootBottom = 0.022f;

        private const float Soft = 0.003f;
        private const float CornerU = 0.020f;
        private const float CornerV = 0.028f;

        // Thuỷ tinh trong: nhìn xuyên được nên thấy cả mực nước bên trong.
        private static readonly Color Glass = new(1f, 0.992f, 0.965f, 0.30f);
        private static readonly Color Outline = new(0.651f, 0.502f, 0.353f, 1f);
        private static readonly Color InnerShade = new(0.847f, 0.776f, 0.667f, 0.28f);

        [MenuItem("DreamCafe/Art/Generate Brewing Pitcher Sprites")]
        public static void GenerateMenu()
        {
            Generate(overwrite: true);
            Debug.Log($"[PitcherArt] Đã vẽ lại ca pha chế + {BrewingController.MaxIngredients} tầng nước trong {OutputDir}.");
        }

        /// <summary>
        /// Sinh đủ bộ ảnh ca. <paramref name="overwrite"/> = false thì file nào có sẵn là giữ
        /// nguyên — art vẽ tay đè lên sẽ không bị xoá mất.
        /// </summary>
        public static void Generate(bool overwrite)
        {
            EnsureBack(overwrite);
            EnsureFront(overwrite);
            for (int i = 0; i < BrewingController.MaxIngredients; i++) EnsureLiquid(i, overwrite);
            AssetDatabase.Refresh();
        }

        // =====================================================================
        // Các lớp ảnh
        // =====================================================================

        /// <summary>Quai, mỏ rót, đế và thân thuỷ tinh — nằm DƯỚI nước.</summary>
        public static Sprite EnsureBack(bool overwrite = false) =>
            ProceduralSpriteUtility.EnsureSprite(BackPath, TextureWidth, TextureHeight, (u, v) =>
            {
                var color = Color.clear;

                // --- Thân + mỏ rót + đế ---
                float body = OuterMask(u, v, 0f);
                float bodyFill = OuterMask(u, v, 0.012f);

                // --- Quai ---
                float handleOuter = ProceduralSpriteUtility.EllipseMask(u, v, HandleCenterU, HandleCenterV,
                    HandleOuterU, HandleOuterV, Soft);
                float handleHole = ProceduralSpriteUtility.EllipseMask(u, v, HandleCenterU, HandleCenterV,
                    HandleInnerU, HandleInnerV, Soft);
                float handleFill = Mathf.Clamp01(
                    ProceduralSpriteUtility.EllipseMask(u, v, HandleCenterU, HandleCenterV,
                        HandleOuterU - 0.015f, HandleOuterV - 0.017f, Soft)
                    - ProceduralSpriteUtility.EllipseMask(u, v, HandleCenterU, HandleCenterV,
                        HandleInnerU + 0.015f, HandleInnerV + 0.017f, Soft));
                float handle = Mathf.Clamp01(handleOuter - handleHole);

                // Cắt bỏ phần vòng quai nằm trong thân ca. KHÔNG thể trông chờ vẽ thân đè lên là
                // xong: thuỷ tinh chỉ đục 30% nên cung trái của vòng vẫn lộ qua, ra hình chữ O rời
                // thay vì chữ C dính liền.
                handle = Mathf.Clamp01(handle - bodyFill);
                handleFill = Mathf.Clamp01(handleFill - bodyFill);

                // Viền phải là một VÒNG chứ không phải tô đặc cả silhouette rồi phủ lên: thuỷ tinh
                // chỉ đục 30% nên tô đặc là ruột ca ra màu viền.
                color = ProceduralSpriteUtility.Over(color, Glass, handleFill);
                color = ProceduralSpriteUtility.Over(color, Outline, Mathf.Clamp01(handle - handleFill));

                color = ProceduralSpriteUtility.Over(color, Glass, bodyFill);
                color = ProceduralSpriteUtility.Over(color, Outline, Mathf.Clamp01(body - bodyFill));

                // --- Lòng ca rỗng: tối dần xuống đáy cho ra chiều sâu ---
                float cavity = CavityMask(u, v);
                float depth = 1f - ProceduralSpriteUtility.SmoothStep01(CavityBottom, RimY, v);
                color = ProceduralSpriteUtility.Over(color, InnerShade, cavity * (0.35f + 0.5f * depth));

                return color;
            }, overwrite);

        /// <summary>Vành miệng, vạch đong và vệt sáng — nằm TRÊN nước.</summary>
        public static Sprite EnsureFront(bool overwrite = false) =>
            ProceduralSpriteUtility.EnsureSprite(FrontPath, TextureWidth, TextureHeight, (u, v) =>
            {
                var color = Color.clear;

                // --- Vành miệng: bầu dục ngoài trừ bầu dục trong ---
                float rimOuter = ProceduralSpriteUtility.EllipseMask(u, v, BodyCenterU, RimY,
                    (OuterTopRight - OuterTopLeft) * 0.5f, RimHalfHeight, Soft);
                float rimInner = ProceduralSpriteUtility.EllipseMask(u, v, BodyCenterU, RimY,
                    (CavityTopRight - CavityTopLeft) * 0.5f, RimHalfHeight * 0.76f, Soft);
                float rim = Mathf.Clamp01(rimOuter - rimInner);

                float rimEdge = Mathf.Clamp01(rim - Mathf.Clamp01(
                    ProceduralSpriteUtility.EllipseMask(u, v, BodyCenterU, RimY,
                        (OuterTopRight - OuterTopLeft) * 0.5f - 0.010f, RimHalfHeight - 0.007f, Soft)
                    - rimInner));

                color = ProceduralSpriteUtility.Over(color, Glass, rim);
                color = ProceduralSpriteUtility.Over(color, Outline, rimEdge);

                // --- Vạch đong ở thành trái, đúng ngay mốc chia giữa các tầng ---
                for (int i = 1; i < BrewingController.MaxIngredients; i++)
                {
                    float level = Mathf.Lerp(CavityBottom, LiquidTop, (float)i / BrewingController.MaxIngredients);
                    CavityEdges(level, out float left, out _);
                    float tick = ProceduralSpriteUtility.RoundedBoxMask(u, v,
                        left + 0.006f, left + 0.072f, level - 0.005f, level + 0.005f, 0.004f, Soft);
                    color = ProceduralSpriteUtility.Over(color, Outline, tick * 0.7f);
                }

                // --- Vệt sáng dọc thành trái, cắt gọn trong silhouette ---
                float body = OuterMask(u, v, 0.012f);
                float shine = ProceduralSpriteUtility.RoundedBoxMask(u, v,
                    0.258f, 0.300f, 0.17f, 0.74f, 0.021f, 0.02f) * body;
                color = ProceduralSpriteUtility.Over(color, Color.white, shine * 0.5f);

                return color;
            }, overwrite);

        /// <summary>
        /// Khoảng cao (chuẩn hoá trong khung ca) mà ảnh tầng thứ <paramref name="index"/> chiếm.
        /// Ô UI của tầng đó phải neo đúng khoảng này thì ảnh mới nằm đúng chỗ.
        ///
        /// Mép trên cộng thêm <see cref="SurfaceRise"/> vì mặt nước vồng lên KHỎI mốc chia — cắt
        /// đúng mốc là mất luôn cái vòm.
        /// </summary>
        public static void LiquidBand(int index, out float bottom, out float top)
        {
            int count = BrewingController.MaxIngredients;
            bottom = Mathf.Lerp(CavityBottom, LiquidTop, (float)index / count);
            top = Mathf.Lerp(CavityBottom, LiquidTop, (float)(index + 1) / count) + SurfaceRise;
        }

        /// <summary>
        /// Một tầng nước: mặt cắt lòng ca trong khoảng cao của tầng đó, vẽ màu trắng để nhuộm lúc
        /// chạy bằng mã hex của nguyên liệu.
        ///
        /// Ảnh được CẮT đúng bằng dải của tầng chứ không phủ trọn khung ca — có vậy thì ô UI mới
        /// chạy được hoạt ảnh rót fillAmount 0..1 trong phạm vi riêng của tầng mình.
        /// </summary>
        public static Sprite EnsureLiquid(int index, bool overwrite = false)
        {
            LiquidBand(index, out float bandBottom, out float bandTop);

            int count = BrewingController.MaxIngredients;
            float surfaceLevel = Mathf.Lerp(CavityBottom, LiquidTop, (float)(index + 1) / count);
            int height = Mathf.Max(8, Mathf.RoundToInt((bandTop - bandBottom) * TextureHeight));

            return ProceduralSpriteUtility.EnsureSprite(LiquidPath(index), TextureWidth, height, (u, v) =>
            {
                // v là toạ độ trong ẢNH TẦNG; đổi về toạ độ trong khung ca để dùng chung hình học.
                float cupV = Mathf.Lerp(bandBottom, bandTop, v);

                float cavity = CavityMask(u, cupV);
                if (cavity <= 0f) return Color.clear;

                float surfaceTop = SurfaceAt(u, surfaceLevel);
                float band = 1f - ProceduralSpriteUtility.SmoothStep01(surfaceTop - Soft, surfaceTop + Soft, cupV);

                // Tầng dưới cùng để lòng ca tự cắt phần đáy; tầng trên thì đáy phải khớp đúng vòm
                // của tầng ngay dưới, không thì hở một đường.
                if (index > 0)
                {
                    float surfaceBottom = SurfaceAt(u, bandBottom);
                    band *= ProceduralSpriteUtility.SmoothStep01(surfaceBottom - Soft, surfaceBottom + Soft, cupV);
                }

                float alpha = cavity * band;
                return alpha <= 0f ? Color.clear : new Color(1f, 1f, 1f, alpha);
            }, overwrite);
        }

        // =====================================================================
        // Hình học
        // =====================================================================

        /// <summary>Trục dọc của thân ca — lệch trái so với tâm ảnh vì quai chiếm chỗ bên phải.</summary>
        private static float BodyCenterU => (OuterTopLeft + OuterTopRight) * 0.5f;

        /// <summary>Mặt nước tại độ cao <paramref name="level"/>: vồng lên ở giữa, chúc xuống hai bên.</summary>
        private static float SurfaceAt(float u, float level)
        {
            CavityEdges(level, out float left, out float right);
            float half = (right - left) * 0.5f;
            if (half <= 0f) return level;

            float offset = Mathf.Clamp((u - (left + half)) / half, -1f, 1f);
            return level + SurfaceRise * Mathf.Sqrt(Mathf.Max(0f, 1f - offset * offset));
        }

        /// <summary>Hai mép TRONG của lòng ca tại độ cao v.</summary>
        public static void CavityEdges(float v, out float left, out float right)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(CavityBottom, RimY, v));
            left = Mathf.Lerp(CavityBottomLeft, CavityTopLeft, t);
            right = Mathf.Lerp(CavityBottomRight, CavityTopRight, t);
        }

        /// <summary>Silhouette đầy đủ: thân + vành + mỏ rót + đế, thu vào trong <paramref name="inset"/>.</summary>
        private static float OuterMask(float u, float v, float inset)
        {
            float body = TaperedMask(u, v, BodyBottom + inset, RimY,
                OuterBottomLeft + inset, OuterBottomRight - inset,
                OuterTopLeft + inset, OuterTopRight - inset);

            float rim = ProceduralSpriteUtility.EllipseMask(u, v, BodyCenterU, RimY,
                (OuterTopRight - OuterTopLeft) * 0.5f - inset, RimHalfHeight - inset, Soft);

            float foot = ProceduralSpriteUtility.RoundedBoxMask(u, v,
                FootLeft + inset, FootRight - inset, FootBottom + inset, BodyBottom + 0.03f,
                0.012f, Soft);

            return Mathf.Max(Mathf.Max(body, rim), Mathf.Max(SpoutMask(u, v, inset), foot));
        }

        /// <summary>Lòng ca cộng miệng loe.</summary>
        private static float CavityMask(float u, float v)
        {
            float body = TaperedMask(u, v, CavityBottom, RimY,
                CavityBottomLeft, CavityBottomRight, CavityTopLeft, CavityTopRight);

            float mouth = ProceduralSpriteUtility.EllipseMask(u, v, BodyCenterU, RimY,
                (CavityTopRight - CavityTopLeft) * 0.5f, RimHalfHeight * 0.76f, Soft);

            return Mathf.Max(body, mouth);
        }

        /// <summary>
        /// Mỏ rót: một cái nêm chụm ở mũi và loe dần vào chỗ nối thân. Mũ 1.3 giữ cho đoạn gần mũi
        /// còn thon, chứ loe tuyến tính thì ra hình tam giác bè trông như cái vòi.
        /// </summary>
        private static float SpoutMask(float u, float v, float inset)
        {
            float tip = SpoutTipU + inset;
            if (u < tip || u > SpoutBaseU) return 0f;

            float flare = Mathf.Pow(Mathf.Clamp01(Mathf.InverseLerp(tip, SpoutBaseU, u)), 1.3f);
            float bottom = Mathf.Lerp(SpoutTipBottom, SpoutBaseBottom, flare) + inset;
            float top = Mathf.Lerp(SpoutTipTop, SpoutBaseTop, flare) - inset;
            if (top <= bottom) return 0f;

            return ProceduralSpriteUtility.SmoothStep01(bottom - Soft, bottom + Soft, v)
                   * (1f - ProceduralSpriteUtility.SmoothStep01(top - Soft, top + Soft, v));
        }

        /// <summary>
        /// Hình thang đứng (loe dần lên trên), mép mềm, đáy bo góc nhẹ. Không dùng được hộp bo góc
        /// thông thường vì hai thành nghiêng chứ không thẳng.
        /// </summary>
        private static float TaperedMask(float u, float v, float bottom, float top,
            float bottomLeft, float bottomRight, float topLeft, float topRight)
        {
            if (v < bottom - Soft || v > top + Soft) return 0f;

            float t = Mathf.Clamp01(Mathf.InverseLerp(bottom, top, v));
            float left = Mathf.Lerp(bottomLeft, topLeft, t);
            float right = Mathf.Lerp(bottomRight, topRight, t);

            float dy = v - bottom;
            if (dy < CornerV)
            {
                // Thu vào theo cung tròn: sát đáy thì hụt hẳn CornerU, lên tới mép góc thì thôi.
                float a = (CornerV - dy) / CornerV;
                float inset = CornerU * (1f - Mathf.Sqrt(Mathf.Max(0f, 1f - a * a)));
                left += inset;
                right -= inset;
            }

            if (right - left <= 0f) return 0f;

            float horizontal = ProceduralSpriteUtility.SmoothStep01(left - Soft, left + Soft, u)
                               * (1f - ProceduralSpriteUtility.SmoothStep01(right - Soft, right + Soft, u));
            float vertical = ProceduralSpriteUtility.SmoothStep01(bottom - Soft, bottom + Soft, v)
                             * (1f - ProceduralSpriteUtility.SmoothStep01(top - Soft, top + Soft, v));

            return horizontal * vertical;
        }
    }
}
