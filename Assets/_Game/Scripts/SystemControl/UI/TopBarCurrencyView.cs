using DreamCafe.DataControl;
using TMPro;
using UnityEngine;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// View hiển thị thanh thông tin tiền tệ ở đỉnh đầu màn hình (Top Bar UI).
    /// Hiển thị:
    /// - Tiền mặt hiện có (Current Money)
    /// - Tốc độ tiền trên giây (Money Per Second)
    /// - Điểm danh tiếng quán (Reputation)
    /// </summary>
    public sealed class TopBarCurrencyView : MonoBehaviour
    {
        [Header("UI Text References (TextMeshPro)")]
        [SerializeField, Tooltip("Text hiển thị tiền hiện có.")]
        private TextMeshProUGUI _moneyText;

        [SerializeField, Tooltip("Text hiển thị tốc độ tiền / giây.")]
        private TextMeshProUGUI _mpsText;

        [SerializeField, Tooltip("Text hiển thị điểm danh tiếng quán.")]
        private TextMeshProUGUI _reputationText;

        [Header("Tùy chọn hiển thị")]
        [SerializeField] private string _currencyUnit = "đ";

        private CurrencyController _currencyController;

        public CurrencyController Controller => _currencyController;

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            // Tự động tick cộng dồn passive income
            _currencyController?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Liên kết View với CurrencyController để tự động cập nhật UI.
        /// </summary>
        public void Bind(CurrencyController controller)
        {
            if (_currencyController != null)
            {
                Unbind();
            }

            _currencyController = controller;

            if (_currencyController != null)
            {
                _currencyController.Changed += Refresh;
                Refresh();
            }
        }

        /// <summary>
        /// Hủy liên kết Controller.
        /// </summary>
        public void Unbind()
        {
            if (_currencyController != null)
            {
                _currencyController.Changed -= Refresh;
                _currencyController = null;
            }
        }

        private static readonly System.Globalization.CultureInfo s_culture = new System.Globalization.CultureInfo("vi-VN");

        /// <summary>
        /// Cập nhật nội dung hiển thị trên các Text UI.
        /// Định dạng:
        /// - Tiền tệ: 500.000
        /// - Mps: 13/s
        /// - Rep: 8.080
        /// </summary>
        public void Refresh()
        {
            if (_currencyController == null) return;

            if (_moneyText != null)
            {
                string moneyStr = _currencyController.CurrentMoney.ToString("N0", s_culture);
                _moneyText.text = string.IsNullOrEmpty(_currencyUnit) ? moneyStr : $"{moneyStr} {_currencyUnit}";
            }

            if (_mpsText != null)
            {
                float mps = _currencyController.MoneyPerSecond;
                string mpsStr = (mps % 1 == 0)
                    ? ((long)mps).ToString("N0", s_culture)
                    : mps.ToString("#,##0.##", s_culture);

                _mpsText.text = $"{mpsStr}/s";
            }

            if (_reputationText != null)
            {
                _reputationText.text = _currencyController.Reputation.ToString("N0", s_culture);
            }
        }
    }
}
