using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Màn chờ "đang pha": tấm panel trượt từ dưới lên giữa màn hình, giữ hình chờ xoay tròn trong
    /// lúc đếm hết thời gian pha, rồi trượt đi để nhường chỗ cho popup kết quả.
    ///
    /// Chen vào giữa <see cref="BrewingController.Brew"/> và <see cref="BrewResultPopupView"/> nên
    /// thuần trang trí: nguyên liệu đã bị trừ và công thức đã mở khoá từ trước lúc màn này bật lên.
    /// Đóng panel pha chế giữa chừng thì <see cref="Cancel"/> bỏ luôn callback — popup không bật nữa.
    ///
    /// Chạy bằng <see cref="Time.unscaledDeltaTime"/> giống các popup khác, để còn hoạt động khi
    /// game bị dừng (timeScale = 0) lúc mở menu.
    /// </summary>
    public sealed class BrewLoadingView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("Node bật/tắt của cả màn chờ (gồm cả lớp mờ nền).")]
        private GameObject root;
        [SerializeField, Tooltip("Tấm panel trượt vào — chứa hình chờ và dòng chữ.")]
        private RectTransform panel;
        [SerializeField, Tooltip("Lớp mờ nền, hiện/tắt mượt theo panel.")]
        private CanvasGroup dim;

        [Header("Hình chờ")]
        [SerializeField, Tooltip("Vòng cung xoay tròn — chính là 'hình chờ' giữa panel.")]
        private RectTransform spinner;
        [SerializeField, Tooltip("Ảnh nằm trong lòng vòng xoay (ly cà phê) — nhún nhẹ theo nhịp.")]
        private RectTransform pulseIcon;
        [SerializeField, Tooltip("Image kiểu Filled/Radial360 chạy dần theo tiến độ chờ.")]
        private Image progressFill;
        [SerializeField, Tooltip("Dòng chữ dưới hình chờ — dấu chấm lửng tự chạy.")]
        private TMP_Text statusLabel;

        [Header("Trượt")]
        [SerializeField, Tooltip("Vị trí xuất phát của panel so với chỗ đứng cuối (mặc định: từ dưới màn hình lên).")]
        private Vector2 slideFrom = new(0f, -900f);
        [SerializeField, Min(0.01f), Tooltip("Thời gian trượt vào (giây).")]
        private float slideInSeconds = 0.34f;
        [SerializeField, Min(0.01f), Tooltip("Thời gian trượt ra (giây).")]
        private float slideOutSeconds = 0.22f;

        [Header("Thời gian chờ")]
        [SerializeField, Min(0f), Tooltip("Chờ ít nhất chừng này, kể cả công thức pha nhanh (giây).")]
        private float minHoldSeconds = 0.9f;
        [SerializeField, Min(0f), Tooltip("Chờ nhiều nhất chừng này, kể cả công thức pha lâu (giây).")]
        private float maxHoldSeconds = 2.4f;
        [SerializeField, Min(0f), Tooltip("Dùng khi công thức không khai báo thời gian pha (và khi pha hỏng).")]
        private float defaultHoldSeconds = 1.25f;

        [Header("Tinh chỉnh")]
        [SerializeField, Tooltip("Tốc độ xoay của vòng chờ (độ/giây) — số âm là xoay theo chiều kim đồng hồ.")]
        private float spinSpeed = -220f;
        [SerializeField, Tooltip("Biên độ nhún của ảnh giữa vòng xoay (0 = tắt).")]
        private float pulseAmount = 0.08f;
        [SerializeField, Tooltip("Số nhịp nhún mỗi giây.")]
        private float pulseSpeed = 2.2f;
        [SerializeField, Tooltip("Chữ mặc định — cố tình không nêu tên món để giữ bất ngờ cho popup.")]
        private string defaultMessage = "Brewing";
        [SerializeField, Min(0.05f), Tooltip("Mỗi chừng này giây thì thêm một dấu chấm vào cuối dòng chữ.")]
        private float dotInterval = 0.32f;

        /// <summary>Đuôi chấm lửng dựng sẵn — khỏi cấp phát chuỗi mới mỗi lần đổi nhịp.</summary>
        private static readonly string[] Dots = { string.Empty, ".", "..", "..." };

        private Coroutine _routine;
        private Action _onComplete;
        private Vector2 _home;
        private string _message;
        private float _elapsed;
        private int _dotCount;

        /// <summary>Màn chờ đang hiện hay không.</summary>
        public bool IsPlaying => _routine != null;

        private void Awake()
        {
            // Chỗ đứng cuối của panel chính là vị trí đã đặt sẵn trong scene.
            if (panel != null) _home = panel.anchoredPosition;
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy() => _onComplete = null;

        private void Update()
        {
            if (_routine == null) return;

            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;

            if (spinner != null) spinner.Rotate(0f, 0f, spinSpeed * dt);

            if (pulseIcon != null && pulseAmount > 0f)
            {
                float pulse = 1f + Mathf.Sin(_elapsed * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
                pulseIcon.localScale = Vector3.one * pulse;
            }

            if (statusLabel != null)
            {
                // Chỉ ghép chuỗi khi số dấu chấm đổi — ghép mỗi khung hình là xả rác cho GC.
                int dots = Mathf.FloorToInt(_elapsed / dotInterval) % 4;
                if (dots != _dotCount)
                {
                    _dotCount = dots;
                    statusLabel.text = _message + Dots[dots];
                }
            }
        }

        // =====================================================================
        // API
        // =====================================================================

        /// <summary>
        /// Quy đổi thời gian pha của công thức thành thời gian đứng chờ. Công thức pha nhanh vẫn
        /// phải chờ <see cref="minHoldSeconds"/> cho kịp thấy hoạt ảnh, còn pha lâu thì bị cắt bớt
        /// ở <see cref="maxHoldSeconds"/> để không phải ngồi nhìn màn chờ cả phút.
        /// </summary>
        /// <param name="craftSeconds">CraftTimeSeconds của công thức; 0 hoặc âm = pha hỏng/không rõ.</param>
        public float HoldFor(float craftSeconds)
        {
            if (craftSeconds <= 0f) return defaultHoldSeconds;
            return Mathf.Clamp(craftSeconds, minHoldSeconds, Mathf.Max(minHoldSeconds, maxHoldSeconds));
        }

        /// <summary>
        /// Bật màn chờ rồi gọi <paramref name="onComplete"/> đúng lúc panel vừa trượt đi hẳn.
        /// Gọi chồng lên lượt đang chạy thì lượt cũ bị bỏ (callback cũ không nổ).
        /// </summary>
        /// <param name="holdSeconds">Thời gian đứng chờ, chưa tính lúc trượt vào/ra.</param>
        /// <param name="message">Chữ dưới hình chờ; bỏ trống thì dùng <see cref="defaultMessage"/>.</param>
        /// <param name="onComplete">Việc làm tiếp sau khi chờ xong — thường là bật popup kết quả.</param>
        public bool Play(float holdSeconds, string message, Action onComplete)
        {
            if (root == null)
            {
                // Chưa gán màn chờ thì cũng đừng nuốt mất popup kết quả.
                onComplete?.Invoke();
                return false;
            }

            if (_routine != null) StopCoroutine(_routine);

            _onComplete = onComplete;
            _message = string.IsNullOrEmpty(message) ? defaultMessage : message;
            _elapsed = 0f;
            _dotCount = 0;

            if (statusLabel != null) statusLabel.text = _message;
            if (spinner != null) spinner.localRotation = Quaternion.identity;
            if (progressFill != null) progressFill.fillAmount = 0f;

            root.SetActive(true);
            _routine = StartCoroutine(PlayRoutine(Mathf.Max(0f, holdSeconds)));
            return true;
        }

        /// <summary>Tắt ngay màn chờ và BỎ callback đang treo — dùng khi người chơi đóng panel giữa chừng.</summary>
        public void Cancel()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _onComplete = null;
            ResetVisuals();
            if (root != null) root.SetActive(false);
        }

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private IEnumerator PlayRoutine(float holdSeconds)
        {
            yield return Slide(_home + slideFrom, _home, slideInSeconds, 0f, 1f, EaseOutBack);

            float waited = 0f;
            while (waited < holdSeconds)
            {
                waited += Time.unscaledDeltaTime;
                if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(waited / holdSeconds);
                yield return null;
            }

            if (progressFill != null) progressFill.fillAmount = 1f;

            yield return Slide(_home, _home + slideFrom, slideOutSeconds, 1f, 0f, EaseInCubic);

            // Dọn sạch TRƯỚC khi gọi callback: callback bật popup, mà popup lại có thể mở lượt chờ
            // kế tiếp — dọn sau sẽ xoá nhầm lượt mới.
            var finished = _onComplete;
            _onComplete = null;
            _routine = null;
            ResetVisuals();
            if (root != null) root.SetActive(false);

            finished?.Invoke();
        }

        private IEnumerator Slide(Vector2 from, Vector2 to, float seconds, float fadeFrom, float fadeTo,
            Func<float, float> ease)
        {
            if (panel != null) panel.anchoredPosition = from;
            if (dim != null) dim.alpha = fadeFrom;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);

                if (panel != null) panel.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t));
                // Lớp mờ đi thẳng theo t chứ không nảy theo panel — nảy cả nền trông rẻ tiền.
                if (dim != null) dim.alpha = Mathf.Lerp(fadeFrom, fadeTo, t);

                yield return null;
            }

            if (panel != null) panel.anchoredPosition = to;
            if (dim != null) dim.alpha = fadeTo;
        }

        private void ResetVisuals()
        {
            if (panel != null) panel.anchoredPosition = _home;
            if (dim != null) dim.alpha = 1f;
            if (pulseIcon != null) pulseIcon.localScale = Vector3.one;
            if (progressFill != null) progressFill.fillAmount = 0f;
        }

        /// <summary>Trượt vào có nảy quá đà một nhịp rồi mới về chỗ.</summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        /// <summary>Trượt ra thì tăng tốc dần — rời đi dứt khoát, không nảy.</summary>
        private static float EaseInCubic(float t) => t * t * t;
    }
}
