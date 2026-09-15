using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Cụm khói nổ một phát ("smoke bomb") dùng để che rồi mở ra một tấm popup — kiểu hiệu ứng
    /// của Fantasy Life i: chớp sáng, một vòng khói bung ra từ tâm, khói loãng dần và lộ ra nội dung.
    ///
    /// Viết bằng Image của UGUI chứ KHÔNG dùng ParticleSystem: Canvas của game chạy Screen Space
    /// Overlay, mà ParticleSystem thì không chen được vào giữa các lớp UI của chế độ này. Bù lại
    /// hiệu ứng luôn nằm đúng thứ tự vẽ mình đặt, và không cần thêm camera phụ.
    ///
    /// Các cụm khói được gom sẵn thành pool ở lần nổ đầu tiên rồi dùng đi dùng lại, nên nổ nhiều
    /// lần không sinh rác. Toàn bộ chạy bằng unscaled time để còn nổ được lúc game đang tạm dừng.
    /// </summary>
    public sealed class UISmokeBurstView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("Ảnh một cụm khói — nên là vệt tròn mờ viền. Bỏ trống thì không có khói.")]
        private Sprite puffSprite;
        [SerializeField, Tooltip("Tấm chớp sáng phủ toàn màn lúc nổ (tuỳ chọn).")]
        private Image flash;
        [SerializeField, Tooltip("Vòng xung kích lan ra từ tâm (tuỳ chọn).")]
        private RectTransform shockwave;

        [Header("Khói")]
        [SerializeField, Min(1), Tooltip("Số cụm khói bung ra.")]
        private int puffCount = 18;
        [SerializeField, Tooltip("Bán kính khói bay tới (pixel UI).")]
        private float radius = 360f;
        [SerializeField, Range(0f, 1f), Tooltip("Mức lệch ngẫu nhiên của bán kính — 0 là bung thành vòng tròn đều tăm tắp.")]
        private float radiusJitter = 0.45f;
        [SerializeField, Tooltip("Cỡ một cụm khói (pixel UI).")]
        private Vector2 puffSize = new(260f, 260f);
        [SerializeField, Tooltip("Tỉ lệ cụm khói lúc vừa nổ.")]
        private float startScale = 0.35f;
        [SerializeField, Tooltip("Tỉ lệ cụm khói lúc tan hết — nở ra thì mới giống khói.")]
        private float endScale = 1.25f;
        [SerializeField, Tooltip("Khói nhẹ nên bốc lên trong lúc tan (pixel UI, 0 = bay ngang đều).")]
        private float rise = 90f;

        [Header("Nhịp")]
        [SerializeField, Min(0.05f), Tooltip("Một cụm khói sống được bao lâu (giây).")]
        private float puffLifetime = 0.72f;
        [SerializeField, Min(0f), Tooltip("Các cụm nổ lệch nhau trong khoảng này cho đỡ đều (giây).")]
        private float spawnSpread = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Giữ đục hoàn toàn trong chừng này của đời cụm khói rồi mới nhạt dần.")]
        private float opaqueFraction = 0.25f;
        [SerializeField, Min(0f), Tooltip("Thời gian chớp sáng (giây).")]
        private float flashSeconds = 0.16f;
        [SerializeField, Min(0f), Tooltip("Thời gian vòng xung kích lan ra (giây).")]
        private float shockwaveSeconds = 0.34f;
        [SerializeField, Tooltip("Vòng xung kích lan tới cỡ gấp mấy lần cỡ gốc.")]
        private float shockwaveScale = 2.2f;

        [Header("Màu")]
        [SerializeField, Tooltip("Màu cụm khói sáng nhất.")]
        private Color lightSmoke = Color.white;
        [SerializeField, Tooltip("Màu cụm khói sẫm nhất — mỗi cụm bốc một màu ngẫu nhiên giữa hai màu này. Để trùng màu sáng thì cả đám khói trắng tinh.")]
        private Color darkSmoke = Color.white;
        [SerializeField, Tooltip("Màu tấm chớp sáng lúc mạnh nhất.")]
        private Color flashColor = new(1f, 1f, 1f, 0.85f);

        private Puff[] _puffs;
        private Coroutine _routine;
        private Image _shockwaveImage;

        /// <summary>Một cụm khói trong pool cùng thông số của lần nổ hiện tại.</summary>
        private struct Puff
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Direction;
            public float Distance;
            public float Delay;
            public float Lifetime;
            public float Spin;
            public float ScaleTo;
            public Color Color;
        }

        public bool IsBursting => _routine != null;

        private void Awake()
        {
            if (shockwave != null) _shockwaveImage = shockwave.GetComponent<Image>();
            HideAll();
        }

        /// <summary>
        /// Nổ một phát khói từ tâm node này. Gọi chồng lên lần nổ trước thì lần trước bị cắt ngang —
        /// hai cụm khói chồng nhau chỉ thành một vũng trắng đục, không đẹp hơn.
        /// </summary>
        public void Burst()
        {
            if (!isActiveAndEnabled) return;

            EnsurePool();
            if (_puffs == null || _puffs.Length == 0) return;

            Arrange();

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(BurstRoutine());
        }

        /// <summary>Dập tắt khói ngay lập tức — dùng khi popup bị đóng giữa chừng.</summary>
        public void StopBurst()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            HideAll();
        }

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private void EnsurePool()
        {
            if (_puffs != null && _puffs.Length == puffCount) return;

            // Đổi puffCount trong Inspector giữa chừng thì dựng lại pool cho khớp.
            if (_puffs != null)
            {
                foreach (var old in _puffs)
                {
                    if (old.Rect != null) Destroy(old.Rect.gameObject);
                }
            }

            _puffs = new Puff[Mathf.Max(1, puffCount)];
            for (int i = 0; i < _puffs.Length; i++)
            {
                var go = new GameObject($"Puff{i}", typeof(RectTransform)) { layer = gameObject.layer };
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = puffSize;

                var image = go.AddComponent<Image>();
                image.sprite = puffSprite;
                image.preserveAspect = true;
                // Khói phủ lên cả nút X và ADD TO MENU của popup — ăn click thì người chơi bấm hụt.
                image.raycastTarget = false;

                go.SetActive(false);
                _puffs[i] = new Puff { Rect = rect, Image = image };
            }

            // Các cụm khói vừa thêm vào cuối nên đang nằm trên tấm chớp sáng — đẩy chớp lên trên cùng,
            // chớp mà bị khói che thì mất hẳn cú "bụp" của quả khói.
            if (flash != null) flash.transform.SetAsLastSibling();
        }

        /// <summary>Rải các cụm khói quanh vòng tròn và bốc thăm thông số cho lần nổ này.</summary>
        private void Arrange()
        {
            // Chia đều rồi mới xê dịch: bốc góc hoàn toàn ngẫu nhiên sẽ để lại mảng trống lộ liễu.
            float step = 360f / _puffs.Length;
            float offset = Random.Range(0f, 360f);

            for (int i = 0; i < _puffs.Length; i++)
            {
                float angle = (offset + step * i + Random.Range(-step * 0.4f, step * 0.4f)) * Mathf.Deg2Rad;

                var puff = _puffs[i];
                puff.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                puff.Distance = radius * Random.Range(1f - radiusJitter, 1f);
                puff.Delay = Random.Range(0f, spawnSpread);
                puff.Lifetime = puffLifetime * Random.Range(0.8f, 1.15f);
                puff.Spin = Random.Range(-70f, 70f);
                puff.ScaleTo = endScale * Random.Range(0.82f, 1.2f);
                puff.Color = Color.Lerp(lightSmoke, darkSmoke, Random.value);

                if (puff.Image != null)
                {
                    puff.Image.sprite = puffSprite;
                    puff.Image.enabled = puffSprite != null;
                }
                if (puff.Rect != null) puff.Rect.sizeDelta = puffSize;

                _puffs[i] = puff;
            }
        }

        private IEnumerator BurstRoutine()
        {
            foreach (var puff in _puffs)
            {
                if (puff.Rect != null) puff.Rect.gameObject.SetActive(true);
            }

            if (flash != null) flash.gameObject.SetActive(true);
            if (shockwave != null) shockwave.gameObject.SetActive(true);

            float total = spawnSpread + puffLifetime * 1.15f;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int i = 0; i < _puffs.Length; i++) StepPuff(_puffs[i], elapsed);
                StepFlash(elapsed);
                StepShockwave(elapsed);

                yield return null;
            }

            _routine = null;
            HideAll();
        }

        private void StepPuff(Puff puff, float elapsed)
        {
            if (puff.Rect == null) return;

            float t = (elapsed - puff.Delay) / puff.Lifetime;

            if (t < 0f)
            {
                // Chưa tới lượt: nằm im ở tâm, trong suốt.
                SetAlpha(puff, 0f);
                return;
            }

            t = Mathf.Clamp01(t);

            // Bung ra thật nhanh rồi ghì lại — khói mất đà gần như tức thì.
            float travel = 1f - Mathf.Pow(1f - t, 4f);
            var position = puff.Direction * (puff.Distance * travel);
            position.y += rise * t * t;
            puff.Rect.anchoredPosition = position;

            float scale = Mathf.LerpUnclamped(startScale, puff.ScaleTo, 1f - Mathf.Pow(1f - t, 3f));
            puff.Rect.localScale = Vector3.one * scale;
            puff.Rect.localRotation = Quaternion.Euler(0f, 0f, puff.Spin * t);

            // Đục hết cỡ lúc đầu (để che kín tấm thẻ) rồi mới loãng dần ra.
            float fade = t <= opaqueFraction
                ? 1f
                : 1f - Mathf.SmoothStep(0f, 1f, (t - opaqueFraction) / (1f - opaqueFraction));
            SetAlpha(puff, fade);
        }

        private void StepFlash(float elapsed)
        {
            if (flash == null) return;

            if (flashSeconds <= 0f || elapsed >= flashSeconds)
            {
                flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
                return;
            }

            float t = elapsed / flashSeconds;
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1f - t));
        }

        private void StepShockwave(float elapsed)
        {
            if (shockwave == null) return;

            if (shockwaveSeconds <= 0f || elapsed >= shockwaveSeconds)
            {
                SetOpacity(_shockwaveImage, 0f);
                return;
            }

            float t = elapsed / shockwaveSeconds;
            shockwave.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, shockwaveScale, 1f - Mathf.Pow(1f - t, 3f));
            SetOpacity(_shockwaveImage, 1f - t);
        }

        private static void SetOpacity(Image image, float alpha)
        {
            if (image == null) return;
            var color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private static void SetAlpha(Puff puff, float alpha)
        {
            if (puff.Image == null) return;
            var color = puff.Color;
            color.a *= Mathf.Clamp01(alpha);
            puff.Image.color = color;
        }

        private void HideAll()
        {
            if (_puffs != null)
            {
                foreach (var puff in _puffs)
                {
                    if (puff.Rect == null) continue;
                    puff.Rect.anchoredPosition = Vector2.zero;
                    puff.Rect.localScale = Vector3.one * startScale;
                    puff.Rect.gameObject.SetActive(false);
                }
            }

            if (flash != null) flash.gameObject.SetActive(false);
            if (shockwave != null) shockwave.gameObject.SetActive(false);
        }
    }
}
