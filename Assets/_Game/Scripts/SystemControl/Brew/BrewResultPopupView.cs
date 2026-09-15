using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Popup báo kết quả sau khi bấm BREW, phủ lên panel pha chế:
    /// - Pha ra món  -> ruy băng xanh + tên món (kèm chip "New!" nếu vừa mò ra lần đầu),
    ///                  tia sáng vàng, mèo mắt sao, chữ "Congratulation".
    /// - Pha hỏng    -> ruy băng tím "FAILED", tia sáng xám, mèo khóc, chữ "Better next time".
    ///
    /// Chạm bất kỳ đâu để tắt. Các trường hợp thao tác sai (quên nước, chưa bỏ nguyên liệu)
    /// KHÔNG bật popup — cái đó chỉ báo bằng dòng chữ dưới bảng gợi ý, đỡ làm phiền người chơi.
    /// </summary>
    public sealed class BrewResultPopupView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("Node bật/tắt của cả popup (gồm cả lớp làm mờ nền).")]
        private GameObject root;
        [SerializeField, Tooltip("Node được phóng to dần lúc hiện — chứa ruy băng, ly, mèo.")]
        private RectTransform content;
        [SerializeField, Tooltip("Lớp mờ nền, bấm vào là tắt popup.")]
        private Button dismissButton;
        [SerializeField, Tooltip("Cụm khói nổ phủ lên popup lúc nó vừa hiện rồi tan ra — để trống thì popup hiện trơn.")]
        private UISmokeBurstView smokeBurst;

        [Header("Nội dung")]
        [SerializeField] private Image rays;
        [SerializeField] private Image drinkIcon;
        [SerializeField] private Image banner;
        [SerializeField] private TMP_Text bannerLabel;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private TMP_Text headlineLabel;
        [SerializeField] private Image cat;

        [Header("Sprite theo kết quả")]
        [SerializeField] private Sprite successRays;
        [SerializeField] private Sprite failRays;
        [SerializeField, Tooltip("Ruy băng xanh khi pha ra món.")]
        private Sprite successBanner;
        [SerializeField, Tooltip("Ruy băng tím khi pha hỏng.")]
        private Sprite failBanner;
        [SerializeField] private Sprite happyCat;
        [SerializeField] private Sprite sadCat;
        [SerializeField, Tooltip("Ảnh ly hỏng — bỏ trống thì khi pha hỏng sẽ không hiện ly nào.")]
        private Sprite failedDrink;

        [Header("Tinh chỉnh")]
        [SerializeField, Tooltip("Tốc độ xoay của vòng tia sáng (độ/giây).")]
        private float raysSpinSpeed = 8f;
        [SerializeField, Tooltip("Thời gian phóng to lúc hiện popup (giây).")]
        private float popInSeconds = 0.18f;

        private Coroutine _popRoutine;

        /// <summary>
        /// Bắn khi popup vừa bị tắt trong lúc ĐANG hiện — dùng để nối popup tiếp theo
        /// (bảng "NEW RECIPE DISCOVERED!"). Gọi Hide() lúc popup đã tắt sẵn thì không bắn,
        /// nên dọn dẹp lúc mở/đóng panel không kích nhầm.
        /// </summary>
        public event Action Dismissed;

        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            if (dismissButton != null) dismissButton.onClick.AddListener(Hide);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (dismissButton != null) dismissButton.onClick.RemoveListener(Hide);
            Dismissed = null;
        }

        private void Update()
        {
            if (rays != null && IsShowing)
            {
                rays.transform.Rotate(0f, 0f, raysSpinSpeed * Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Hiện popup cho một kết quả pha. Trả về false nếu kết quả này không đáng bật popup
        /// (thao tác sai chứ không phải pha hỏng).
        /// </summary>
        public bool Show(BrewResult result)
        {
            if (root == null) return false;

            switch (result.Outcome)
            {
                case BrewOutcome.Success:
                case BrewOutcome.Discovered:
                    ShowSuccess(result);
                    break;

                case BrewOutcome.NoMatch:
                    ShowFailure();
                    break;

                default:
                    // Quên nước / chưa bỏ nguyên liệu / hết hàng — không phải kết quả pha.
                    return false;
            }

            root.SetActive(true);
            PlayPopIn();
            // Nổ SAU khi bật root: khói là con của root nên lúc root còn tắt thì không chạy được
            // coroutine. Khói vẽ đè lên popup rồi tan dần, thành ra popup như được khói thả xuống —
            // nối thẳng vào đúng lúc màn chờ vừa trượt đi.
            if (smokeBurst != null) smokeBurst.Burst();
            return true;
        }

        public void Hide()
        {
            bool wasShowing = IsShowing;

            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }
            if (smokeBurst != null) smokeBurst.StopBurst();
            if (root != null) root.SetActive(false);

            if (wasShowing) Dismissed?.Invoke();
        }

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private void ShowSuccess(BrewResult result)
        {
            var recipe = result.Recipe;

            SetBanner(successRays, successBanner,
                recipe != null ? recipe.DisplayName.ToUpperInvariant() : "DRINK READY");

            if (newBadge != null) newBadge.SetActive(result.Outcome == BrewOutcome.Discovered);
            if (headlineLabel != null) headlineLabel.text = "Congratulation";
            if (cat != null) cat.sprite = happyCat;

            SetDrink(recipe != null ? recipe.Icon : null);
        }

        private void ShowFailure()
        {
            SetBanner(failRays, failBanner, "FAILED");

            if (newBadge != null) newBadge.SetActive(false);
            if (headlineLabel != null) headlineLabel.text = "Better next time";
            if (cat != null) cat.sprite = sadCat;

            SetDrink(failedDrink);
        }

        private void SetBanner(Sprite raysSprite, Sprite bannerSprite, string title)
        {
            if (rays != null)
            {
                rays.sprite = raysSprite;
                rays.enabled = raysSprite != null;
                // Reset góc xoay để lần nào hiện cũng bắt đầu như nhau.
                rays.transform.localRotation = Quaternion.identity;
            }

            if (banner != null)
            {
                banner.sprite = bannerSprite;
                banner.enabled = bannerSprite != null;
            }

            if (bannerLabel != null) bannerLabel.text = title;
        }

        private void SetDrink(Sprite sprite)
        {
            if (drinkIcon == null) return;
            drinkIcon.sprite = sprite;
            // Không có ảnh thì ẩn hẳn, tránh hiện ô vuông trắng mặc định của Image.
            drinkIcon.enabled = sprite != null;
        }

        private void PlayPopIn()
        {
            if (content == null) return;
            if (_popRoutine != null) StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(PopInRoutine());
        }

        private IEnumerator PopInRoutine()
        {
            const float startScale = 0.82f;
            float elapsed = 0f;

            while (elapsed < popInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popInSeconds);
                // Nảy nhẹ một chút ở cuối cho đỡ khô.
                float eased = Mathf.LerpUnclamped(startScale, 1f, 1f - Mathf.Pow(1f - t, 3f));
                content.localScale = Vector3.one * eased;
                yield return null;
            }

            content.localScale = Vector3.one;
            _popRoutine = null;
        }
    }
}
